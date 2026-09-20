"""Pydantic schemas mirroring the C# domain models."""

from pydantic import BaseModel, Field
from typing import Optional
from datetime import datetime


class ExperimentSubmitRequest(BaseModel):
    experiment_id: str
    name: str
    dataset_path: str
    hyperparameters: dict = Field(default_factory=dict)
    compute_target: str = "local"


class ExperimentResult(BaseModel):
    run_id: str
    accepted: bool
    error_message: Optional[str] = None


class ExperimentStatus(BaseModel):
    run_id: str
    status: str
    progress: Optional[float] = None
    current_metrics: Optional[dict[str, float]] = None
    error_message: Optional[str] = None


class MetricUpdate(BaseModel):
    metric_name: str
    value: float
    epoch: int
    timestamp: datetime = Field(default_factory=datetime.utcnow)


class TrainRequest(BaseModel):
    dataset_path: str
    algorithm: str
    framework: str
    hyperparameters: Optional[dict] = None
    train_test_split: float = 0.8


class TrainResult(BaseModel):
    model_path: str
    metrics: dict[str, float]
    training_duration_seconds: float


class EvalRequest(BaseModel):
    model_path: str
    test_dataset_path: str


class EvalResult(BaseModel):
    metrics: dict[str, float]
    test_sample_count: int


class PredictRequest(BaseModel):
    model_path: str
    input_data: list[dict]


class PredictResult(BaseModel):
    predictions: list
    probabilities: Optional[list[float]] = None


class ProfileRequest(BaseModel):
    dataset_path: str


class ColumnProfile(BaseModel):
    name: str
    data_type: str
    null_count: int = 0
    null_percentage: float = 0.0
    unique_count: int = 0
    mean: Optional[float] = None
    std_dev: Optional[float] = None
    min: Optional[float] = None
    max: Optional[float] = None


class ProfileResult(BaseModel):
    row_count: int
    column_count: int
    columns: list[ColumnProfile]
    quality_score: float


class OutlierRequest(BaseModel):
    dataset_path: str
    method: str = "zscore"
    parameters: Optional[dict] = None


class OutlierResult(BaseModel):
    method: str
    outlier_count: int
    outlier_indices: list[int]
    outlier_scores: list[float]


class CleaningRecommendRequest(BaseModel):
    dataset_path: str
    existing_profile: Optional[dict] = None


class RecommendedOperation(BaseModel):
    operation_type: str
    target_column: str
    reason: str
    confidence: float
    parameters: Optional[dict] = None


class CleaningRecommendation(BaseModel):
    operations: list[RecommendedOperation]
    estimated_quality_improvement: float


# ── Code Execution ────────────────────────────────────────────────────

class CodeExecuteRequest(BaseModel):
    code: str
    timeout_seconds: int = 30
    working_directory: Optional[str] = None


class CodeExecuteResult(BaseModel):
    stdout: str
    stderr: str
    exit_code: int
    execution_time_seconds: float
    has_figure: bool = False
    figure_base64: Optional[str] = None


# ── Package Management ────────────────────────────────────────────────

class PackageInfo(BaseModel):
    name: str
    version: str
    summary: Optional[str] = None
    latest_version: Optional[str] = None


class PackageListResult(BaseModel):
    packages: list[PackageInfo]


class PackageInstallRequest(BaseModel):
    package_name: str
    version: Optional[str] = None


class PackageActionResult(BaseModel):
    success: bool
    package_name: str
    message: str


# ── Visualization ─────────────────────────────────────────────────────

class VisualizationRequest(BaseModel):
    dataset_path: str
    chart_type: str  # line, bar, scatter, histogram, box, heatmap, correlation
    x_column: Optional[str] = None
    y_column: Optional[str] = None
    columns: Optional[list[str]] = None
    title: Optional[str] = None
    options: Optional[dict] = None


class VisualizationResult(BaseModel):
    chart_type: str
    figure_base64: str
    python_code: str
