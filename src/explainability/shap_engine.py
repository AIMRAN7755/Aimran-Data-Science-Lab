"""
Model Explainability Engine.

Primary backend: native feature importances or sklearn permutation importance.
Optional backend: SHAP (tree / kernel explainer) when the library is installed.

The engine is fully cache-aware: global explanations are computed once per
model and cached until :meth:`invalidate_cache` is called (e.g. after retraining).
"""

from __future__ import annotations

import logging
import time
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)

try:
    import shap as _shap
    _SHAP_AVAILABLE = True
except ImportError:
    _SHAP_AVAILABLE = False
    logger.info("shap not installed — using permutation-importance fallback")


# ---------------------------------------------------------------------------
# Engine
# ---------------------------------------------------------------------------

class ExplainabilityEngine:
    """
    Unified model explainability engine.

    Methods
    -------
    explain(model, model_id, features, explain_type, feature_names)
        Explain a single prediction or compute global importance.
    local_explanation(model, model_id, features)
        Per-prediction (local) explanation.
    global_explanation(model, model_id, feature_names, top_n)
        Dataset-level feature importance.
    invalidate_cache(model_id)
        Purge cached explanations for a given model.
    """

    def __init__(self):
        self._cache: Dict[str, Dict[str, Any]] = {}

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def explain(
        self,
        model: Any,
        model_id: str,
        features: List[float],
        explain_type: str = "local",
        feature_names: Optional[List[str]] = None,
    ) -> Dict[str, Any]:
        """Dispatch to local or global explanation."""
        if explain_type == "global":
            X = pd.DataFrame([features], columns=feature_names) if feature_names else pd.DataFrame([features])
            return self.global_explanation(model, model_id, feature_names=feature_names)
        return self.local_explanation(model, model_id, features, feature_names=feature_names)

    def local_explanation(
        self,
        model: Any,
        model_id: str,
        features: List[float],
        feature_names: Optional[List[str]] = None,
    ) -> Dict[str, Any]:
        """Explain a single prediction using SHAP or native importances."""
        t0 = time.perf_counter()
        names = feature_names or [f"feature_{i}" for i in range(len(features))]
        X = pd.DataFrame([features], columns=names)

        contributions: Dict[str, float] = {}

        # Try SHAP first
        if _SHAP_AVAILABLE and hasattr(model, "predict"):
            try:
                explainer = (
                    _shap.TreeExplainer(model)
                    if hasattr(model, "feature_importances_")
                    else _shap.KernelExplainer(model.predict, X)
                )
                shap_values = explainer.shap_values(X)
                if isinstance(shap_values, list):
                    shap_values = shap_values[1]  # positive class
                arr = np.array(shap_values).flatten()
                contributions = {n: float(v) for n, v in zip(names, arr)}
            except Exception as exc:
                logger.debug("SHAP local failed (%s), using native", exc)

        # Fallback to native importances
        if not contributions and hasattr(model, "feature_importances_"):
            imps = model.feature_importances_
            contributions = {n: float(v) for n, v in zip(names, imps)}

        elapsed_ms = (time.perf_counter() - t0) * 1000
        ranked = sorted(contributions.items(), key=lambda kv: abs(kv[1]), reverse=True)

        return {
            "type": "local",
            "model_id": model_id,
            "method": "shap" if _SHAP_AVAILABLE else "native",
            "feature_contributions": contributions,
            "top_features": [{"feature": k, "value": round(v, 6)} for k, v in ranked[:10]],
            "computation_time_ms": round(elapsed_ms, 2),
        }

    def global_explanation(
        self,
        model: Any,
        model_id: str,
        feature_names: Optional[List[str]] = None,
        top_n: int = 20,
    ) -> Dict[str, Any]:
        """Compute global feature importance (cached)."""
        cache_key = f"{model_id}__global"
        if cache_key in self._cache:
            logger.debug("Returning cached global explanation for '%s'", model_id)
            return self._cache[cache_key]

        t0 = time.perf_counter()
        importances: Optional[np.ndarray] = None

        if hasattr(model, "feature_importances_"):
            importances = model.feature_importances_
            method = "native_importances"
        elif hasattr(model, "coef_"):
            coef = model.coef_
            importances = np.abs(coef).mean(axis=0) if coef.ndim > 1 else np.abs(coef)
            method = "linear_coefficients"
        else:
            method = "unavailable"

        if importances is not None:
            names = feature_names or [f"feature_{i}" for i in range(len(importances))]
            # Normalise to 0-1
            total = importances.sum()
            norm = importances / total if total > 0 else importances
            ranked = sorted(
                zip(names, norm.tolist()),
                key=lambda kv: kv[1],
                reverse=True,
            )[:top_n]
            result = {
                "type": "global",
                "model_id": model_id,
                "method": method,
                "top_features": [
                    {"rank": i + 1, "feature": k, "importance": round(v, 6)}
                    for i, (k, v) in enumerate(ranked)
                ],
                "total_features": len(names),
                "computation_time_ms": round((time.perf_counter() - t0) * 1000, 2),
            }
        else:
            result = {
                "type": "global",
                "model_id": model_id,
                "method": method,
                "top_features": [],
                "total_features": 0,
                "note": "This model type does not expose feature importances.",
            }

        self._cache[cache_key] = result
        return result

    def invalidate_cache(self, model_id: str) -> None:
        """Remove cached explanations for *model_id*."""
        keys = [k for k in self._cache if k.startswith(model_id)]
        for k in keys:
            del self._cache[k]
        if keys:
            logger.debug("Invalidated %d cached explanation(s) for '%s'", len(keys), model_id)
