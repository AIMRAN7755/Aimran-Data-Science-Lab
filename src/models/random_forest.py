"""Random Forest model wrapper."""

from __future__ import annotations

import logging
from typing import Any, Dict

import pandas as pd

from src.models.base_model import BaseModel

logger = logging.getLogger(__name__)


class RandomForestModel(BaseModel):
    """
    Scikit-learn Random Forest wrapper supporting classification and regression.

    Parameters
    ----------
    task : str
        ``"classification"`` (default) or ``"regression"``.
    **kwargs
        Forwarded verbatim to the underlying sklearn estimator
        (e.g. ``n_estimators``, ``max_depth``, ``random_state``).
    """

    def __init__(self, task: str = "classification", **kwargs: Any):
        super().__init__(model_type=task)
        self._kwargs = kwargs
        self._init_model()

    def _init_model(self) -> None:
        if self.model_type == "regression":
            from sklearn.ensemble import RandomForestRegressor
            self.model = RandomForestRegressor(**self._kwargs)
        else:
            from sklearn.ensemble import RandomForestClassifier
            self.model = RandomForestClassifier(**self._kwargs)

    def train(self, X: pd.DataFrame, y: pd.Series) -> "RandomForestModel":
        """Fit the forest and compute in-sample metrics."""
        super().train(X, y)
        self.evaluate(X, y, task=self.model_type)
        logger.info(
            "RandomForestModel (%s) trained — metrics: %s", self.model_type, self.metrics
        )
        return self

    # task property kept for API compatibility
    @property
    def task(self) -> str:
        return self.model_type

    @task.setter
    def task(self, value: str) -> None:
        self.model_type = value
