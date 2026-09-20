"""
ModelStore — versioned metadata layer on top of the flat MODEL_REGISTRY.

Keeps a history of model registrations, tracks the champion version for
each model ID, and provides rich metadata queries without any external DB.
"""

from __future__ import annotations

import logging
import time
from copy import deepcopy
from typing import Any, Dict, List, Optional

logger = logging.getLogger(__name__)


class ModelVersion:
    """Snapshot of a model registration event."""

    def __init__(
        self,
        version: int,
        model: Any,
        task: str,
        metrics: Dict[str, float],
        tags: Dict[str, str],
        registered_at: float,
    ):
        self.version = version
        self.model = model
        self.task = task
        self.metrics = metrics
        self.tags = tags
        self.registered_at = registered_at

    def to_dict(self) -> Dict[str, Any]:
        return {
            "version": self.version,
            "task": self.task,
            "metrics": self.metrics,
            "tags": self.tags,
            "registered_at": self.registered_at,
        }


class ModelStore:
    """
    Lightweight versioned model store.

    Usage::

        store = ModelStore()
        store.register("my_model", model, task="classification", metrics={"accuracy": 0.95})
        champion = store.promote_champion("my_model")
        meta = store.get_metadata("my_model")
    """

    def __init__(self):
        # model_id -> list of ModelVersion (chronological)
        self._versions: Dict[str, List[ModelVersion]] = {}
        # model_id -> champion version number
        self._champions: Dict[str, int] = {}

    # ------------------------------------------------------------------
    # Write operations
    # ------------------------------------------------------------------

    def register(
        self,
        model_id: str,
        model: Any,
        task: str = "classification",
        metrics: Optional[Dict[str, float]] = None,
        tags: Optional[Dict[str, str]] = None,
    ) -> ModelVersion:
        """
        Register a new version of a model.

        Returns the created :class:`ModelVersion`.
        """
        if model_id not in self._versions:
            self._versions[model_id] = []

        version_num = len(self._versions[model_id]) + 1
        mv = ModelVersion(
            version=version_num,
            model=model,
            task=task,
            metrics=metrics or {},
            tags=tags or {},
            registered_at=time.time(),
        )
        self._versions[model_id].append(mv)
        logger.info(
            "ModelStore: registered '%s' v%d (task=%s, metrics=%s)",
            model_id, version_num, task, metrics,
        )
        return mv

    def promote_champion(self, model_id: str) -> Dict[str, Any]:
        """
        Promote the latest version of *model_id* to champion.

        Returns a dict with champion version info.

        Raises
        ------
        KeyError
            If *model_id* has no registered versions.
        """
        versions = self._versions.get(model_id)
        if not versions:
            raise KeyError(f"No versions registered for model '{model_id}'")
        champion_version = versions[-1]
        self._champions[model_id] = champion_version.version
        logger.info(
            "ModelStore: promoted '%s' v%d to champion", model_id, champion_version.version
        )
        return {
            "model_id": model_id,
            "champion_version": champion_version.version,
            "promoted_at": time.time(),
            "metrics": champion_version.metrics,
        }

    # ------------------------------------------------------------------
    # Read operations
    # ------------------------------------------------------------------

    def get_metadata(self, model_id: str) -> Optional[Dict[str, Any]]:
        """Return metadata dict for *model_id*, or ``None`` if unknown."""
        versions = self._versions.get(model_id)
        if not versions:
            return None
        latest = versions[-1]
        return {
            "model_id": model_id,
            "model_type": type(latest.model).__name__,
            "task": latest.task,
            "version_count": len(versions),
            "champion_version": self._champions.get(model_id),
            "latest_version": latest.version,
            "latest_metrics": latest.metrics,
            "tags": latest.tags,
            "is_trained": getattr(latest.model, "is_trained", True),
            "feature_names": getattr(latest.model, "feature_names", []),
            "metrics": getattr(latest.model, "metrics", latest.metrics),
        }

    def list_all(self) -> Dict[str, Dict[str, Any]]:
        """Return metadata for every known model."""
        return {mid: self.get_metadata(mid) for mid in self._versions}

    def get_versions(self, model_id: str) -> List[Dict[str, Any]]:
        """Return the full version history for *model_id*."""
        versions = self._versions.get(model_id, [])
        return [v.to_dict() for v in versions]

    def get_champion(self, model_id: str) -> Optional[ModelVersion]:
        """Return the champion ModelVersion for *model_id*, or ``None``."""
        versions = self._versions.get(model_id, [])
        champion_v = self._champions.get(model_id)
        if champion_v is None or not versions:
            return None
        for v in versions:
            if v.version == champion_v:
                return v
        return None
