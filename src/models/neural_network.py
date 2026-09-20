"""Neural Network model wrapper (sklearn MLP — no heavy DL dependency)."""

from __future__ import annotations

import logging
from typing import Any

import pandas as pd

from src.models.base_model import BaseModel

logger = logging.getLogger(__name__)


class NeuralNetworkModel(BaseModel):
    """
    Scikit-learn MLP (Multi-layer Perceptron) wrapper.

    Uses ``MLPClassifier`` / ``MLPRegressor`` so there is no torch/tensorflow
    dependency at this layer.  Swap the underlying ``self.model`` for a
    PyTorch / TF estimator if needed — the interface stays identical.

    Parameters
    ----------
    task : str
        ``"classification"`` (default) or ``"regression"``.
    hidden_layer_sizes : tuple
        Sizes of the hidden layers (default ``(128, 64)``).
    max_iter : int
        Maximum training epochs (default ``500``).
    **kwargs
        Additional keyword args forwarded to the MLP constructor.
    """

    def __init__(
        self,
        task: str = "classification",
        hidden_layer_sizes: tuple = (128, 64),
        max_iter: int = 500,
        **kwargs: Any,
    ):
        super().__init__(model_type=task)
        self._kwargs = dict(
            hidden_layer_sizes=hidden_layer_sizes,
            max_iter=max_iter,
            **kwargs,
        )
        self._init_model()

    def _init_model(self) -> None:
        if self.model_type == "regression":
            from sklearn.neural_network import MLPRegressor
            self.model = MLPRegressor(**self._kwargs)
        else:
            from sklearn.neural_network import MLPClassifier
            self.model = MLPClassifier(**self._kwargs)

    def train(self, X: pd.DataFrame, y: pd.Series) -> "NeuralNetworkModel":
        super().train(X, y)
        self.evaluate(X, y, task=self.model_type)
        logger.info(
            "NeuralNetworkModel (%s) trained — metrics: %s", self.model_type, self.metrics
        )
        return self

    @property
    def task(self) -> str:
        return self.model_type

    @task.setter
    def task(self, value: str) -> None:
        self.model_type = value
