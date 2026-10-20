
import asyncio
import getpass
import json
import os
import ssl
import time
import websockets

URL = "wss://127.0.0.1:6868"
HEADSET_ID = "EPOCX-F416A4AD"

# Сохраняем только метрики Cortex.
STREAMS = ["met"]
EXPORT_STREAM_TYPES = ["PM"]

RECORD_SECONDS = 60 * 10
EXPORT_FOLDER = (
    r"C:\Users\denis\PycharmProjects\pythonProject8\emotiv_records"
)

request_id = 0
record_stopped_warning = False


async def rpc(ws, method, params=None):
    global request_id, record_stopped_warning

    request_id += 1
    rid = request_id

    payload = {
        "jsonrpc": "2.0",
        "id": rid,
        "method": method,
        "params": params or {}
    }

    await ws.send(json.dumps(payload))
    print(f"\n[REQUEST] {method}")

    while True:
        raw = await asyncio.wait_for(ws.recv(), timeout=30)
        message = json.loads(raw)

        warning = message.get("warning")
        if isinstance(warning, dict) and warning.get("code") == 30:
            record_stopped_warning = True

        if message.get("id") != rid:
            print("[Cortex message]", json.dumps(
                message, ensure_ascii=False
            ))
            continue

        if "error" in message:
            raise RuntimeError(
                f"{method}: "
                f"{json.dumps(message['error'], ensure_ascii=False)}"
            )

        return message.get("result")


async def main():
    global record_stopped_warning

    os.makedirs(EXPORT_FOLDER, exist_ok=True)

    client_id = 'lRnxfUgumIThPEbfa3DdimUJ6Jn4DaeJhMsIpkdh'
    client_secret = 'cbkrogXMHCUQV4GF5u7QTButLvSBbIixVSTVki0AVWOofxgr3eusA9Vz5Bwy6WRuONYtSQYtH47rzcmaDiyWcbux8k7w0TgVIPBalLeaNQGTx11mFAsIGMQQyaBmQzZc'

    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE

    async with websockets.connect(
        URL, ssl=ssl_context, ping_interval=20
    ) as ws:
        print("[OK] Cortex подключён")

        # 1. Авторизация
        auth = await rpc(ws, "authorize", {
            "clientId": client_id,
            "clientSecret": client_secret
        })

        token = auth.get("cortexToken") if auth else None
        if not token:
            raise RuntimeError("Не получен cortexToken")

        print("[OK] Авторизация успешна")

        # 2. Проверяем подключение гарнитуры
        headsets = await rpc(ws, "queryHeadsets", {})
        headset = next(
            (h for h in (headsets or [])
             if h.get("id") == HEADSET_ID),
            None
        )

        if not headset or headset.get("status") != "connected":
            raise RuntimeError(
                "Гарнитура не подключена. Проверь Emotiv Launcher."
            )

        # 3. Создаём сессию
        session = await rpc(ws, "createSession", {
            "cortexToken": token,
            "headset": HEADSET_ID,
            "status": "active"
        })

        session_id = session.get("id") if session else None
        if not session_id:
            raise RuntimeError(f"Не создана сессия: {session}")

        print(f"[OK] Сессия: {session_id}")

        # 4. Начинаем запись
        created = await rpc(ws, "createRecord", {
            "cortexToken": token,
            "session": session_id,
            "title": "VR_ML_met",
            "description": "Запись только метрик Cortex",
            "tags": ["VR", "ML", "MET"]
        })

        record = created.get("record", {}) if created else {}
        record_id = record.get("uuid")

        if not record_id:
            raise RuntimeError(
                f"Cortex не вернул UUID записи: {created}"
            )

        print(f"[OK] Запись началась: {record_id}")

        try:
            # 5. Подписываемся только на met
            sub = await rpc(ws, "subscribe", {
                "cortexToken": token,
                "session": session_id,
                "streams": STREAMS
            })

            print("[SUBSCRIBE]", json.dumps(
                sub, ensure_ascii=False, indent=2
            ))

            # Проверяем, что met действительно подписан
            success = sub.get("success", [])
            failure = sub.get("failure", [])

            if any(item.get("streamName") == "met" for item in failure):
                raise RuntimeError(
                    f"Подписка на met не удалась: {failure}"
                )

            if not any(item.get("streamName") == "met" for item in success):
                raise RuntimeError(
                    "Cortex не подтвердил подписку на met."
                )

            # 6. Ждём данные
            print(f"[RECORDING] Записываем met {RECORD_SECONDS} секунд...")

            started = time.monotonic()
            last_status = -1
            met_samples = 0

            while True:
                elapsed = time.monotonic() - started
                remaining = RECORD_SECONDS - elapsed

                if remaining <= 0:
                    break

                try:
                    raw = await asyncio.wait_for(
                        ws.recv(),
                        timeout=min(remaining, 1.0)
                    )
                except asyncio.TimeoutError:
                    raw = None

                if raw:
                    message = json.loads(raw)

                    if "error" in message or "warning" in message:
                        print("[NOTICE]", json.dumps(
                            message, ensure_ascii=False
                        ))
                    elif "met" in message:
                        met_samples += 1

                        # Выводим только факт получения метрик.
                        if met_samples == 1:
                            print("[OK] Получен первый образец met")

                seconds_left = int(RECORD_SECONDS - elapsed)
                if seconds_left != last_status:
                    last_status = seconds_left
                    if seconds_left % 10 == 0:
                        print(
                            f"[RECORDING] Осталось {seconds_left} сек."
                        )

        finally:
            # 7. Останавливаем запись
            print("[INFO] Останавливаем запись...")

            stopped = await rpc(ws, "stopRecord", {
                "cortexToken": token,
                "session": session_id
            })

            print("[STOP RECORD]", json.dumps(
                stopped, ensure_ascii=False
            ))

        print(f"[INFO] Получено уведомлений met: {met_samples}")

        # 8. Ждём уведомление о завершении записи
        deadline = time.monotonic() + 10

        while not record_stopped_warning and time.monotonic() < deadline:
            try:
                raw = await asyncio.wait_for(
                    ws.recv(),
                    timeout=min(1.0, deadline - time.monotonic())
                )
                message = json.loads(raw)

                warning = message.get("warning")
                if isinstance(warning, dict) and warning.get("code") == 30:
                    record_stopped_warning = True

                print("[Cortex message]", json.dumps(
                    message, ensure_ascii=False
                ))

            except asyncio.TimeoutError:
                continue

        # 9. Экспортируем только PM
        exported = await rpc(ws, "exportRecord", {
            "cortexToken": token,
            "recordIds": [record_id],
            "folder": EXPORT_FOLDER,
            "streamTypes": EXPORT_STREAM_TYPES,
            "format": "CSV",
            "version": "V2"
        })

        print("\n[EXPORT RESULT]")
        print(json.dumps(exported, ensure_ascii=False, indent=2))

        if exported and exported.get("success"):
            print(f"\n[OK] Экспорт завершён: {EXPORT_FOLDER}")
        else:
            print(
                "\n[WARNING] Экспорт не подтверждён. "
                "Проверь ответ exportRecord."
            )


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nОстановка по Ctrl+C.")
    except Exception as exc:
        print(f"\n[ERROR] {exc}")
