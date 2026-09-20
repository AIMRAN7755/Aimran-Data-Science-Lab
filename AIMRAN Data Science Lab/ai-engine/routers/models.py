"""Model training and evaluation endpoints."""

import time
from fastapi import APIRouter
from schemas import TrainRequest, TrainResult, EvalRequest, EvalResult, PredictRequest, PredictResult

router = APIRouter()


@router.post("/train", response_model=TrainResult)
async def train_model(request: TrainRequest):
    start = time.monotonic()

    # Placeholder — real implementation will use scikit-learn / PyTorch
    model_path = f"/tmp/aimran_models/{request.algorithm}_{int(time.time())}.pkl"
    metrics = {"accuracy": 0.0, "f1_score": 0.0}

    elapsed = time.monotonic() - start
    return TrainResult(
        model_path=model_path,
        metrics=metrics,
        training_duration_seconds=elapsed,
    )


@router.post("/evaluate", response_model=EvalResult)
async def evaluate_model(request: EvalRequest):
    return EvalResult(metrics={"accuracy": 0.0}, test_sample_count=0)


@router.post("/predict", response_model=PredictResult)
async def predict(request: PredictRequest):
    return PredictResult(predictions=[], probabilities=None)
