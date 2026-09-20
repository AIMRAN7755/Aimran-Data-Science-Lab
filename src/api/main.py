"""
Aimran Data Science Lab — Enterprise FastAPI Application
=========================================================

Production-grade REST API providing:
- Async ML inference (single, batch, streaming)
- Model registry with versioning and champion promotion
- AutoML search
- Experiment tracking
- Data drift detection
- Model explainability (SHAP / feature importance)
- API-key authentication with RBAC
- Per-key rate limiting
- WebSocket real-time metrics streaming
- Kubernetes readiness / liveness probes
"""

from __future__ import annotations

import asyncio
import io
import json
import logging
import os
import time
import uuid
from concurrent.futures import ThreadPoolExecutor
from contextlib import asynccontextmanager
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd
import psutil
from fastapi import (
    Depends,
    FastAPI,
    File,
    Form,
    HTTPException,
    Request,
    Response,
    UploadFile,
    WebSocket,
    WebSocketDisconnect,
    status,
)
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.middleware.gzip import GZipMiddleware

from src.api.schemas import (
    AutoMLRequest,
    BatchPredictionRequest,
    CreateExperimentRequest,
    CreateUserRequest,
    DriftCheckRequest,
    ExplainRequest,
    HealthResponse,
    LogMetricsRequest,
    LoginRequest,
    PredictionRequest,
    PredictionResponse,
    RefreshRequest,
    StartRunRequest,
    TokenResponse,
    TrainRequest,
)
from src.automl.engine import AutoMLConfig, AutoMLEngine
from src.experiments.manager import ExperimentManager
from src.explainability.shap_engine import ExplainabilityEngine
from src.models.model_store import ModelStore
from src.models.random_forest import RandomForestModel
from src.models.registry import (
    get_model,
    list_models,
    load_model_registry,
    register_model,
    save_model_registry,
)
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded

from src.monitoring.drift_detector import DataDriftDetector
from src.monitoring.performance import PerformanceMiddleware, metrics_collector, system_monitor
from src.security.auth import AuthManager, RateLimiter, UserRole
from src.security.audit import get_audit_logger
from src.api.auth import init_auth
from worker import celery_app
from src.storage import AutoJobStore, DriftStore, RedisClient, init_db
from src.observability import OpenTelemetryMiddleware

_storage_redis = RedisClient()
init_db()
auto_job_store = AutoJobStore(_storage_redis)
drift_store = DriftStore(_storage_redis)

# ---------------------------------------------------------------------------
# Module-level singletons
# ---------------------------------------------------------------------------

logger = logging.getLogger(__name__)

# Redis-backed rate limiter (shared across all workers/replicas)
_REDIS_URL = os.getenv("REDIS_URL", "")
limiter = Limiter(
    key_func=get_remote_address,
    storage_uri=_REDIS_URL if _REDIS_URL else "memory://",
)
# ponytail: two pools so long jobs (train/automl/upload) don't starve interactive inference latency
executor = ThreadPoolExecutor(max_workers=4)            # long jobs: train, automl, upload, drift, batch
inference_executor = ThreadPoolExecutor(max_workers=4)  # low-latency: single predict + explain
auth_manager = AuthManager()
rate_limiter = RateLimiter(max_requests=200, window_seconds=60, storage_uri=_REDIS_URL or None)
audit_logger = get_audit_logger()
experiment_manager = ExperimentManager()
model_store = ModelStore()
explainability_engine = ExplainabilityEngine()
drift_detectors: Dict[str, DataDriftDetector] = {}   # model_id -> DataDriftDetector — in-memory hot cache
# NB: automl_jobs is now persisted via auto_job_store (Redis, 24h TTL)

_API_VERSION = "1.0.0"
# Paths reachable without authentication: docs, auth endpoints, and infra endpoints
# that external systems scrape/probe with no credentials (Prometheus, k8s probes, LB health).
_PUBLIC_PATHS = {
    "/", "/health", "/status", "/metrics", "/readiness", "/liveness",
    "/auth/login", "/auth/refresh",
    "/docs", "/openapi.json", "/redoc", "/favicon.ico",
}


# ---------------------------------------------------------------------------
# Custom API-key middleware
# ---------------------------------------------------------------------------

class AuthMiddleware(BaseHTTPMiddleware):
    """Authenticate every request via X-API-Key header or JWT Bearer token, except public paths."""

    async def dispatch(self, request: Request, call_next):
        path = request.url.path

        # CORS preflight (OPTIONS) carries no auth headers; public paths bypass auth.
        if request.method == "OPTIONS" or path in _PUBLIC_PATHS or path.startswith(("/docs", "/redoc")):
            return await call_next(request)

        user = None
        auth_header = request.headers.get("Authorization", "")
        api_key = request.headers.get("X-API-Key", "")

        # Try API key first
        if api_key:
            user = auth_manager.authenticate_api_key(api_key)
            if user and not rate_limiter.is_allowed(api_key):
                from fastapi.responses import JSONResponse
                return JSONResponse(
                    status_code=status.HTTP_429_TOO_MANY_REQUESTS,
                    content={"detail": "Rate limit exceeded"},
                )

        # Fall back to JWT Bearer token
        if user is None and auth_header.startswith("Bearer "):
            token = auth_header.removeprefix("Bearer ")
            user = auth_manager.get_user_from_token(token)

        # Authorization gate: reject unauthenticated access to every non-public route.
        # Single choke point so routes can't be left open by forgetting a per-route guard.
        if user is None:
            from fastapi.responses import JSONResponse
            return JSONResponse(
                status_code=status.HTTP_401_UNAUTHORIZED,
                content={"detail": "Authentication required. Provide X-API-Key or Bearer token."},
                headers={"WWW-Authenticate": "Bearer"},
            )

        request.state.user = user
        return await call_next(request)


# ---------------------------------------------------------------------------
# Lifespan
# ---------------------------------------------------------------------------

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    load_model_registry()
    model_count = len(list_models())
    logger.info("Loaded %d models from registry", model_count)
    system_monitor.collect_system_metrics()
    logger.info("Aimran Data Science Lab API v%s started", _API_VERSION)
    yield
    # Shutdown
    save_model_registry()
    logger.info("Model registry saved on shutdown")
    executor.shutdown(wait=False)
    inference_executor.shutdown(wait=False)


# ---------------------------------------------------------------------------
# FastAPI app
# ---------------------------------------------------------------------------

app = FastAPI(
    title="Aimran Data Science Lab API",
    description=(
        "Enterprise ML platform providing model serving, AutoML, experiment tracking, "
        "drift detection, and explainability."
    ),
    version=_API_VERSION,
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc",
)
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

# CORS — restrict origins in production via ALLOWED_ORIGINS env var
_ALLOWED_ORIGINS = os.getenv("ALLOWED_ORIGINS", "http://localhost:3000,http://localhost:8501").split(",")

app.add_middleware(GZipMiddleware, minimum_size=1000)
app.add_middleware(
    CORSMiddleware,
    allow_origins=_ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allow_headers=["Content-Type", "Authorization", "X-API-Key"],
)
app.add_middleware(PerformanceMiddleware, collector=metrics_collector)
app.add_middleware(OpenTelemetryMiddleware)
app.add_middleware(AuthMiddleware)
# Note: slowapi applies its own middleware via app.state.limiter

from src.api.v2 import router as v2_router
app.include_router(v2_router)


# ---------------------------------------------------------------------------
# Helper utilities
# ---------------------------------------------------------------------------

def _get_loop() -> asyncio.AbstractEventLoop:
    return asyncio.get_event_loop()


def _audit_user(request: Request) -> tuple:
    """Extract (user_id, username) from request state for audit logging."""
    user = getattr(request.state, "user", None)
    if user:
        return user.user_id, user.username
    return "", "anonymous"


def _get_client_ip(request: Request) -> str:
    forwarded = request.headers.get("X-Forwarded-For", "")
    if forwarded:
        return forwarded.split(",")[0].strip()
    return request.client.host if request.client else ""


def _require_model(model_id: str):
    model = get_model(model_id)
    if model is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Model '{model_id}' not found. Register it first via POST /models/train",
        )
    return model


def _require_admin(request: Request):
    user = getattr(request.state, "user", None)
    if user is None:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Authentication required")
    if not auth_manager.require_role(user, UserRole.ADMIN):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Admin role required")
    return user


def _build_prediction(model, features: list) -> tuple[float, Optional[list]]:
    """Run inference synchronously (intended for executor)."""
    X = pd.DataFrame(np.array(features).reshape(1, -1))
    pred = model.predict(X)
    prob = None
    if hasattr(model, "predict_proba"):
        try:
            prob = model.predict_proba(X).tolist()[0]
        except Exception:
            pass
    return float(pred[0]), prob


def _check_celery_worker() -> tuple[bool, int]:
    """Check Celery worker connectivity via inspect().active().
    Returns (connected: bool, active_tasks: int)."""
    try:
        inspector = celery_app.control.inspect()
        active = inspector.active()
        if active is None:
            return False, 0
        total = sum(len(tasks) for tasks in active.values())
        return True, total
    except Exception:
        return False, 0


def _profile_dataframe(df: pd.DataFrame) -> Dict[str, Any]:
    """Generate a lightweight data-quality profile."""
    profile: Dict[str, Any] = {}
    for col in df.columns:
        s = df[col]
        col_info: Dict[str, Any] = {
            "dtype": str(s.dtype),
            "null_count": int(s.isna().sum()),
            "null_pct": round(float(s.isna().mean() * 100), 2),
            "unique_count": int(s.nunique()),
        }
        if pd.api.types.is_numeric_dtype(s):
            col_info.update({
                "mean": round(float(s.mean()), 4) if not s.isna().all() else None,
                "std": round(float(s.std()), 4) if not s.isna().all() else None,
                "min": round(float(s.min()), 4) if not s.isna().all() else None,
                "max": round(float(s.max()), 4) if not s.isna().all() else None,
                "p25": round(float(s.quantile(0.25)), 4) if not s.isna().all() else None,
                "p75": round(float(s.quantile(0.75)), 4) if not s.isna().all() else None,
            })
        else:
            top = s.value_counts().head(5).to_dict()
            col_info["top_values"] = {str(k): int(v) for k, v in top.items()}
        profile[col] = col_info
    return profile


# ---------------------------------------------------------------------------
# Root & Health
# ---------------------------------------------------------------------------

@app.get("/", tags=["Root"])
async def root():
    return {
        "name": "Aimran Data Science Lab API",
        "version": _API_VERSION,
        "docs": "/docs",
        "health": "/health",
        "features": [
            "async-inference",
            "batch-predictions",
            "streaming-predictions",
            "model-registry",
            "model-versioning",
            "champion-promotion",
            "automl",
            "experiment-tracking",
            "data-drift-detection",
            "model-explainability",
            "api-key-auth",
            "rate-limiting",
            "gzip-compression",
            "websocket-metrics",
            "k8s-probes",
        ],
    }


@app.get("/health", response_model=HealthResponse, tags=["Health"])
async def health():
    cpu = psutil.cpu_percent(interval=0.1)
    mem = psutil.virtual_memory()
    celery_ok, celery_tasks = _check_celery_worker()
    status = "healthy" if cpu < 90 else "degraded"
    if not celery_ok:
        status = "degraded"
    return HealthResponse(
        status=status,
        version=_API_VERSION,
        environment=os.getenv("ENVIRONMENT", "development"),
        cpu_percent=round(cpu, 1),
        memory_percent=round(mem.percent, 1),
        models_count=len(list_models()),
        celery_worker_connected=celery_ok,
        celery_active_tasks=celery_tasks,
    )


@app.get("/status", tags=["Status"])
async def get_status():
    process = psutil.Process()
    sys_metrics = system_monitor.collect_system_metrics() or {}
    return {
        "api": "running",
        "version": _API_VERSION,
        "models_count": len(list_models()),
        "timestamp": pd.Timestamp.now().isoformat(),
        "system": {
            "cpu_percent": sys_metrics.get("cpu_percent"),
            "memory_percent": sys_metrics.get("memory_percent"),
            "disk_percent": sys_metrics.get("disk_percent"),
            "process_memory_mb": round(process.memory_info().rss / 1024 / 1024, 2),
            "pid": process.pid,
        },
        "metrics_summary": metrics_collector.get_summary(),
    }


@app.get("/metrics", tags=["Monitoring"])
async def get_metrics():
    """Prometheus text exposition (v0.0.4). JSON summary is under /health."""
    summary = metrics_collector.get_summary()
    lines: List[str] = []

    def _emit(name: str, value: Any, help_: str) -> None:
        lines.append(f"# HELP {name} {help_}")
        lines.append(f"# TYPE {name} gauge")
        lines.append(f"{name} {float(value)}")

    _emit("aimran_metrics_collected_total", summary.get("total_metrics_collected", 0),
          "Total performance metrics collected")
    for group in ("cpu", "memory"):
        for stat, val in (summary.get(group) or {}).items():
            _emit(f"aimran_{group}_{stat}", val, f"{group} {stat}")
    for group, prefix in (("api_requests", "aimran_api"), ("db_queries", "aimran_db")):
        for stat, val in (summary.get(group) or {}).items():
            _emit(f"{prefix}_{stat}", val, f"{group} {stat}")

    return Response("\n".join(lines) + "\n", media_type="text/plain; version=0.0.4; charset=utf-8")


@app.get("/readiness", tags=["Health"])
async def readiness():
    """Kubernetes readiness probe."""
    models = list_models()
    return {"status": "ready", "models_available": len(models)}


@app.get("/liveness", tags=["Health"])
async def liveness():
    """Kubernetes liveness probe."""
    return {"status": "alive", "timestamp": time.time()}


# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------

@app.post("/auth/login", tags=["Auth"])
@limiter.limit("10/minute")
async def login(req: Request, request: LoginRequest):
    """Authenticate with username+password, receive JWT access + refresh tokens."""
    result = auth_manager.login(request.username, request.password)
    if result is None:
        audit_logger.login(username=request.username, success=False)
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid username or password",
        )
    access_token, refresh_token, user = result
    audit_logger.login(
        username=request.username, success=True,
        details={"user_id": user.user_id, "role": user.role.value},
    )
    return TokenResponse(
        access_token=access_token,
        refresh_token=refresh_token,
        user=user.to_dict(),
    )


@app.post("/auth/refresh", tags=["Auth"])
@limiter.limit("20/minute")
async def refresh_token(req: Request, request: RefreshRequest):
    """Exchange a valid refresh token for a new access token + new refresh token."""
    result = auth_manager.refresh_access_token(request.refresh_token)
    if result is None:
        audit_logger.token_refresh(user_id="", username="unknown", success=False)
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or expired refresh token",
        )
    new_access, new_refresh = result
    audit_logger.token_refresh(user_id="", username="unknown", success=True)
    return TokenResponse(
        access_token=new_access,
        refresh_token=new_refresh,
        user={},
    )


# ---------------------------------------------------------------------------
# Predictions
# ---------------------------------------------------------------------------

@app.post("/predict", response_model=PredictionResponse, tags=["Predictions"])
async def predict(request: PredictionRequest):
    start = time.perf_counter()
    model = _require_model(request.model_id)

    loop = _get_loop()
    prediction, probability = await loop.run_in_executor(
        inference_executor, _build_prediction, model, request.features
    )

    explanation: Optional[Dict[str, Any]] = None
    if request.explain:
        def _explain():
            return explainability_engine.local_explanation(
                model, request.model_id, request.features
            )
        explanation = await loop.run_in_executor(inference_executor, _explain)

    elapsed_ms = (time.perf_counter() - start) * 1000
    metrics_collector.record_api_request("/predict", "POST", elapsed_ms, 200)
    logger.info("Prediction '%s' in %.2fms", request.model_id, elapsed_ms)

    return PredictionResponse(
        prediction=prediction,
        probability=probability,
        model_id=request.model_id,
        explanation=explanation,
        processing_time_ms=round(elapsed_ms, 3),
    )


@app.post("/predict-batch", tags=["Predictions"])
async def predict_batch(payload: BatchPredictionRequest):
    start = time.perf_counter()
    requests = payload.requests

    def process_all() -> List[Dict[str, Any]]:
        results = []
        for req in requests:
            model = get_model(req.model_id)
            if model is None:
                results.append({"model_id": req.model_id, "error": "Model not found", "prediction": None})
                continue
            try:
                pred, prob = _build_prediction(model, req.features)
                results.append({
                    "model_id": req.model_id,
                    "prediction": pred,
                    "probability": prob,
                })
            except Exception as exc:
                results.append({"model_id": req.model_id, "error": str(exc), "prediction": None})
        return results

    loop = _get_loop()
    results = await loop.run_in_executor(executor, process_all)

    elapsed_ms = (time.perf_counter() - start) * 1000
    logger.info("Batch %d predictions in %.2fms", len(requests), elapsed_ms)

    return {
        "status": "completed",
        "count": len(requests),
        "results": results,
        "processing_time_ms": round(elapsed_ms, 3),
    }


@app.post("/predict-stream", tags=["Predictions"])
async def predict_stream(payload: BatchPredictionRequest):
    """Stream predictions one-by-one as newline-delimited JSON."""

    async def generate():
        for req in payload.requests:
            model = get_model(req.model_id)
            if model is None:
                row = {"model_id": req.model_id, "error": "Model not found"}
            else:
                try:
                    loop = _get_loop()
                    pred, prob = await loop.run_in_executor(
                        executor, _build_prediction, model, req.features
                    )
                    row = {"model_id": req.model_id, "prediction": pred, "probability": prob}
                except Exception as exc:
                    row = {"model_id": req.model_id, "error": str(exc)}
            yield json.dumps(row) + "\n"

    return StreamingResponse(generate(), media_type="application/x-ndjson")


# ---------------------------------------------------------------------------
# Data upload
# ---------------------------------------------------------------------------

@app.post("/upload", tags=["Data"])
async def upload_file(file: UploadFile = File(...)):
    start = time.perf_counter()
    contents = await file.read()

    loop = _get_loop()
    df = await loop.run_in_executor(executor, lambda: pd.read_csv(io.BytesIO(contents)))

    elapsed_ms = (time.perf_counter() - start) * 1000
    logger.info("Uploaded '%s' (%d rows) in %.2fms", file.filename, len(df), elapsed_ms)

    return {
        "filename": file.filename,
        "rows": len(df),
        "columns": df.columns.tolist(),
        "shape": list(df.shape),
        "dtypes": {col: str(dtype) for col, dtype in df.dtypes.items()},
        "preview": df.head(5).to_dict(orient="records"),
        "processing_time_ms": round(elapsed_ms, 3),
    }


@app.post("/upload-and-profile", tags=["Data"])
async def upload_and_profile(file: UploadFile = File(...)):
    start = time.perf_counter()
    contents = await file.read()

    def parse_and_profile():
        df = pd.read_csv(io.BytesIO(contents))
        profile = _profile_dataframe(df)
        return df, profile

    loop = _get_loop()
    df, profile = await loop.run_in_executor(executor, parse_and_profile)

    elapsed_ms = (time.perf_counter() - start) * 1000
    logger.info("Profiled '%s' (%d rows) in %.2fms", file.filename, len(df), elapsed_ms)

    null_counts = {col: info["null_count"] for col, info in profile.items()}
    total_cells = df.shape[0] * df.shape[1]
    total_nulls = sum(null_counts.values())

    return {
        "filename": file.filename,
        "shape": list(df.shape),
        "columns": df.columns.tolist(),
        "data_quality": {
            "total_cells": total_cells,
            "total_nulls": total_nulls,
            "null_pct": round(total_nulls / max(total_cells, 1) * 100, 2),
            "complete_columns": sum(1 for v in null_counts.values() if v == 0),
        },
        "column_profiles": profile,
        "processing_time_ms": round(elapsed_ms, 3),
    }


# ---------------------------------------------------------------------------
# Models
# ---------------------------------------------------------------------------

@app.get("/models", tags=["Models"])
async def list_registered_models():
    registered = list_models()
    store_meta = model_store.list_all()
    models_list = []
    for model_id, model_type in registered.items():
        meta = store_meta.get(model_id, {})
        model_obj = get_model(model_id)
        is_trained = getattr(model_obj, "is_trained", True)
        models_list.append({
            "model_id": model_id,
            "model_type": model_type,
            "status": "trained" if is_trained else "registered",
            "version_count": meta.get("version_count", 1),
            "champion_version": meta.get("champion_version"),
            "metrics": getattr(model_obj, "metrics", {}),
        })
    return {"models": models_list, "total": len(models_list)}


@app.get("/models/{model_id}", tags=["Models"])
async def get_model_info(model_id: str):
    meta = model_store.get_metadata(model_id)
    if meta is None:
        model = get_model(model_id)
        if model is None:
            raise HTTPException(status_code=404, detail=f"Model '{model_id}' not found")
        meta = {
            "model_id": model_id,
            "model_type": type(model).__name__,
            "is_trained": getattr(model, "is_trained", False),
            "feature_names": getattr(model, "feature_names", []),
            "metrics": getattr(model, "metrics", {}),
        }
    return meta


@app.post("/models/train", tags=["Models"])
async def register_model_type(request: TrainRequest, req: Request):
    """Register (instantiate) a model type. Train data separately via /models/train/{id}."""
    uid, uname = _audit_user(req)
    params = request.params or {}
    model_type = request.model_type

    def build_model():
        if model_type == "random_forest":
            return RandomForestModel(task=request.task, **params)
        elif model_type == "gradient_boosting":
            from src.models.gradient_boosting import GradientBoostingModel
            return GradientBoostingModel(task=request.task, **params)
        elif model_type == "neural_network":
            from src.models.neural_network import NeuralNetworkModel
            return NeuralNetworkModel(task=request.task, **params)
        else:
            raise ValueError(f"Unknown model type: {model_type}")

    try:
        loop = _get_loop()
        model = await loop.run_in_executor(executor, build_model)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc))
    except Exception as exc:
        logger.error("Model build error: %s", exc)
        raise HTTPException(status_code=500, detail=str(exc))

    model_store.register(request.model_id, model, task=request.task)
    audit_logger.model_registered(uid, uname, request.model_id, model_type, request.task)
    logger.info("Registered %s model as '%s'", model_type, request.model_id)

    return {
        "status": "registered",
        "model_id": request.model_id,
        "model_type": model_type,
        "task": request.task,
        "message": f"Model '{request.model_id}' registered. Train it via POST /models/train/{request.model_id}",
    }


@app.post("/models/train/{model_id}", tags=["Models"])
async def train_model_with_data(model_id: str, file: UploadFile = File(...), req: Request = None):
    model = _require_model(model_id)
    uid, uname = _audit_user(req) if req else ("", "anonymous")
    contents = await file.read()

    def train():
        df = pd.read_csv(io.BytesIO(contents))
        target_col = "target" if "target" in df.columns else df.columns[-1]
        X = df.drop(columns=[target_col])
        y = df[target_col]
        model.train(X, y)
        task = getattr(model, "task", "classification")
        metrics = model.evaluate(X, y, task=task)
        return {
            "rows": len(df),
            "features": X.shape[1],
            "target": target_col,
            "metrics": metrics,
            "feature_names": X.columns.tolist(),
        }

    loop = _get_loop()
    try:
        result = await loop.run_in_executor(executor, train)
    except Exception as exc:
        logger.error("Training error for '%s': %s", model_id, exc)
        raise HTTPException(status_code=500, detail=str(exc))

    explainability_engine.invalidate_cache(model_id)
    model_store.register(model_id, model, metrics=result["metrics"])
    audit_logger.model_trained(uid, uname, model_id, result["rows"], result["features"], result["metrics"])
    logger.info("Trained model '%s' on %d rows", model_id, result["rows"])

    return {"status": "trained", "model_id": model_id, **result}


@app.delete("/models/{model_id}", tags=["Models"])
async def deregister_model(model_id: str, req: Request):
    uid, uname = _audit_user(req)
    from src.models.registry import MODEL_REGISTRY
    if model_id not in MODEL_REGISTRY:
        audit_logger.custom(uid, uname, "model_delete_failed", "model", model_id, outcome="failure")
        raise HTTPException(status_code=404, detail=f"Model '{model_id}' not found")
    del MODEL_REGISTRY[model_id]
    explainability_engine.invalidate_cache(model_id)
    audit_logger.model_deleted(uid, uname, model_id)
    logger.info("Deregistered model '%s'", model_id)
    return {"status": "deregistered", "model_id": model_id}


@app.get("/models/{model_id}/metrics", tags=["Models"])
async def get_model_metrics(model_id: str):
    model = _require_model(model_id)
    return {
        "model_id": model_id,
        "metrics": getattr(model, "metrics", {}),
        "is_trained": getattr(model, "is_trained", False),
        "feature_names": getattr(model, "feature_names", []),
    }


@app.post("/models/{model_id}/champion", tags=["Models"])
async def promote_champion(model_id: str, req: Request):
    uid, uname = _audit_user(req)
    _require_model(model_id)  # existence check
    try:
        version_info = model_store.promote_champion(model_id)
    except KeyError:
        model = get_model(model_id)
        model_store.register(model_id, model)
        version_info = model_store.promote_champion(model_id)
    audit_logger.champion_promoted(uid, uname, model_id, version_info)
    logger.info("Promoted '%s' to champion", model_id)
    return {"status": "promoted", "model_id": model_id, "version_info": version_info}


# ---------------------------------------------------------------------------
# AutoML
# ---------------------------------------------------------------------------

@app.post("/automl/train", tags=["AutoML"])
async def automl_train(
    file: UploadFile = File(...),
    task: str = Form("classification"),
    target_column: str = Form("target"),
    time_budget: int = Form(60),
    n_trials: int = Form(20),
    req: Request = None,
):
    uid, uname = _audit_user(req) if req else ("", "anonymous")
    contents = await file.read()
    job_id = str(uuid.uuid4())
    auto_job_store.set(job_id, {"status": "running", "started_at": time.time()})

    config = AutoMLConfig(
        task=task,
        time_budget=float(time_budget),
        n_trials=n_trials,
    )
    engine = AutoMLEngine(config)

    def run_automl():
        df = pd.read_csv(io.BytesIO(contents))
        if target_column not in df.columns:
            raise ValueError(f"Target column '{target_column}' not found")
        X = df.drop(columns=[target_column]).select_dtypes(include=[np.number]).fillna(0)
        y = df[target_column]
        engine.fit(X, y, task=task, time_budget=float(time_budget), n_trials=n_trials)
        leaderboard_df = engine.get_leaderboard()
        best_result = engine._best_result  # type: ignore[attr-defined]
        return leaderboard_df, best_result

    loop = _get_loop()
    try:
        leaderboard_df, best_result = await loop.run_in_executor(executor, run_automl)
    except Exception as exc:
        auto_job_store.set(job_id, {"status": "failed", "error": str(exc)})
        audit_logger.automl_run(uid, uname, job_id, task, "failed", details={"error": str(exc)})
        logger.error("AutoML job %s failed: %s", job_id, exc)
        raise HTTPException(status_code=500, detail=str(exc))

    # Register the best model in the store
    best_model = engine.get_best_model()
    best_model_id = f"automl_{job_id[:8]}"
    best_score = float(best_result.mean_score) if best_result else 0.0
    best_model_name = best_result.model_name if best_result else "unknown"

    if best_model is not None:
        model_store.register(
            best_model_id,
            best_model,
            task=task,
            metrics={"score": best_score},
            tags={"source": "automl", "job_id": job_id},
        )

    leaderboard_list = leaderboard_df.to_dict(orient="records") if leaderboard_df is not None else []
    scoring_metric = config.scoring_metric or ("accuracy" if task == "classification" else "r2")

    result_payload = {
        "job_id": job_id,
        "status": "completed",
        "best_model_type": best_model_name,
        "best_score": best_score,
        "metric": scoring_metric,
        "task": task,
        "trials_run": len(leaderboard_list),
        "best_model_id": best_model_id,
        "leaderboard": leaderboard_list,
    }
    auto_job_store.set(job_id, result_payload)
    logger.info("AutoML job %s completed, best model registered as '%s'", job_id, best_model_id)

    return result_payload


@app.get("/automl/{job_id}/status", tags=["AutoML"])
async def automl_status(job_id: str):
    job = auto_job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail=f"AutoML job '{job_id}' not found")
    return {
        "job_id": job_id,
        "status": job.get("status"),
        "trials_run": job.get("trials_run"),
        "started_at": job.get("started_at"),
        "duration_s": job.get("duration_s"),
    }


@app.get("/automl/{job_id}/leaderboard", tags=["AutoML"])
async def automl_leaderboard(job_id: str):
    job = auto_job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail=f"AutoML job '{job_id}' not found")
    if job.get("status") != "completed":
        raise HTTPException(status_code=409, detail="AutoML job not yet completed")
    return {
        "job_id": job_id,
        "metric": job.get("metric"),
        "best_model_type": job.get("best_model_type"),
        "best_score": job.get("best_score"),
        "leaderboard": job.get("leaderboard", []),
    }


# ---------------------------------------------------------------------------
# Experiments
# ---------------------------------------------------------------------------

@app.post("/experiments", tags=["Experiments"])
async def create_experiment(request: CreateExperimentRequest, req: Request):
    uid, uname = _audit_user(req)
    exp = experiment_manager.create_experiment(
        name=request.name,
        description=request.description,
        tags=request.tags or {},
    )
    audit_logger.experiment_created(uid, uname, exp.experiment_id, request.name)
    return exp.to_dict(include_runs=False)


@app.get("/experiments", tags=["Experiments"])
async def list_experiments():
    return {"experiments": experiment_manager.list_experiments()}


@app.get("/experiments/{experiment_id}", tags=["Experiments"])
async def get_experiment(experiment_id: str):
    exp = experiment_manager.get_experiment(experiment_id)
    if exp is None:
        raise HTTPException(status_code=404, detail=f"Experiment '{experiment_id}' not found")
    return {**exp.to_dict(), "summary": exp.summary()}


@app.post("/experiments/{experiment_id}/runs", tags=["Experiments"])
async def start_run(experiment_id: str, request: StartRunRequest, req: Request):
    uid, uname = _audit_user(req)
    try:
        run = experiment_manager.start_run(
            experiment_id=experiment_id,
            name=request.name,
            params=request.params or {},
            tags=request.tags or {},
        )
    except KeyError as exc:
        raise HTTPException(status_code=404, detail=str(exc))
    audit_logger.run_started(uid, uname, experiment_id, run.run_id)
    return run.to_dict()


@app.put("/experiments/runs/{run_id}/metrics", tags=["Experiments"])
async def log_metrics(run_id: str, request: LogMetricsRequest):
    try:
        run = experiment_manager.log_metrics(run_id, request.metrics)
    except KeyError as exc:
        raise HTTPException(status_code=404, detail=str(exc))
    return run.to_dict()


@app.get("/experiments/{experiment_id}/best-run", tags=["Experiments"])
async def get_best_run(experiment_id: str, metric: str = "accuracy", higher_is_better: bool = True):
    best = experiment_manager.get_best_run(experiment_id, metric, higher_is_better)
    if best is None:
        raise HTTPException(
            status_code=404,
            detail=f"No finished runs with metric '{metric}' in experiment '{experiment_id}'",
        )
    return best


# ---------------------------------------------------------------------------
# Explainability
# ---------------------------------------------------------------------------

@app.post("/explain", tags=["Explainability"])
async def explain_prediction(request: ExplainRequest):
    model = _require_model(request.model_id)
    feature_names = getattr(model, "feature_names", [])

    loop = _get_loop()
    explanation = await loop.run_in_executor(
        inference_executor,
        lambda: explainability_engine.explain(
            model,
            request.model_id,
            request.features,
            explain_type=request.explain_type,
            feature_names=feature_names or None,
        ),
    )
    return explanation


@app.get("/explain/{model_id}/global", tags=["Explainability"])
async def global_explanation(model_id: str, top_n: int = 20):
    model = _require_model(model_id)
    feature_names = getattr(model, "feature_names", [])

    loop = _get_loop()
    explanation = await loop.run_in_executor(
        inference_executor,
        lambda: explainability_engine.global_explanation(
            model, model_id, feature_names=feature_names or None, top_n=top_n
        ),
    )
    return explanation


# ---------------------------------------------------------------------------
# Drift detection
# ---------------------------------------------------------------------------

@app.post("/drift/detect", tags=["Drift"])
async def detect_drift(request: DriftCheckRequest):
    model_id = request.model_id
    if model_id not in drift_detectors:
        drift_detectors[model_id] = DataDriftDetector(model_id)

    detector = drift_detectors[model_id]

    loop = _get_loop()
    report = await loop.run_in_executor(
        executor,
        lambda: detector.detect_from_dicts(
            request.reference_data,
            request.current_data,
            feature_names=request.feature_names,
        ),
    )
    return report.to_dict()


@app.get("/drift/{model_id}/report", tags=["Drift"])
async def get_drift_report(model_id: str):
    detector = drift_detectors.get(model_id)
    if detector is None:
        raise HTTPException(
            status_code=404,
            detail=f"No drift report available for model '{model_id}'. Run POST /drift/detect first.",
        )
    report = detector.get_last_report()
    if report is None:
        raise HTTPException(status_code=404, detail="No drift report found for this model")
    return report


# ---------------------------------------------------------------------------
# Admin
# ---------------------------------------------------------------------------

@app.post("/admin/users", tags=["Admin"])
@limiter.limit("30/hour")
async def create_user(req: Request, request: CreateUserRequest):
    _require_admin(req)
    uid, uname = _audit_user(req)
    try:
        role = UserRole(request.role)
    except ValueError:
        raise HTTPException(status_code=400, detail=f"Invalid role: {request.role}")

    user = auth_manager.create_user(
        username=request.username,
        email=request.email,
        role=role,
    )
    audit_logger.user_created(uid, uname, user.user_id, request.username, request.role)
    raw_key = getattr(user, "_raw_api_key", None)
    return {
        **user.to_dict(),
        "api_key": raw_key,
        "warning": "Store this API key securely — it will not be shown again.",
    }


@app.get("/admin/users", tags=["Admin"])
async def list_users(req: Request):
    _require_admin(req)
    return {"users": auth_manager.list_users()}


@app.delete("/admin/users/{user_id}", tags=["Admin"])
async def deactivate_user(user_id: str, req: Request):
    _require_admin(req)
    uid, uname = _audit_user(req)
    success = auth_manager.deactivate_user(user_id)
    if not success:
        audit_logger.custom(uid, uname, "user_deactivate_failed", "user", user_id, outcome="failure")
        raise HTTPException(status_code=404, detail=f"User '{user_id}' not found")
    audit_logger.user_deactivated(uid, uname, user_id)
    return {"status": "deactivated", "user_id": user_id}


# ---------------------------------------------------------------------------
# Audit log retrieval
# ---------------------------------------------------------------------------

@app.get("/audit/log", tags=["Audit"])
async def get_audit_log(req: Request, limit: int = 100, action: str = "", user_id: str = ""):
    """Retrieve recent audit log entries. Optionally filter by action or user_id. Admin only."""
    _require_admin(req)
    if action:
        return {"entries": audit_logger.get_by_action(action, limit)}
    elif user_id:
        return {"entries": audit_logger.get_by_user(user_id, limit)}
    return {"entries": audit_logger.get_recent(limit)}


# ---------------------------------------------------------------------------
# WebSocket: real-time metrics streaming
# ---------------------------------------------------------------------------

@app.websocket("/ws/metrics")
async def websocket_metrics(websocket: WebSocket):
    await websocket.accept()
    logger.info("WebSocket client connected: %s", websocket.client)
    try:
        while True:
            disk = psutil.disk_usage("/")
            payload = {
                "timestamp": time.time(),
                "cpu_percent": psutil.cpu_percent(interval=None),
                "memory_percent": psutil.virtual_memory().percent,
                "disk_percent": disk.percent,
                "models_count": len(list_models()),
            }
            await websocket.send_json(payload)
            await asyncio.sleep(2)
    except WebSocketDisconnect:
        logger.info("WebSocket client disconnected")
    except Exception as exc:
        logger.error("WebSocket error: %s", exc)
        await websocket.close(code=1011)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "src.api.main:app",
        host="0.0.0.0",
        port=8000,
        reload=False,
        workers=1,
        log_level="info",
    )
