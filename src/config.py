import os
import logging
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

class Config:
    """Base configuration"""
    DEBUG = os.getenv("DEBUG", "False") == "True"
    ENVIRONMENT = os.getenv("ENVIRONMENT", "development")

    # API
    API_HOST = os.getenv("API_HOST", "0.0.0.0")
    API_PORT = int(os.getenv("API_PORT", 8000))

    # Database
    DB_HOST = os.getenv("DB_HOST", "localhost")
    DB_PORT = int(os.getenv("DB_PORT", 5432))
    DB_USER = os.getenv("DB_USER", "airflow")
    DB_PASSWORD = os.getenv("DB_PASSWORD", "airflow")
    DB_NAME = os.getenv("DB_NAME", "airflow")

    # MLflow
    MLFLOW_TRACKING_URI = os.getenv("MLFLOW_TRACKING_URI", "http://localhost:5000")
    MLFLOW_BACKEND_STORE_URI = os.getenv("MLFLOW_BACKEND_STORE_URI", "sqlite:///mlflow.db")
    MLFLOW_ARTIFACT_ROOT = os.getenv("MLFLOW_ARTIFACT_ROOT", "./mlflow_artifacts")

    # Cloud Storage
    AWS_S3_BUCKET = os.getenv("AWS_S3_BUCKET", "")
    GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "")

    # Data Paths
    DATA_RAW_PATH = os.getenv("DATA_RAW_PATH", "./data/raw")
    DATA_PROCESSED_PATH = os.getenv("DATA_PROCESSED_PATH", "./data/processed")
    DATA_INTERIM_PATH = os.getenv("DATA_INTERIM_PATH", "./data/interim")
    MODELS_PATH = os.getenv("MODELS_PATH", "./models")

    # Logging
    LOG_LEVEL = os.getenv("LOG_LEVEL", "INFO")
    LOG_FORMAT = os.getenv("LOG_FORMAT", "json")


class DevelopmentConfig(Config):
    """Development configuration"""
    DEBUG = True
    ENVIRONMENT = "development"


class ProductionConfig(Config):
    """Production configuration"""
    DEBUG = False
    ENVIRONMENT = "production"

    def __init__(self):
        # Fail-fast: refuse to boot production on the insecure dev-default DB password.
        if os.getenv("DB_PASSWORD", "airflow") == "airflow":
            raise RuntimeError(
                "DB_PASSWORD is unset or uses the insecure development default 'airflow'. "
                "Set a strong DB_PASSWORD environment variable before running in production."
            )


class TestingConfig(Config):
    """Testing configuration"""
    DEBUG = True
    ENVIRONMENT = "testing"
    TESTING = True


def get_config():
    """Get appropriate configuration based on environment"""
    env = os.getenv("ENVIRONMENT", "development")
    if env == "production":
        return ProductionConfig()
    elif env == "testing":
        return TestingConfig()
    return DevelopmentConfig()


def setup_logging(config):
    """Setup application logging"""
    logging.basicConfig(
        level=getattr(logging, config.LOG_LEVEL),
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    )
    return logging.getLogger(__name__)


config = get_config()
logger = setup_logging(config)
