"""Gradient Boosting model wrapper (sklearn GBM)."""

from __future__ import annotations

import logging
from typing import Any

import pandas as pd

from src.models.base_model import BaseModel

logger = logging.getLogger(__name__)


class GradientBoostingModel(BaseModel):
    """
    Scikit-learn Gradient Boosting wrapper (classification or regression).

    Parameters
    ----------
    task : str
        ``"classification"`` (default) or ``"regression"``.
    **kwargs
        Passed to the underlying sklearn GradientBoosting* class.
    """

    def __init__(self, task: str = "classification", **kwargs: Any):
        super().__init__(model_type=task)
        self._kwargs = kwargs
        self._init_model()

    def _init_model(self) -> None:
        if self.model_type == "regression":
            from sklearn.ensemble import GradientBoostingRegressor
            self.model = GradientBoostingRegressor(**self._kwargs)
        else:
            from sklearn.ensemble import GradientBoostingClassifier
            self.model = GradientBoostingClassifier(**self._kwargs)

    def train(self, X: pd.DataFrame, y: pd.Series) -> "GradientBoostingModel":
        super().train(X, y)
        self.evaluate(X, y, task=self.model_type)
        logger.info(
            "GradientBoostingModel (%s) trained — metrics: %s", self.model_type, self.metrics
        )
        return self

    @property
    def task(self) -> str:
        return self.model_type

    @task.setter
    def task(self, value: str) -> None:
        self.model_type = value
