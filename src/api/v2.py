"""
API v2 — Enterprise endpoints: AutoML, Explainability, Drift Detection.
Mounted on the main app as /v2.
"""

from __future__ import annotations

import io
import logging
import uuid
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd
from fastapi import APIRouter, File, HTTPException, UploadFile
from fastapi.responses import JSONResponse

from src.explainability.shap_engine import ExplainabilityEngine
from src.models.registry import get_model

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/v2", tags=["Enterprise v2"])

# In-memory drift detector cache (model_id -> DataDriftDetector)
_drift_detectors: Dict[str, Any] = {}


# ---------------------------------------------------------------------------
# Info
# ---------------------------------------------------------------------------

@router.get("/info", summary="V2 API capabilities")
async def v2_info():
    try:
        import shap  # noqa: F401
        shap_available = True
    except ImportError:
        shap_available = False

    return {
        "version": "2.0.0",
        "capabilities": {
            "automl": True,
            "explainability": True,
            "shap_available": shap_available,
            "drift_detection": True,
        },
        "endpoints": {
            "automl_run": "POST /v2/automl/run",
            "explain_global": "POST /v2/explain/global",
            "explain_local": "POST /v2/explain/local",
            "drift_fit": "POST /v2/drift/fit",
            "drift_detect": "POST /v2/drift/detect",
        },
    }


# ---------------------------------------------------------------------------
# AutoML
# ---------------------------------------------------------------------------

@router.post("/automl/run", summary="Run AutoML on uploaded CSV")
async def run_automl(
    file: UploadFile = File(...),
    task: str = "auto",
    cv_folds: int = 5,
    timeout_seconds: int = 120,
):
    """Upload a CSV, run AutoML, register the best model, and return a leaderboard."""
    import asyncio
    from concurrent.futures import ThreadPoolExecutor

    from src.automl.engine import AutoMLEngine
    from src.models.base_model import BaseModel as LabBaseModel
    from src.models.registry import register_model

    executor = ThreadPoolExecutor(max_workers=2)
    contents = await file.read()

    def _run():
        df = pd.read_csv(io.BytesIO(contents))
        target_col = "target" if "target" in df.columns else df.columns[-1]
        X = df.drop(columns=[target_col])
        y = df[target_col]

        engine = AutoMLEngine(cv_folds=cv_folds, timeout_seconds=timeout_seconds, verbose=False)
        result = engine.fit(X, y, task=task)

        lab_model = LabBaseModel(model_type=result.task)
        lab_model.model = result.best_model
        lab_model.is_trained = True
        lab_model.feature_names = result.feature_names

        model_id = f"automl_{result.best_model_name.lower()}_{uuid.uuid4().hex[:6]}"
        register_model(model_id, lab_model)
        return result.summary(), model_id

    try:
        loop = asyncio.get_event_loop()
        summary, model_id = await loop.run_in_executor(executor, _run)
    except Exception as exc:
        logger.error("V2 AutoML error: %s", exc)
        raise HTTPException(status_code=500, detail=str(exc))
    finally:
        executor.shutdown(wait=False)

    return {
        "status": "completed",
        "automl_summary": summary,
        "registered_model_id": model_id,
        "message": f"Best model '{summary['best_model']}' registered as '{model_id}'",
    }


# ---------------------------------------------------------------------------
# Explainability
# ---------------------------------------------------------------------------

_explainer = ExplainabilityEngine()


def _lookup_estimator(model_id: str):
    """Fetch a registered model and unwrap the underlying estimator."""
    wrapper = get_model(model_id)
    if wrapper is None:
        raise HTTPException(status_code=404, detail=f"Model '{model_id}' not found.")
    estimator = getattr(wrapper, "model", wrapper)
    feature_names = getattr(wrapper, "feature_names", None) or None
    return estimator, feature_names


@router.post("/explain/global", summary="Global feature importance for a registered model")
async def explain_global(model_id: str, top_n: int = 20):
    estimator, feature_names = _lookup_estimator(model_id)
    return _explainer.global_explanation(estimator, model_id, feature_names=feature_names, top_n=top_n)


@router.post("/explain/local", summary="Local explanation for a single prediction")
async def explain_local(model_id: str, features: List[float]):
    estimator, feature_names = _lookup_estimator(model_id)
    return _explainer.local_explanation(estimator, model_id, features, feature_names=feature_names)


# ---------------------------------------------------------------------------
# Drift detection
# ---------------------------------------------------------------------------

@router.post("/drift/fit", summary="Fit drift detector on reference data")
async def fit_drift_detector(
    file: UploadFile = File(...),
    detector_id: Optional[str] = None,
):
    """Upload reference CSV to establish the baseline distribution."""
    import asyncio
    from concurrent.futures import ThreadPoolExecutor
    from src.monitoring.drift_detector import DataDriftDetector

    executor = ThreadPoolExecutor(max_workers=1)
    det_id = detector_id or f"detector_{uuid.uuid4().hex[:8]}"
    contents = await file.read()

    def _fit():
        df = pd.read_csv(io.BytesIO(contents))
        detector = DataDriftDetector(det_id)
        detector.fit_reference(df)
        _drift_detectors[det_id] = detector
        return len(df), list(df.columns)

    try:
        loop = asyncio.get_event_loop()
        n_rows, columns = await loop.run_in_executor(executor, _fit)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc))
    finally:
        executor.shutdown(wait=False)

    return {
        "status": "fitted",
        "detector_id": det_id,
        "reference_rows": n_rows,
        "columns": columns,
        "message": f"Use detector_id='{det_id}' in /v2/drift/detect",
    }


@router.post("/drift/detect", summary="Detect drift against reference distribution")
async def detect_drift(
    file: UploadFile = File(...),
    detector_id: str = ...,
):
    """Upload current data CSV and compare against the fitted reference."""
    import asyncio
    from concurrent.futures import ThreadPoolExecutor

    detector = _drift_detectors.get(detector_id)
    if detector is None:
        raise HTTPException(
            status_code=404,
            detail=f"Detector '{detector_id}' not found. Call /v2/drift/fit first.",
        )

    executor = ThreadPoolExecutor(max_workers=1)
    contents = await file.read()

    def _detect():
        df = pd.read_csv(io.BytesIO(contents))
        report = detector.detect_drift(df)
        return report.to_dict()

    try:
        loop = asyncio.get_event_loop()
        report = await loop.run_in_executor(executor, _detect)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc))
    finally:
        executor.shutdown(wait=False)

    return {"status": "completed", "detector_id": detector_id, "report": report}


@router.get("/drift/detectors", summary="List active drift detectors")
async def list_drift_detectors():
    return {"count": len(_drift_detectors), "detectors": list(_drift_detectors.keys())}
