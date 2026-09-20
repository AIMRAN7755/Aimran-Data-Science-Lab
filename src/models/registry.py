"""
Model Registry — in-process store for trained model instances.

All models registered here are accessible by any API endpoint in the
same process.  Persistence is handled via :func:`save_model_registry` /
:func:`load_model_registry`, which write a JSON index + per-model joblib
pickle alongside it.
"""

from __future__ import annotations

import json
import logging
from pathlib import Path
from typing import Dict, Optional

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Registry store
# ---------------------------------------------------------------------------

# Concrete type is BaseModel but we avoid the circular import by using Any.
MODEL_REGISTRY: Dict[str, object] = {}

_DEFAULT_REGISTRY_PATH = "models/registry_index.json"


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def register_model(name: str, model: object) -> None:
    """
    Add or overwrite a model in the in-process registry.

    Parameters
    ----------
    name : str
        Unique identifier for the model.
    model : BaseModel
        A trained (or untrained) model instance.
    """
    MODEL_REGISTRY[name] = model
    logger.info("Registered model '%s' (%s)", name, type(model).__name__)


def get_model(name: str) -> Optional[object]:
    """Return the model registered under *name*, or ``None``."""
    return MODEL_REGISTRY.get(name)


def list_models() -> Dict[str, str]:
    """Return ``{model_id: class_name}`` for every registered model."""
    return {name: type(model).__name__ for name, model in MODEL_REGISTRY.items()}


def save_model_registry(path: str = _DEFAULT_REGISTRY_PATH) -> None:
    """
    Persist the registry to disk.

    Each model is saved as ``<dir>/<name>.pkl`` via :mod:`joblib`.
    An index JSON at *path* records ``{name: pkl_path}`` for reload.

    Parameters
    ----------
    path : str
        Destination path of the JSON index file.
    """
    import joblib

    index_path = Path(path)
    index_path.parent.mkdir(parents=True, exist_ok=True)
    model_dir = index_path.parent

    index: Dict[str, str] = {}
    for name, model in MODEL_REGISTRY.items():
        safe_name = name.replace("/", "_").replace("\\", "_")
        pkl_path = model_dir / f"{safe_name}.pkl"
        try:
            # Delegate to BaseModel.save if available, else raw joblib dump.
            if hasattr(model, "save"):
                model.save(str(pkl_path))          # type: ignore[union-attr]
            else:
                joblib.dump(model, pkl_path, compress=3)
            index[name] = str(pkl_path)
        except Exception as exc:
            logger.warning("Could not save model '%s': %s", name, exc)

    with open(index_path, "w", encoding="utf-8") as f:
        json.dump(index, f, indent=2)

    logger.info("Registry saved: %d models → %s", len(index), index_path)


def load_model_registry(path: str = _DEFAULT_REGISTRY_PATH) -> None:
    """
    Load previously persisted models back into :data:`MODEL_REGISTRY`.

    Missing or corrupt pickle files are skipped with a warning.

    Parameters
    ----------
    path : str
        Path to the JSON index produced by :func:`save_model_registry`.
    """
    import joblib

    index_path = Path(path)
    if not index_path.exists():
        logger.info("No registry index found at %s — starting with empty registry", index_path)
        return

    with open(index_path, encoding="utf-8") as f:
        index: Dict[str, str] = json.load(f)

    loaded = 0
    for name, pkl_path in index.items():
        pkl = Path(pkl_path)
        if not pkl.exists():
            logger.warning("Model pickle missing for '%s': %s", name, pkl_path)
            continue
        try:
            payload = joblib.load(pkl)
            # Payload may be a raw dict (produced by BaseModel.save) or a model object.
            if isinstance(payload, dict) and "model" in payload:
                from src.models.base_model import BaseModel
                klass_name = payload.get("class", "BaseModel")
                # Reconstruct from payload
                obj = BaseModel.__new__(BaseModel)
                obj.model        = payload["model"]
                obj.model_type   = payload.get("model_type", "classifier")
                obj.task         = payload.get("task", obj.model_type)
                obj.is_trained   = payload.get("is_trained", True)
                obj.feature_names = payload.get("feature_names", [])
                obj.metrics      = payload.get("metrics", {})
                MODEL_REGISTRY[name] = obj
            else:
                MODEL_REGISTRY[name] = payload
            loaded += 1
        except Exception as exc:
            logger.warning("Could not load model '%s' from %s: %s", name, pkl_path, exc)

    logger.info("Registry loaded: %d/%d models restored from %s", loaded, len(index), index_path)
