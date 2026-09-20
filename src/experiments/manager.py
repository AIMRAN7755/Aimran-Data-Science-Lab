"""
Experiment tracking — lightweight in-process alternative to MLflow
that works without any external service.

Supports creating experiments, starting runs, logging metrics/params,
and querying the best run for a given metric.
"""

from __future__ import annotations

import time
import uuid
import logging
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Data classes
# ---------------------------------------------------------------------------

@dataclass
class ExperimentRun:
    """A single training run inside an experiment."""

    run_id: str
    experiment_id: str
    name: str
    status: str = "running"       # running | finished | failed
    params: Dict[str, Any] = field(default_factory=dict)
    metrics: Dict[str, float] = field(default_factory=dict)
    tags: Dict[str, str] = field(default_factory=dict)
    started_at: float = field(default_factory=time.time)
    finished_at: Optional[float] = None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "run_id": self.run_id,
            "experiment_id": self.experiment_id,
            "name": self.name,
            "status": self.status,
            "params": self.params,
            "metrics": self.metrics,
            "tags": self.tags,
            "started_at": self.started_at,
            "finished_at": self.finished_at,
            "duration_s": (
                round((self.finished_at or time.time()) - self.started_at, 3)
            ),
        }

    def finish(self) -> "ExperimentRun":
        self.status = "finished"
        self.finished_at = time.time()
        return self


@dataclass
class Experiment:
    """A named collection of runs."""

    experiment_id: str
    name: str
    description: str = ""
    tags: Dict[str, str] = field(default_factory=dict)
    created_at: float = field(default_factory=time.time)
    runs: Dict[str, ExperimentRun] = field(default_factory=dict)

    def to_dict(self, include_runs: bool = True) -> Dict[str, Any]:
        d: Dict[str, Any] = {
            "experiment_id": self.experiment_id,
            "name": self.name,
            "description": self.description,
            "tags": self.tags,
            "created_at": self.created_at,
            "run_count": len(self.runs),
        }
        if include_runs:
            d["runs"] = [r.to_dict() for r in self.runs.values()]
        return d

    def summary(self) -> Dict[str, Any]:
        finished = [r for r in self.runs.values() if r.status == "finished"]
        return {
            "total_runs": len(self.runs),
            "finished_runs": len(finished),
            "running_runs": len([r for r in self.runs.values() if r.status == "running"]),
            "failed_runs": len([r for r in self.runs.values() if r.status == "failed"]),
        }


# ---------------------------------------------------------------------------
# Manager
# ---------------------------------------------------------------------------

class ExperimentManager:
    """
    Manages experiments and runs in memory.

    Thread-safe for single-process use.  For multi-process / distributed
    scenarios, swap the in-memory dicts for a shared store (Redis / DB).
    """

    def __init__(self):
        self._experiments: Dict[str, Experiment] = {}
        self._runs: Dict[str, ExperimentRun] = {}  # run_id -> run (global lookup)

    # ------------------------------------------------------------------
    # Experiments
    # ------------------------------------------------------------------

    def create_experiment(
        self,
        name: str,
        description: str = "",
        tags: Optional[Dict[str, str]] = None,
    ) -> Experiment:
        exp = Experiment(
            experiment_id=str(uuid.uuid4()),
            name=name,
            description=description,
            tags=tags or {},
        )
        self._experiments[exp.experiment_id] = exp
        logger.info("Experiment created: '%s' (%s)", name, exp.experiment_id)
        return exp

    def get_experiment(self, experiment_id: str) -> Optional[Experiment]:
        return self._experiments.get(experiment_id)

    def list_experiments(self) -> List[Dict[str, Any]]:
        return [e.to_dict(include_runs=False) for e in self._experiments.values()]

    # ------------------------------------------------------------------
    # Runs
    # ------------------------------------------------------------------

    def start_run(
        self,
        experiment_id: str,
        name: str = "",
        params: Optional[Dict[str, Any]] = None,
        tags: Optional[Dict[str, str]] = None,
    ) -> ExperimentRun:
        exp = self._experiments.get(experiment_id)
        if exp is None:
            raise KeyError(f"Experiment '{experiment_id}' not found")
        run = ExperimentRun(
            run_id=str(uuid.uuid4()),
            experiment_id=experiment_id,
            name=name or f"run_{len(exp.runs) + 1}",
            params=params or {},
            tags=tags or {},
        )
        exp.runs[run.run_id] = run
        self._runs[run.run_id] = run
        logger.info("Run started: '%s' in experiment '%s'", run.run_id, experiment_id)
        return run

    def log_metrics(self, run_id: str, metrics: Dict[str, float]) -> ExperimentRun:
        run = self._runs.get(run_id)
        if run is None:
            raise KeyError(f"Run '{run_id}' not found")
        run.metrics.update(metrics)
        run.finish()
        logger.info("Run '%s' metrics logged: %s", run_id, metrics)
        return run

    def get_best_run(
        self,
        experiment_id: str,
        metric: str = "accuracy",
        higher_is_better: bool = True,
    ) -> Optional[Dict[str, Any]]:
        exp = self._experiments.get(experiment_id)
        if exp is None:
            return None
        finished = [r for r in exp.runs.values() if r.status == "finished" and metric in r.metrics]
        if not finished:
            return None
        best = max(finished, key=lambda r: r.metrics[metric]) if higher_is_better else min(
            finished, key=lambda r: r.metrics[metric]
        )
        return {**best.to_dict(), "best_metric": {metric: best.metrics[metric]}}
