"""Training pipeline for Aimran Data Science Lab"""

import logging
from pathlib import Path
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier, GradientBoostingClassifier
from sklearn.preprocessing import StandardScaler
from src.data.loaders import DataLoader, Preprocessor
from src.features.engineering import FeatureEngineer, FeatureSelector
from src.models.base_model import BaseModel
from src.monitoring.metrics import ExperimentTracker

logger = logging.getLogger(__name__)


class ModelTrainingPipeline:
    """End-to-end model training pipeline"""

    def __init__(self, config_path: str = None):
        self.config_path = config_path
        self.tracker = ExperimentTracker()
        self.scaler = None
        self.model = None

    def load_and_prepare_data(self, filepath: str) -> tuple:
        """Load and prepare data"""
        logger.info(f"Loading data from {filepath}")
        loader = DataLoader()
        df = loader.load_dataset(Path(filepath).name)

        # Clean data
        logger.info("Cleaning data")
        df = Preprocessor.clean_data(df, drop_na=True)

        # Separate features and target
        X = df.drop('target', axis=1)
        y = df['target']

        return X, y

    def feature_engineering(self, X: pd.DataFrame) -> pd.DataFrame:
        """Apply feature engineering"""
        logger.info("Engineering features")
        engineer = FeatureEngineer()

        # Create interaction features
        X = engineer.create_interaction_features(X, columns=X.columns[:3])

        return X

    def scale_and_select_features(self, X: pd.DataFrame, y: pd.Series) -> pd.DataFrame:
        """Scale features and select top features"""
        logger.info("Scaling and selecting features")

        # Scale features
        X_scaled, self.scaler = Preprocessor.scale_features(X, columns=X.columns)

        # Select top features
        selector = FeatureSelector()
        top_features = selector.select_top_features(X_scaled, y, n_features=10)

        return X_scaled[top_features]

    def train_model(self, X: pd.DataFrame, y: pd.Series, model_type: str = "random_forest"):
        """Train machine learning model"""
        logger.info(f"Training {model_type} model")

        # Split data
        X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

        # Select and train model
        if model_type == "random_forest":
            model = RandomForestClassifier(n_estimators=100, random_state=42)
        elif model_type == "gradient_boosting":
            model = GradientBoostingClassifier(n_estimators=100, random_state=42)
        else:
            raise ValueError(f"Unknown model type: {model_type}")

        model.fit(X_train, y_train)

        # Wrap in base model
        self.model = BaseModel(model_type="classifier")
        self.model.model = model
        self.model.is_trained = True
        self.model.feature_names = X.columns.tolist()

        # Evaluate
        metrics = self.model.evaluate(X_test, y_test, task="classification")

        # Log to MLflow
        self.tracker.log_params({"model_type": model_type, "n_estimators": 100})
        self.tracker.log_metrics(metrics)

        return self.model, metrics

    def save_model(self, filepath: str):
        """Save trained model"""
        if self.model is None:
            raise ValueError("No model to save. Train a model first.")
        self.model.save(filepath)
        logger.info(f"Model saved to {filepath}")

    def run_pipeline(self, data_filepath: str, model_type: str = "random_forest", 
                     save_path: str = "./models/model.pkl"):
        """Run complete pipeline"""
        try:
            # Load and prepare data
            X, y = self.load_and_prepare_data(data_filepath)

            # Feature engineering
            X = self.feature_engineering(X)

            # Scale and select features
            X = self.scale_and_select_features(X, y)

            # Train model
            model, metrics = self.train_model(X, y, model_type=model_type)

            # Save model
            self.save_model(save_path)

            logger.info("Pipeline completed successfully")
            return model, metrics

        except Exception as e:
            logger.error(f"Pipeline failed: {str(e)}")
            raise

        finally:
            self.tracker.end_run()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    # Example usage
    pipeline = ModelTrainingPipeline()
    # pipeline.run_pipeline("data/raw/sample_data.csv")
