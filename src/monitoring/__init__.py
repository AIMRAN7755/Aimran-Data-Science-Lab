"""
src.monitoring
==============
Production monitoring sub-package for the Aimran Data Science Lab.

Exports
-------
MetricsCollector
    Collects and stores API, DB, and system performance metrics.

SystemMonitor
    Wraps psutil to capture live CPU/memory/disk readings.

PerformanceMiddleware
    ASGI middleware for automatic per-request latency recording.

metrics_collector
    Module-level singleton ``MetricsCollector`` instance.

system_monitor
    Module-level singleton ``SystemMonitor`` instance.

ExperimentTracker
    MLflow-style experiment and run tracking.

DataVersionControl
    Dataset versioning and lineage tracking.

DataDriftDetector
    Statistical data drift detection (KS test for numeric, Chi-squared for
    categorical) per feature column against a fitted reference distribution.

ModelPerformanceDriftDetector
    Rolling-window model accuracy / F1 trend analysis with degradation
    alerts.
"""

from .performance import (
    metrics_collector,
    system_monitor,
    PerformanceMiddleware,
    MetricsCollector,
    SystemMonitor,
)
from .metrics import ExperimentTracker, DataVersionControl
from .drift_detector import (
    DataDriftDetector,
    ModelPerformanceDriftDetector,
    DriftReport,
)

__all__ = [
    # Performance monitoring
    "metrics_collector",
    "system_monitor",
    "PerformanceMiddleware",
    "MetricsCollector",
    "SystemMonitor",
    # Experiment tracking
    "ExperimentTracker",
    "DataVersionControl",
    # Drift detection
    "DataDriftDetector",
    "ModelPerformanceDriftDetector",
    "DriftReport",
]
