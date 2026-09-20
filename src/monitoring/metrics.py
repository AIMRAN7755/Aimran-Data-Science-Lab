"""MLflow experiment tracking and logging utilities"""

try:
    import mlflow
    import mlflow.sklearn
    import mlflow.pytorch
    MLFLOW_AVAILABLE = True
except ImportError:
    MLFLOW_AVAILABLE = False

import logging
from typing import Dict, Any, Optional

logger = logging.getLogger(__name__)


class ExperimentTracker:
    """MLflow experiment tracking wrapper"""

    def __init__(self, tracking_uri: str = "http://localhost:5000", experiment_name: str = "default"):
        self.tracking_uri = tracking_uri
        self.available = False
        if not MLFLOW_AVAILABLE:
            logger.warning("MLflow not installed. Tracking disabled.")
            return
        try:
            mlflow.set_tracking_uri(tracking_uri)
            mlflow.set_experiment(experiment_name)
            self.available = True
            logger.info(f"Initialized MLflow tracking to {tracking_uri}")
        except Exception as e:
            logger.warning(f"MLflow not available: {e}. Tracking disabled.")

    def log_params(self, params: Dict[str, Any]):
        """Log parameters"""
        if not self.available:
            return
        for key, value in params.items():
            mlflow.log_param(key, value)
        logger.info(f"Logged {len(params)} parameters")

    def log_metrics(self, metrics: Dict[str, float], step: int = None):
        """Log metrics"""
        if not self.available:
            return
        for key, value in metrics.items():
            mlflow.log_metric(key, value, step=step)
        logger.info(f"Logged {len(metrics)} metrics")

    def log_model(self, model: Any, artifact_path: str = "models", model_type: str = "sklearn"):
        """Log model artifact"""
        if not self.available:
            return
        if model_type == "sklearn":
            mlflow.sklearn.log_model(model, artifact_path)
        elif model_type == "pytorch":
            mlflow.pytorch.log_model(model, artifact_path)
        logger.info(f"Logged model to {artifact_path}")

    def end_run(self):
        """End current MLflow run"""
        if not self.available:
            return
        mlflow.end_run()
        logger.info("MLflow run ended")


class DataVersionControl:
    """DVC (Data Version Control) utilities"""

    @staticmethod
    def track_dataset(filepath: str, message: str = ""):
        """Track dataset with DVC"""
        import subprocess
        try:
            subprocess.run(["dvc", "add", filepath], check=True)
            logger.info(f"Tracked {filepath} with DVC")
        except subprocess.CalledProcessError as e:
            logger.error(f"Failed to track {filepath}: {str(e)}")

    @staticmethod
    def pull_data():
        """Pull DVC tracked data"""
        import subprocess
        try:
            subprocess.run(["dvc", "pull"], check=True)
            logger.info("Pulled DVC tracked data")
        except subprocess.CalledProcessError as e:
            logger.error(f"Failed to pull DVC data: {str(e)}")
