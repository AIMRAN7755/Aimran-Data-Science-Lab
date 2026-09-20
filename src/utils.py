"""Core utilities for Aimran Data Science Lab"""

import numpy as np
import pandas as pd
from typing import Any, Dict, List, Optional, Union
import json
import pickle
from pathlib import Path


class DataLoader:
    """Unified data loading interface"""

    @staticmethod
    def load_csv(filepath: str, **kwargs) -> pd.DataFrame:
        """Load CSV file"""
        return pd.read_csv(filepath, **kwargs)

    @staticmethod
    def load_parquet(filepath: str, **kwargs) -> pd.DataFrame:
        """Load Parquet file"""
        return pd.read_parquet(filepath, **kwargs)

    @staticmethod
    def load_json(filepath: str) -> Dict:
        """Load JSON file"""
        with open(filepath, 'r') as f:
            return json.load(f)

    @staticmethod
    def save_csv(df: pd.DataFrame, filepath: str, **kwargs) -> None:
        """Save DataFrame to CSV"""
        df.to_csv(filepath, index=False, **kwargs)

    @staticmethod
    def save_parquet(df: pd.DataFrame, filepath: str, **kwargs) -> None:
        """Save DataFrame to Parquet"""
        df.to_parquet(filepath, index=False, **kwargs)

    @staticmethod
    def save_model(model: Any, filepath: str) -> None:
        """Serialize and save model"""
        Path(filepath).parent.mkdir(parents=True, exist_ok=True)
        with open(filepath, 'wb') as f:
            pickle.dump(model, f)

    @staticmethod
    def load_model(filepath: str) -> Any:
        """Load serialized model"""
        with open(filepath, 'rb') as f:
            return pickle.load(f)


class DataPreprocessor:
    """Data preprocessing utilities"""

    @staticmethod
    def handle_missing_values(df: pd.DataFrame, strategy: str = 'mean') -> pd.DataFrame:
        """Handle missing values in DataFrame"""
        df_copy = df.copy()

        if strategy == 'mean':
            return df_copy.fillna(df_copy.mean(numeric_only=True))
        elif strategy == 'median':
            return df_copy.fillna(df_copy.median(numeric_only=True))
        elif strategy == 'drop':
            return df_copy.dropna()
        else:
            raise ValueError(f"Unknown strategy: {strategy}")

    @staticmethod
    def remove_outliers(df: pd.DataFrame, columns: List[str], method: str = 'iqr') -> pd.DataFrame:
        """Remove outliers from DataFrame"""
        df_copy = df.copy()

        for col in columns:
            if method == 'iqr':
                Q1 = df_copy[col].quantile(0.25)
                Q3 = df_copy[col].quantile(0.75)
                IQR = Q3 - Q1
                df_copy = df_copy[(df_copy[col] >= Q1 - 1.5 * IQR) & 
                                  (df_copy[col] <= Q3 + 1.5 * IQR)]
            elif method == 'zscore':
                z_scores = np.abs((df_copy[col] - df_copy[col].mean()) / df_copy[col].std())
                df_copy = df_copy[z_scores < 3]

        return df_copy

    @staticmethod
    def normalize(df: pd.DataFrame, columns: List[str]) -> pd.DataFrame:
        """Normalize numeric columns to 0-1 range"""
        df_copy = df.copy()
        for col in columns:
            min_val = df_copy[col].min()
            max_val = df_copy[col].max()
            df_copy[col] = (df_copy[col] - min_val) / (max_val - min_val)
        return df_copy


class ModelEvaluator:
    """Model evaluation utilities"""

    @staticmethod
    def get_metrics(y_true: np.ndarray, y_pred: np.ndarray, task: str = 'classification') -> Dict:
        """Calculate performance metrics"""
        from sklearn.metrics import (accuracy_score, precision_score, recall_score, 
                                     f1_score, mean_squared_error, r2_score, roc_auc_score)

        metrics = {}

        if task == 'classification':
            metrics['accuracy'] = accuracy_score(y_true, y_pred)
            metrics['precision'] = precision_score(y_true, y_pred, average='weighted', zero_division=0)
            metrics['recall'] = recall_score(y_true, y_pred, average='weighted', zero_division=0)
            metrics['f1'] = f1_score(y_true, y_pred, average='weighted', zero_division=0)

        elif task == 'regression':
            metrics['mse'] = mean_squared_error(y_true, y_pred)
            metrics['rmse'] = np.sqrt(metrics['mse'])
            metrics['r2'] = r2_score(y_true, y_pred)

        return metrics


def check_data_quality(df: pd.DataFrame) -> Dict:
    """Check data quality metrics"""
    return {
        'shape': df.shape,
        'missing_values': df.isnull().sum().to_dict(),
        'missing_percentage': (df.isnull().sum() / len(df) * 100).to_dict(),
        'duplicates': df.duplicated().sum(),
        'data_types': df.dtypes.to_dict(),
    }
