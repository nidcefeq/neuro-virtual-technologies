using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using EmotivUnityPlugin;

public class EmotivMet : MonoBehaviour
{
    private EmotivUnityItf emotiv;

    private bool querySent;
    private bool sessionRequested;
    private bool subscriptionRequested;

    private float lastStatusLogTime;
    private float lastDataLogTime;
    private float lastQueryTime;
    private float sessionRequestTime;

    private int sessionAttempts;

    private const int MaxSessionAttempts = 3;
    private const float QueryRetryInterval = 5f;
    private const float SessionTimeout = 10f;

    [Header("Cortex credentials")]
    public string clientId = "YOUR_CLIENT_ID";
    public string clientSecret = "YOUR_CLIENT_SECRET";

    [Header("Cortex")]
    public string appName = "MyUnityApp";
    public string cortexUrl = "wss://127.0.0.1:6868";

    [Header("Headset")]
    public string headsetId = "EPOCX-F416A4AD";

    [Header("MET logging")]
    [Min(0.1f)]
    public float logInterval = 1f;

    [Header("ML Predictor")]
    public string predictorUrl = "http://127.0.0.1:8000/predict";
    private int metSampleCounter = 0;
    private const int PredictEveryNSamples = 3; // Отправлять каждый 3-й сэмпл

    [Serializable]
    public class PredictResponse
    {
        public string level;
        // probabilities можно добавить сюда же, если понадобятся
    }

    private void Start()
    {
        emotiv = EmotivUnityItf.Instance;

        if (emotiv == null)
        {
            Debug.LogError("[Emotiv] EmotivUnityItf.Instance не найден");
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(appName) ||
            clientId == "YOUR_CLIENT_ID" ||
            clientSecret == "YOUR_CLIENT_SECRET")
        {
            Debug.LogError("[Emotiv] Укажи clientId, clientSecret и appName");
            enabled = false;
            return;
        }

        emotiv.ErrorMsgReceived += OnCortexError;

        try
        {
            emotiv.Init(clientId, clientSecret, appName, false, true, cortexUrl);
            emotiv.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Emotiv] Ошибка запуска: {e.Message}");
        }
    }

    private void Update()
    {
        if (emotiv == null) return;

        if (!emotiv.IsAuthorizedOK)
        {
            LogStatus();
            return;
        }

        if (!querySent)
        {
            QueryHeadsets();
            LogStatus();
            return;
        }

        if (emotiv.IsSessionCreated)
        {
            if (!subscriptionRequested)
                SubscribeToMET();

            if (subscriptionRequested)
                LogMET();
        }
        else
        {
            if (sessionRequested && Time.time - sessionRequestTime >= SessionTimeout)
            {
                Debug.LogWarning($"[Emotiv] Таймаут создания сессии. Попытка {sessionAttempts}/{MaxSessionAttempts}");
                sessionRequested = false;
            }

            if (!sessionRequested && sessionAttempts < MaxSessionAttempts)
            {
                TryCreateSession();
            }
        }

        LogStatus();
    }

    private void QueryHeadsets()
    {
        if (Time.time - lastQueryTime < QueryRetryInterval && lastQueryTime > 0f)
            return;

        lastQueryTime = Time.time;

        try
        {
            emotiv.QueryHeadsets();
            querySent = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Emotiv] QueryHeadsets: {e.Message}");
            querySent = false;
        }
    }

    private void TryCreateSession()
    {
        try
        {
            List<Headset> detected = emotiv.GetDetectedHeadsets();

            if (detected == null || detected.Count == 0)
            {
                if (Time.time - lastQueryTime >= QueryRetryInterval)
                    querySent = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(headsetId))
            {
                Debug.LogError("[Emotiv] Не указан headsetId");
                enabled = false;
                return;
            }

            sessionAttempts++;
            sessionRequested = true;
            sessionRequestTime = Time.time;

            emotiv.CreateSessionWithHeadset(headsetId);
        }
        catch (Exception e)
        {
            sessionRequested = false;
            Debug.LogError($"[Emotiv] Ошибка создания сессии: {e.Message}");
        }
    }

    private void SubscribeToMET()
    {
        if (subscriptionRequested || !emotiv.IsSessionCreated) return;

        try
        {
            emotiv.DataSubLog = "";
            emotiv.SubscribeData(new List<string> { "met" });
            subscriptionRequested = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Emotiv] Ошибка подписки на MET: {e.Message}");
        }
    }

    private void LogMET()
    {
        if (Time.time - lastDataLogTime < logInterval) return;
        lastDataLogTime = Time.time;

        try
        {
            if (emotiv.GetNumberPMSamples() <= 0) return;

            List<string> labels = emotiv.GetPMLists();
            if (labels == null || labels.Count == 0) return;

            List<string> values = new List<string>();
            Dictionary<string, double> metDataDict = new Dictionary<string, double>();

            foreach (string label in labels)
            {
                double value = emotiv.GetPMData(label);
                values.Add($"{label}={value:F4}");
                metDataDict[label] = value;
            }

            Debug.Log($"[Emotiv][MET] {string.Join("; ", values)}");

            metSampleCounter++;
            if (metSampleCounter >= PredictEveryNSamples)
            {
                metSampleCounter = 0;
                StartCoroutine(SendPredictionRequest(metDataDict));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Emotiv][MET] Ошибка чтения: {e.Message}");
        }
    }

    private IEnumerator SendPredictionRequest(Dictionary<string, double> metDataDict)
    {
        // 1. Маппинг ключей Emotiv в ключи FastAPI
        Dictionary<string, string> keyMapping = new Dictionary<string, string>
        {
            { "attention", "PM.Attention.Raw" },
            { "eng",       "PM.Engagement.Raw" },
            { "exc",       "PM.Excitement.Raw" },
            { "str",       "PM.Stress.Raw" },
            { "rel",       "PM.Relaxation.Raw" },
            { "int",       "PM.Interest.Raw" },
            { "lex",       "PM.LongTermExcitement" }
        };

        // 2. Формируем JSON вручную
        string jsonPayload = "{";
        bool first = true;

        foreach (var kvp in metDataDict)
        {
            string apiKey = keyMapping.ContainsKey(kvp.Key) ? keyMapping[kvp.Key] : kvp.Key;

            if (!first) jsonPayload += ",";
            jsonPayload += $"\"{apiKey}\": {kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            first = false;
        }
        jsonPayload += "}";

        // 3. Отправляем запрос
        using (UnityWebRequest request = new UnityWebRequest(predictorUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                try
                {
                    PredictResponse response = JsonUtility.FromJson<PredictResponse>(responseJson);

                    if (response != null && !string.IsNullOrEmpty(response.level))
                    {
                        Debug.Log($"[ML Predict] Ответ сервера: {response.level}");

                        // >>> Единственное смысловое изменение: сообщаем игре новый уровень сложности.
                        GameDifficulty.SetFromStressLevel(response.level);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ML Predict] Ошибка парсинга ответа: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[ML Predict] Ошибка HTTP запроса: {request.error}");
            }
        }
    }

    private void LogStatus()
    {
        if (Time.time - lastStatusLogTime < 5f) return;
        lastStatusLogTime = Time.time;
    }

    private void OnCortexError(object sender, ErrorMsgEventArgs error)
    {
        Debug.LogError($"[Emotiv][Cortex Error] Code={error.Code}; Method={error.MethodName}; Message={error.MessageError}");
    }

    private void OnApplicationQuit()
    {
        if (emotiv == null) return;

        try
        {
            emotiv.ErrorMsgReceived -= OnCortexError;
            emotiv.Stop();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Emotiv] Ошибка остановки: {e.Message}");
        }
    }

}