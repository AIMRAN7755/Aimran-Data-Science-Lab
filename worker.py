import os
import logging

logger = logging.getLogger(__name__)

try:
    from celery import Celery
except ImportError:
    raise ImportError(
        "Celery is required for async task execution. "
        "Install it with: pip install celery[redis]"
    )

redis_url = os.getenv("REDIS_URL", "redis://localhost:6379/0")

celery_app = Celery(
    "aimran_workers",
    broker=redis_url,
    backend=redis_url,
)

celery_app.conf.update(
    task_serializer="json",
    accept_content=["json"],
    result_serializer="json",
    timezone="UTC",
    enable_utc=True,
    task_track_started=True,
    task_time_limit=30 * 60,
)


@celery_app.task(bind=True, max_retries=3)
def train_model_task(self, model_type: str, model_id: str, data_path: str, params: dict = None):
    from src.models.random_forest import RandomForestModel
    from src.models.gradient_boosting import GradientBoostingModel
    from src.models.registry import register_model
    import pandas as pd

    try:
        if model_type == "random_forest":
            model = RandomForestModel(**(params or {}))
        elif model_type == "gradient_boosting":
            model = GradientBoostingModel(**(params or {}))
        else:
            raise ValueError(f"Unknown model type: {model_type}")

        df = pd.read_csv(data_path)
        target_col = "target" if "target" in df.columns else df.columns[-1]
        X = df.drop(columns=[target_col])
        y = df[target_col]

        model.train(X, y)
        register_model(model_id, model)

        return {
            "status": "completed",
            "model_id": model_id,
            "model_type": model_type,
            "rows": len(df),
            "features": X.shape[1],
            "metrics": model.metrics,
        }
    except Exception as exc:
        raise self.retry(exc=exc, countdown=60)


@celery_app.task(bind=True, max_retries=3)
def process_dataset_task(self, input_path: str, output_path: str, options: dict = None):
    from src.data.loaders import DataLoader, Preprocessor
    import pandas as pd

    try:
        loader = DataLoader()
        df = loader.load_dataset(input_path, use_chunking=True)

        if options:
            if options.get("clean"):
                df = Preprocessor.clean_data(df, drop_na=options.get("drop_na", False))
            if options.get("optimize_dtypes"):
                df = Preprocessor.optimize_dtypes(df)

        df.to_csv(output_path, index=False)
        return {"status": "completed", "input": input_path, "output": output_path, "rows": len(df)}
    except Exception as exc:
        raise self.retry(exc=exc, countdown=60)

logger.info("Celery worker initialized")
