import os
import logging
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Annotated
import joblib
import pandas as pd
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict, Field

logger = logging.getLogger("uvicorn.error")

FEATURES = [
    "PM.Attention.Raw",
    "PM.Engagement.Raw",
    "PM.Excitement.Raw",
    "PM.Stress.Raw",
    "PM.Relaxation.Raw",
    "PM.Interest.Raw",
    "PM.LongTermExcitement",
]

MODEL_PATH = Path(os.environ.get("MODEL_PATH", "/app/rf_active_model.joblib"))

model = None


class PredictRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    attention: Annotated[float | None, Field(alias="PM.Attention.Raw")] = None
    engagement: Annotated[float | None, Field(alias="PM.Engagement.Raw")] = None
    excitement: Annotated[float | None, Field(alias="PM.Excitement.Raw")] = None
    stress: Annotated[float | None, Field(alias="PM.Stress.Raw")] = None
    relaxation: Annotated[float | None, Field(alias="PM.Relaxation.Raw")] = None
    interest: Annotated[float | None, Field(alias="PM.Interest.Raw")] = None
    long_term_excitement: Annotated[float | None, Field(alias="PM.LongTermExcitement")] = None


class PredictResponse(BaseModel):
    level: str
    probabilities: dict[str, float]


@asynccontextmanager
async def lifespan(app: FastAPI):
    global model
    if not MODEL_PATH.is_file():
        logger.error(f"Файл модели не найден: {MODEL_PATH}")
        raise FileNotFoundError(f"Model file not found: {MODEL_PATH}")
    
    model = joblib.load(MODEL_PATH)
    logger.info(f"Модель успешно загружена из {MODEL_PATH}")
    yield
    model = None
    logger.info("Модель выгружена из памяти")


app = FastAPI(title="Random Forest predictor", lifespan=lifespan)


@app.get("/health")
def health():
    if model is None:
        raise HTTPException(status_code=503, detail="model not loaded")
    return {"status": "ok"}


@app.post("/predict", response_model=PredictResponse)
def predict(payload: PredictRequest):
    received_data = payload.model_dump(by_alias=True)
    logger.info(f"Получен запрос на предсказание: {received_data}")
    
    try:
        frame = pd.DataFrame([received_data], columns=FEATURES)
        level = model.predict(frame)[0]
        proba = model.predict_proba(frame)[0]
        
        probabilities = {
            str(label): float(value) for label, value in zip(model.classes_, proba)
        }
        
        # Логируем результат
        logger.info(f"Результат предсказания: level='{level}', probabilities={probabilities}")
        
        return PredictResponse(level=str(level), probabilities=probabilities)
    
    except Exception as e:
        # Логируем ошибку, если что-то пошло не так во время инференса
        logger.error(f"Ошибка при обработке запроса: {e}")
        raise HTTPException(status_code=500, detail="Internal Server Error")