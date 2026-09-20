"""
Pydantic request / response schemas for the Aimran Data Science Lab API.
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------

class LoginRequest(BaseModel):
    username: str
    password: str


class RefreshRequest(BaseModel):
    refresh_token: str


class TokenResponse(BaseModel):
    access_token: str
    refresh_token: str = ""
    token_type: str = "bearer"
    user: Dict[str, Any] = Field(default_factory=dict)


# ---------------------------------------------------------------------------
# Prediction
# ---------------------------------------------------------------------------

class PredictionRequest(BaseModel):
    features: List[float]
    model_id: str = "default"
    explain: bool = False


class PredictionResponse(BaseModel):
    prediction: float
    probability: Optional[List[float]] = None
    model_id: str
    explanation: Optional[Dict[str, Any]] = None
    processing_time_ms: float = 0.0


class _SinglePredictionItem(BaseModel):
    features: List[float]
    model_id: str = "default"


class BatchPredictionRequest(BaseModel):
    requests: List[_SinglePredictionItem]


# ---------------------------------------------------------------------------
# Health
# ---------------------------------------------------------------------------

class HealthResponse(BaseModel):
    status: str
    version: str
    environment: str
    cpu_percent: float = 0.0
    memory_percent: float = 0.0
    models_count: int = 0
    celery_worker_connected: bool = False
    celery_active_tasks: int = 0


# ---------------------------------------------------------------------------
# Models
# ---------------------------------------------------------------------------

class TrainRequest(BaseModel):
    model_type: str = "random_forest"
    task: str = "classification"
    model_id: str = "default"
    target_column: str = "target"
    params: Optional[Dict[str, Any]] = None


# ---------------------------------------------------------------------------
# AutoML
# ---------------------------------------------------------------------------

class AutoMLRequest(BaseModel):
    task: str = Field("classification", description="'classification' or 'regression'")
    target_column: str = "target"
    time_budget: int = Field(60, ge=10, le=600)
    n_trials: int = Field(20, ge=1, le=200)
    scoring_metric: Optional[str] = None


# ---------------------------------------------------------------------------
# Experiments
# ---------------------------------------------------------------------------

class CreateExperimentRequest(BaseModel):
    name: str
    description: str = ""
    tags: Optional[Dict[str, str]] = None


class StartRunRequest(BaseModel):
    name: str = ""
    params: Optional[Dict[str, Any]] = None
    tags: Optional[Dict[str, str]] = None


class LogMetricsRequest(BaseModel):
    metrics: Dict[str, float]


# ---------------------------------------------------------------------------
# Explainability
# ---------------------------------------------------------------------------

class ExplainRequest(BaseModel):
    model_id: str
    features: List[float]
    explain_type: str = Field("local", description="'local' or 'global'")


# ---------------------------------------------------------------------------
# Drift
# ---------------------------------------------------------------------------

class DriftCheckRequest(BaseModel):
    model_id: str
    reference_data: List[Dict[str, Any]]
    current_data: List[Dict[str, Any]]
    feature_names: Optional[List[str]] = None


# ---------------------------------------------------------------------------
# Admin
# ---------------------------------------------------------------------------

class CreateUserRequest(BaseModel):
    username: str
    email: str = ""
    role: str = "viewer"
    password: Optional[str] = None
