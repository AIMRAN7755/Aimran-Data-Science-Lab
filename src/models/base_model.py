"""
Base model abstraction for the Aimran Data Science Lab.

All concrete model implementations inherit from BaseModel, which provides:
- Unified train / predict / evaluate interface
- Feature name tracking
- joblib serialisation (save / load)
- Lightweight metrics storage
"""

from __future__ import annotations

import logging
from pathlib import Path
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)


class BaseModel:
    """
    Abstract base wrapper around any scikit-learn–compatible estimator.

    Attributes
    ----------
    model_type : str
        Either ``"classifier"`` or ``"regressor"``.
    task : str
        Same as model_type but kept for API compatibility.
    model : Any
        The underlying estimator (set by subclasses or externally).
    is_trained : bool
        Becomes ``True`` after :meth:`train` completes successfully.
    feature_names : List[str]
        Column names of the training feature matrix.
    metrics : Dict[str, float]
        Evaluation metrics populated after training.
    """

    def __init__(self, model_type: str = "classifier", **kwargs):
        self.model_type = model_type
        self.task = model_type          # convenience alias
        self.model: Optional[Any] = None
        self.is_trained: bool = False
        self.feature_names: List[str] = []
        self.metrics: Dict[str, float] = {}

    # ------------------------------------------------------------------
    # Training
    # ------------------------------------------------------------------

    def train(self, X: pd.DataFrame, y: pd.Series) -> "BaseModel":
        """Fit the underlying estimator and record feature names."""
        if self.model is None:
            raise RuntimeError("No underlying model set. Use a concrete subclass.")
        self.feature_names = list(X.columns)
        self.model.fit(X, y)
        self.is_trained = True
        logger.info("Model trained on %d rows × %d features", len(X), X.shape[1])
        return self

    # ------------------------------------------------------------------
    # Inference
    # ------------------------------------------------------------------

    def predict(self, X: pd.DataFrame) -> np.ndarray:
        """Return class predictions or regression values."""
        if not self.is_trained or self.model is None:
            raise RuntimeError("Model is not trained yet.")
        return self.model.predict(X)

    def predict_proba(self, X: pd.DataFrame) -> Optional[np.ndarray]:
        """Return class-probability matrix, or None if not supported."""
        if not self.is_trained or self.model is None:
            return None
        if hasattr(self.model, "predict_proba"):
            try:
                return self.model.predict_proba(X)
            except Exception as exc:
                logger.warning("predict_proba failed: %s", exc)
        return None

    # ------------------------------------------------------------------
    # Evaluation
    # ------------------------------------------------------------------

    def evaluate(self, X: pd.DataFrame, y: pd.Series, task: str = "classification") -> Dict[str, float]:
        """
        Compute evaluation metrics.

        Parameters
        ----------
        X : pd.DataFrame
            Feature matrix.
        y : pd.Series
            True labels / values.
        task : str
            ``"classification"`` or ``"regression"``.

        Returns
        -------
        Dict[str, float]
            For classification: accuracy, precision, recall, f1.
            For regression: mse, rmse, mae, r2.
        """
        from sklearn import metrics as sk_metrics

        preds = self.predict(X)

        if task == "classification":
            self.metrics = {
                "accuracy":  float(sk_metrics.accuracy_score(y, preds)),
                "precision": float(sk_metrics.precision_score(y, preds, average="weighted", zero_division=0)),
                "recall":    float(sk_metrics.recall_score(y, preds, average="weighted", zero_division=0)),
                "f1":        float(sk_metrics.f1_score(y, preds, average="weighted", zero_division=0)),
            }
        else:
            mse = float(sk_metrics.mean_squared_error(y, preds))
            self.metrics = {
                "mse":  mse,
                "rmse": float(np.sqrt(mse)),
                "mae":  float(sk_metrics.mean_absolute_error(y, preds)),
                "r2":   float(sk_metrics.r2_score(y, preds)),
            }

        logger.info("Evaluation (%s): %s", task, self.metrics)
        return self.metrics

    # ------------------------------------------------------------------
    # Feature importance
    # ------------------------------------------------------------------

    def get_feature_importance(self) -> Optional[np.ndarray]:
        """
        Return feature importances from the underlying estimator.

        Returns
        -------
        np.ndarray or None
            Array of shape ``(n_features,)`` or ``None`` if not supported.
        """
        if self.model is None:
            return None
        if hasattr(self.model, "feature_importances_"):
            return self.model.feature_importances_
        if hasattr(self.model, "coef_"):
            coef = self.model.coef_
            return np.abs(coef).mean(axis=0) if coef.ndim > 1 else np.abs(coef)
        return None

    # ------------------------------------------------------------------
    # Serialisation
    # ------------------------------------------------------------------

    def save(self, filepath: str) -> None:
        """Persist the model to disk using :mod:`joblib`."""
        import joblib

        path = Path(filepath)
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "model":         self.model,
            "model_type":    self.model_type,
            "task":          self.task,
            "is_trained":    self.is_trained,
            "feature_names": self.feature_names,
            "metrics":       self.metrics,
            "class":         type(self).__name__,
        }
        joblib.dump(payload, filepath, compress=3)
        logger.info("Model saved to %s", filepath)

    def load(self, filepath: str) -> "BaseModel":
        """Restore a model from disk."""
        import joblib

        payload = joblib.load(filepath)
        self.model = payload["model"]
        self.model_type = payload.get("model_type", self.model_type)
        self.task = payload.get("task", self.task)
        self.is_trained = payload.get("is_trained", False)
        self.feature_names = payload.get("feature_names", [])
        self.metrics = payload.get("metrics", {})
        logger.info("Model loaded from %s", filepath)
        return self

    # ------------------------------------------------------------------
    # Repr
    # ------------------------------------------------------------------

    def __repr__(self) -> str:
        return (
            f"{type(self).__name__}("
            f"task={self.model_type!r}, "
            f"trained={self.is_trained}, "
            f"features={len(self.feature_names)})"
        )
