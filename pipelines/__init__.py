"""
pipelines — Pipeline layer for Aimran Data Science Lab.

Public exports
--------------
ModelTrainingPipeline   — sklearn-based end-to-end training pipeline
PipelineOrchestrator    — enterprise orchestrator with DAG, retry, history
PipelineStep            — step definition dataclass
Pipeline                — pipeline definition dataclass
PipelineRun             — run record dataclass

Note: Imports are lazy to avoid blocking on optional heavy dependencies
(e.g. MLflow) at package import time.  Access any name from this module
and it will be resolved on first use.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Only imported during static analysis / type-checking — never at runtime.
    from pipelines.training_pipeline import ModelTrainingPipeline
    from pipelines.orchestrator import (
        PipelineOrchestrator,
        PipelineStep,
        Pipeline,
        PipelineRun,
    )


def __getattr__(name: str):  # PEP 562 — module-level lazy attribute resolution
    """Lazily resolve names to avoid blocking imports at package load time."""
    if name == "ModelTrainingPipeline":
        from pipelines.training_pipeline import ModelTrainingPipeline
        return ModelTrainingPipeline

    if name == "PipelineOrchestrator":
        from pipelines.orchestrator import PipelineOrchestrator
        return PipelineOrchestrator

    if name == "PipelineStep":
        from pipelines.orchestrator import PipelineStep
        return PipelineStep

    if name == "Pipeline":
        from pipelines.orchestrator import Pipeline
        return Pipeline

    if name == "PipelineRun":
        from pipelines.orchestrator import PipelineRun
        return PipelineRun

    raise AttributeError(f"module 'pipelines' has no attribute {name!r}")


__all__ = [
    "ModelTrainingPipeline",
    "PipelineOrchestrator",
    "PipelineStep",
    "Pipeline",
    "PipelineRun",
]
