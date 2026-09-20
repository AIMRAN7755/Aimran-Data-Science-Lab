"""Feature engineering module for Aimran Data Science Lab"""

import pandas as pd
import numpy as np
from typing import List, Dict, Optional
import logging

logger = logging.getLogger(__name__)


class FeatureEngineer:
    """Feature engineering and transformation operations"""

    @staticmethod
    def create_polynomial_features(df: pd.DataFrame, columns: List[str], degree: int = 2) -> pd.DataFrame:
        """Create polynomial features"""
        from sklearn.preprocessing import PolynomialFeatures

        df_poly = df.copy()
        poly = PolynomialFeatures(degree=degree, include_bias=False)

        poly_features = poly.fit_transform(df[columns])
        poly_feature_names = poly.get_feature_names_out(columns)

        for name, feature in zip(poly_feature_names, poly_features.T):
            df_poly[name] = feature

        logger.info(f"Created {len(poly_feature_names)} polynomial features")
        return df_poly

    @staticmethod
    def create_interaction_features(df: pd.DataFrame, columns: List[str]) -> pd.DataFrame:
        """Create interaction features between columns"""
        df_interact = df.copy()

        for i, col1 in enumerate(columns):
            for col2 in columns[i+1:]:
                feature_name = f"{col1}_x_{col2}"
                df_interact[feature_name] = df[col1] * df[col2]

        logger.info(f"Created interaction features from {len(columns)} columns")
        return df_interact

    @staticmethod
    def create_aggregate_features(df: pd.DataFrame, group_col: str, 
                                  agg_cols: List[str], agg_funcs: List[str]) -> pd.DataFrame:
        """Create aggregated features by group"""
        df_agg = df.copy()

        for agg_col in agg_cols:
            for func in agg_funcs:
                feature_name = f"{agg_col}_{func}_by_{group_col}"
                df_agg[feature_name] = df_agg.groupby(group_col)[agg_col].transform(func)

        logger.info(f"Created aggregated features for {len(agg_cols)} columns")
        return df_agg

    @staticmethod
    def create_lag_features(df: pd.DataFrame, columns: List[str], lags: List[int]) -> pd.DataFrame:
        """Create lag features for time series"""
        df_lag = df.copy()

        for col in columns:
            for lag in lags:
                feature_name = f"{col}_lag_{lag}"
                df_lag[feature_name] = df[col].shift(lag)

        logger.info(f"Created lag features with lags {lags}")
        return df_lag


class FeatureSelector:
    """Feature selection operations"""

    @staticmethod
    def select_top_features(X: pd.DataFrame, y: pd.Series, n_features: int = 10, 
                           method: str = 'mutual_info') -> List[str]:
        """Select top features based on mutual information or correlation"""
        if method == 'mutual_info':
            from sklearn.feature_selection import mutual_info_classif
            scores = mutual_info_classif(X, y, random_state=42)
        elif method == 'correlation':
            scores = np.abs(X.corrwith(y))
        else:
            raise ValueError(f"Unknown method: {method}")

        top_features = X.columns[np.argsort(scores)[-n_features:]].tolist()
        logger.info(f"Selected top {n_features} features using {method}")
        return top_features

    @staticmethod
    def remove_low_variance_features(df: pd.DataFrame, threshold: float = 0.01) -> pd.DataFrame:
        """Remove features with low variance"""
        from sklearn.feature_selection import VarianceThreshold

        selector = VarianceThreshold(threshold=threshold)
        df_selected = selector.fit_transform(df)

        selected_features = df.columns[selector.get_support()].tolist()
        logger.info(f"Removed {df.shape[1] - len(selected_features)} low-variance features")

        return pd.DataFrame(df_selected, columns=selected_features)
