"""
Enterprise Pipeline Orchestrator for Aimran Data Science Lab.

Pure-Python orchestration engine — no Airflow, Celery, or subprocess calls
required.  Pipelines are graphs of ``PipelineStep`` nodes whose outputs are
automatically wired to the inputs of downstream steps.  Run history is
persisted as JSON files under ``logs/pipeline_runs/``.
"""

from __future__ import annotations

import json
import logging
import re
import threading
import time
import uuid
from dataclasses import dataclass, field, asdict
from datetime import datetime
from pathlib import Path
from typing import Any, Callable, Dict, List, Optional

logger = logging.getLogger(__name__)

_RUNS_DIR = Path("logs/pipeline_runs")


# ---------------------------------------------------------------------------
# Dataclasses
# ---------------------------------------------------------------------------

@dataclass
class PipelineStep:
    """
    A single executable node in a pipeline.

    Attributes
    ----------
    name:
        Unique name within the pipeline; used as the key in the shared
        data context.
    function:
        Callable that receives a dict of named inputs and returns a dict
        of named outputs.  Signature: ``fn(inputs: dict) -> dict``.
    inputs:
        Names of context keys this step reads.
    outputs:
        Names of context keys this step writes.
    retry_count:
        Number of additional attempts on failure (0 = try once).
    timeout_seconds:
        Maximum wall-clock seconds for one attempt (0 = no limit).
    tags:
        Arbitrary labels (e.g. ``["training", "gpu"]``).
    """

    name: str
    function: Callable[[Dict[str, Any]], Dict[str, Any]]
    inputs: List[str] = field(default_factory=list)
    outputs: List[str] = field(default_factory=list)
    retry_count: int = 0
    timeout_seconds: float = 0
    tags: List[str] = field(default_factory=list)


@dataclass
class Pipeline:
    """Registered pipeline definition."""

    pipeline_id: str
    name: str
    steps: List[PipelineStep]
    schedule: str = ""         # cron expression, empty = manual only
    last_run: Optional[str] = None   # ISO-8601 timestamp
    status: str = "idle"       # idle | running | failed | completed


@dataclass
class PipelineRun:
    """Record of a single pipeline execution."""

    run_id: str
    pipeline_id: str
    start_time: str
    end_time: str = ""
    status: str = "running"    # running | completed | failed | cancelled
    steps_results: Dict[str, Any] = field(default_factory=dict)
    error: str = ""

    def to_dict(self) -> Dict[str, Any]:
        d = asdict(self)
        return d

    @classmethod
    def from_dict(cls, d: Dict[str, Any]) -> "PipelineRun":
        d.pop("function", None)  # function is not serialisable
        return cls(**d)


# ---------------------------------------------------------------------------
# Orchestrator
# ---------------------------------------------------------------------------

class PipelineOrchestrator:
    """
    Register, run, and inspect pipelines.

    All run records are written to ``logs/pipeline_runs/{run_id}.json`` so
    that history survives restarts.

    Example
    -------
    ::

        orch = PipelineOrchestrator()
        pipe = orch.register_pipeline("my_pipe", [step1, step2])
        run  = orch.run_pipeline(pipe.pipeline_id, inputs={"raw_df": df})
        print(run.status)   # "completed"
    """

    def __init__(self, runs_dir: Optional[Path] = None) -> None:
        self._runs_dir = Path(runs_dir) if runs_dir else _RUNS_DIR
        self._runs_dir.mkdir(parents=True, exist_ok=True)

        self._pipelines: Dict[str, Pipeline] = {}
        self._active_runs: Dict[str, threading.Event] = {}   # run_id → cancel event

    # ------------------------------------------------------------------
    # Registration
    # ------------------------------------------------------------------

    def register_pipeline(self, name: str, steps: List[PipelineStep]) -> Pipeline:
        """
        Create and store a new pipeline.

        Parameters
        ----------
        name:
            Human-readable pipeline name.
        steps:
            Ordered list of ``PipelineStep`` objects.

        Returns
        -------
        Pipeline
        """
        pipeline_id = uuid.uuid4().hex[:12]
        pipeline = Pipeline(
            pipeline_id=pipeline_id,
            name=name,
            steps=steps,
        )
        self._pipelines[pipeline_id] = pipeline
        logger.info("Registered pipeline '%s' (id=%s, %d steps).", name, pipeline_id, len(steps))
        return pipeline

    # ------------------------------------------------------------------
    # Execution
    # ------------------------------------------------------------------

    def run_pipeline(
        self,
        pipeline_id: str,
        inputs: Optional[Dict[str, Any]] = None,
    ) -> PipelineRun:
        """
        Execute all steps in a pipeline sequentially.

        Steps share a mutable *context* dict.  After each step the context
        is updated with the step's output dict, making those values available
        to downstream steps by name.

        Errors are retried up to ``step.retry_count`` times.  A step that
        exhausts its retries aborts the run.

        Parameters
        ----------
        pipeline_id:
            Must match a previously registered pipeline.
        inputs:
            Initial context values (e.g. raw DataFrames, config dicts).

        Returns
        -------
        PipelineRun
            Persisted run record with final status.
        """
        pipeline = self._get_pipeline(pipeline_id)
        run_id = uuid.uuid4().hex[:12]
        cancel_event = threading.Event()
        self._active_runs[run_id] = cancel_event

        run = PipelineRun(
            run_id=run_id,
            pipeline_id=pipeline_id,
            start_time=datetime.utcnow().isoformat(),
        )
        pipeline.status = "running"
        logger.info("Starting run %s for pipeline '%s'.", run_id, pipeline.name)

        context: Dict[str, Any] = dict(inputs or {})

        try:
            for step in pipeline.steps:
                if cancel_event.is_set():
                    run.status = "cancelled"
                    logger.warning("Run %s cancelled before step '%s'.", run_id, step.name)
                    break

                step_result = self._execute_step(step, context, cancel_event)
                run.steps_results[step.name] = {
                    "status": step_result["status"],
                    "attempts": step_result["attempts"],
                    "duration_seconds": step_result["duration_seconds"],
                    "error": step_result.get("error", ""),
                }

                if step_result["status"] == "failed":
                    run.status = "failed"
                    run.error = f"Step '{step.name}' failed: {step_result.get('error', '')}"
                    logger.error("Run %s failed at step '%s'.", run_id, step.name)
                    break

                # Merge step outputs into context
                context.update(step_result.get("outputs", {}))

            else:
                # All steps completed without breaking
                if run.status == "running":
                    run.status = "completed"

        except Exception as exc:  # noqa: BLE001
            run.status = "failed"
            run.error = str(exc)
            logger.exception("Unexpected error in run %s.", run_id)

        finally:
            run.end_time = datetime.utcnow().isoformat()
            pipeline.status = run.status
            pipeline.last_run = run.end_time
            self._persist_run(run)
            self._active_runs.pop(run_id, None)

        logger.info("Run %s finished with status '%s'.", run_id, run.status)
        return run

    def cancel_run(self, run_id: str) -> bool:
        """
        Signal a running pipeline to stop after the current step.

        Returns
        -------
        bool
            ``True`` if a cancel signal was sent; ``False`` if the run was
            not found (already finished or never started).
        """
        event = self._active_runs.get(run_id)
        if event is None:
            return False
        event.set()
        logger.info("Cancel signal sent to run %s.", run_id)
        return True

    # ------------------------------------------------------------------
    # Scheduling (parse / validate only)
    # ------------------------------------------------------------------

    def schedule_pipeline(self, pipeline_id: str, cron_expression: str) -> None:
        """
        Attach a cron schedule string to a pipeline.

        The expression is validated but *not* executed automatically — a
        separate scheduler (e.g. APScheduler, cron, Windows Task Scheduler)
        should call ``run_pipeline`` at the appropriate times.

        Raises
        ------
        ValueError
            If the cron expression is structurally invalid.
        """
        self._validate_cron(cron_expression)
        pipeline = self._get_pipeline(pipeline_id)
        pipeline.schedule = cron_expression
        logger.info(
            "Scheduled pipeline '%s' with expression '%s'.",
            pipeline.name, cron_expression,
        )

    # ------------------------------------------------------------------
    # History and DAG
    # ------------------------------------------------------------------

    def get_run_history(self, pipeline_id: str) -> List[PipelineRun]:
        """
        Load all persisted run records for *pipeline_id*, newest first.
        """
        runs: List[PipelineRun] = []
        for json_file in sorted(self._runs_dir.glob("*.json"), reverse=True):
            try:
                with json_file.open("r", encoding="utf-8") as fh:
                    data = json.load(fh)
                if data.get("pipeline_id") == pipeline_id:
                    runs.append(PipelineRun.from_dict(data))
            except (json.JSONDecodeError, TypeError):
                logger.debug("Skipping malformed run file: %s", json_file.name)
        return runs

    def get_pipeline_dag(self, pipeline_id: str) -> Dict[str, Any]:
        """
        Return a DAG description of the pipeline.

        The DAG is expressed as an adjacency list:

        .. code-block:: json

            {
              "nodes": [{"name": "...", "inputs": [...], "outputs": [...]}],
              "edges": [{"from": "step_a", "to": "step_b", "via": "col_name"}]
            }

        An edge ``A → B`` is drawn when any output of step A matches an input
        of step B.
        """
        pipeline = self._get_pipeline(pipeline_id)
        nodes = [
            {"name": s.name, "inputs": s.inputs, "outputs": s.outputs, "tags": s.tags}
            for s in pipeline.steps
        ]

        # Build output → step mapping
        output_to_step: Dict[str, str] = {}
        for step in pipeline.steps:
            for out in step.outputs:
                output_to_step[out] = step.name

        edges: List[Dict[str, str]] = []
        for step in pipeline.steps:
            for inp in step.inputs:
                producer = output_to_step.get(inp)
                if producer and producer != step.name:
                    edges.append({"from": producer, "to": step.name, "via": inp})

        return {
            "pipeline_id": pipeline_id,
            "name": pipeline.name,
            "nodes": nodes,
            "edges": edges,
        }

    # ------------------------------------------------------------------
    # Built-in pipeline templates
    # ------------------------------------------------------------------

    def create_training_pipeline(self) -> Pipeline:
        """
        Return a pre-configured training pipeline template.

        Steps
        -----
        1. ``load_data``       — reads ``raw_df`` from context, passes it through
        2. ``preprocess``      — cleans and encodes ``raw_df`` → ``processed_df``
        3. ``feature_engineering`` — selects/creates features → ``features``
        4. ``train_model``     — trains a model on ``features`` → ``model``
        5. ``evaluate``        — scores ``model`` on ``features`` → ``metrics``
        """
        def _load_data(inputs: Dict[str, Any]) -> Dict[str, Any]:
            import pandas as pd
            df = inputs.get("raw_df", pd.DataFrame())
            logger.info("[training] load_data: %d rows.", len(df))
            return {"raw_df": df}

        def _preprocess(inputs: Dict[str, Any]) -> Dict[str, Any]:
            import pandas as pd
            import numpy as np
            df: pd.DataFrame = inputs["raw_df"].copy()
            # Drop full-NA columns, fill numeric NaNs with median
            df = df.dropna(axis=1, how="all")
            num_cols = df.select_dtypes(include=[np.number]).columns
            df[num_cols] = df[num_cols].fillna(df[num_cols].median())
            logger.info("[training] preprocess: %d rows after cleaning.", len(df))
            return {"processed_df": df}

        def _feature_engineering(inputs: Dict[str, Any]) -> Dict[str, Any]:
            import numpy as np
            df = inputs["processed_df"]
            num_cols = df.select_dtypes(include=[np.number]).columns.tolist()
            features = df[num_cols] if num_cols else df
            logger.info("[training] feature_engineering: %d features.", len(features.columns))
            return {"features": features}

        def _train_model(inputs: Dict[str, Any]) -> Dict[str, Any]:
            from sklearn.ensemble import RandomForestClassifier
            import numpy as np
            features = inputs["features"]
            if features.empty or len(features.columns) < 2:
                return {"model": None}
            X = features.iloc[:, :-1]
            y = features.iloc[:, -1]
            model = RandomForestClassifier(n_estimators=50, random_state=42)
            model.fit(X, y.astype(int) if np.issubdtype(y.dtype, np.floating) else y)
            logger.info("[training] train_model: model fitted.")
            return {"model": model}

        def _evaluate(inputs: Dict[str, Any]) -> Dict[str, Any]:
            from sklearn.metrics import accuracy_score
            import numpy as np
            model = inputs.get("model")
            features = inputs["features"]
            if model is None or features.empty or len(features.columns) < 2:
                return {"metrics": {}}
            X = features.iloc[:, :-1]
            y = features.iloc[:, -1]
            preds = model.predict(X)
            metrics = {"accuracy": float(accuracy_score(y.astype(int) if np.issubdtype(y.dtype, np.floating) else y, preds))}
            logger.info("[training] evaluate: %s", metrics)
            return {"metrics": metrics}

        steps = [
            PipelineStep("load_data", _load_data, inputs=["raw_df"], outputs=["raw_df"], tags=["io"]),
            PipelineStep("preprocess", _preprocess, inputs=["raw_df"], outputs=["processed_df"], tags=["data"]),
            PipelineStep("feature_engineering", _feature_engineering, inputs=["processed_df"], outputs=["features"], tags=["features"]),
            PipelineStep("train_model", _train_model, inputs=["features"], outputs=["model"], retry_count=1, tags=["training"]),
            PipelineStep("evaluate", _evaluate, inputs=["model", "features"], outputs=["metrics"], tags=["evaluation"]),
        ]
        return self.register_pipeline("training_pipeline_template", steps)

    def create_data_processing_pipeline(self) -> Pipeline:
        """
        Return a pre-configured data-processing pipeline template.

        Steps
        -----
        1. ``ingest``   — validates presence of ``raw_df``
        2. ``validate`` — runs auto quality profile
        3. ``transform`` — type-optimises the DataFrame
        4. ``export``   — logs the final shape
        """
        def _ingest(inputs: Dict[str, Any]) -> Dict[str, Any]:
            import pandas as pd
            df = inputs.get("raw_df", pd.DataFrame())
            if df.empty:
                raise ValueError("raw_df is empty — nothing to process.")
            logger.info("[processing] ingest: %d rows, %d cols.", len(df), len(df.columns))
            return {"raw_df": df}

        def _validate(inputs: Dict[str, Any]) -> Dict[str, Any]:
            from src.data.quality import DataQualityEngine
            df = inputs["raw_df"]
            engine = DataQualityEngine()
            report = engine.auto_profile(df, dataset_name="processing_input")
            logger.info("[processing] validate: quality score=%.1f.", report.score)
            return {"quality_report": report}

        def _transform(inputs: Dict[str, Any]) -> Dict[str, Any]:
            import numpy as np
            df = inputs["raw_df"].copy()
            # Downcast numerics
            for col in df.select_dtypes(include=["float64"]).columns:
                df[col] = df[col].astype(np.float32)
            for col in df.select_dtypes(include=["int64"]).columns:
                df[col] = pd.to_numeric(df[col], downcast="integer")
            logger.info("[processing] transform complete.")
            return {"processed_df": df}

        def _export(inputs: Dict[str, Any]) -> Dict[str, Any]:
            df = inputs["processed_df"]
            logger.info("[processing] export: final shape=%s.", df.shape)
            return {"export_shape": list(df.shape)}

        steps = [
            PipelineStep("ingest", _ingest, inputs=["raw_df"], outputs=["raw_df"], tags=["io"]),
            PipelineStep("validate", _validate, inputs=["raw_df"], outputs=["quality_report"], tags=["quality"]),
            PipelineStep("transform", _transform, inputs=["raw_df"], outputs=["processed_df"], tags=["data"]),
            PipelineStep("export", _export, inputs=["processed_df"], outputs=["export_shape"], tags=["io"]),
        ]
        return self.register_pipeline("data_processing_pipeline_template", steps)

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    def _get_pipeline(self, pipeline_id: str) -> Pipeline:
        if pipeline_id not in self._pipelines:
            raise KeyError(f"Pipeline '{pipeline_id}' not registered.")
        return self._pipelines[pipeline_id]

    def _execute_step(
        self,
        step: PipelineStep,
        context: Dict[str, Any],
        cancel_event: threading.Event,
    ) -> Dict[str, Any]:
        """Run one step with retry and optional timeout support."""
        step_inputs = {k: context[k] for k in step.inputs if k in context}
        max_attempts = step.retry_count + 1
        last_error = ""

        for attempt in range(1, max_attempts + 1):
            if cancel_event.is_set():
                return {"status": "cancelled", "attempts": attempt, "duration_seconds": 0}

            t_start = time.perf_counter()
            try:
                if step.timeout_seconds > 0:
                    result = self._run_with_timeout(step.function, step_inputs, step.timeout_seconds)
                else:
                    result = step.function(step_inputs)

                duration = round(time.perf_counter() - t_start, 3)
                logger.info(
                    "Step '%s' completed in %.3fs (attempt %d/%d).",
                    step.name, duration, attempt, max_attempts,
                )
                return {
                    "status": "completed",
                    "attempts": attempt,
                    "duration_seconds": duration,
                    "outputs": result or {},
                }

            except Exception as exc:  # noqa: BLE001
                duration = round(time.perf_counter() - t_start, 3)
                last_error = str(exc)
                logger.warning(
                    "Step '%s' attempt %d/%d failed after %.3fs: %s",
                    step.name, attempt, max_attempts, duration, exc,
                )
                if attempt < max_attempts:
                    time.sleep(min(2 ** attempt, 30))   # exponential back-off, cap at 30 s

        return {
            "status": "failed",
            "attempts": max_attempts,
            "duration_seconds": 0,
            "error": last_error,
        }

    @staticmethod
    def _run_with_timeout(
        fn: Callable[[Dict[str, Any]], Dict[str, Any]],
        inputs: Dict[str, Any],
        timeout: float,
    ) -> Dict[str, Any]:
        """Execute *fn* in a daemon thread and raise ``TimeoutError`` on expiry."""
        result_container: List[Any] = [None, None]  # [result, exception]

        def _target() -> None:
            try:
                result_container[0] = fn(inputs)
            except Exception as exc:  # noqa: BLE001
                result_container[1] = exc

        thread = threading.Thread(target=_target, daemon=True)
        thread.start()
        thread.join(timeout)

        if thread.is_alive():
            raise TimeoutError(f"Step exceeded timeout of {timeout}s.")
        if result_container[1] is not None:
            raise result_container[1]
        return result_container[0] or {}

    def _persist_run(self, run: PipelineRun) -> None:
        path = self._runs_dir / f"{run.run_id}.json"
        with path.open("w", encoding="utf-8") as fh:
            json.dump(run.to_dict(), fh, indent=2, default=str)
        logger.debug("Persisted run record to %s.", path)

    # ------------------------------------------------------------------
    # Cron validation
    # ------------------------------------------------------------------

    @staticmethod
    def _validate_cron(expr: str) -> None:
        """
        Validate that *expr* looks like a standard 5-field cron expression.

        Fields: minute hour day-of-month month day-of-week.
        Each field may contain digits, ``*``, ``/``, ``-``, or ``,``.

        Raises
        ------
        ValueError
            If the expression is malformed.
        """
        fields = expr.strip().split()
        if len(fields) != 5:
            raise ValueError(
                f"Cron expression must have exactly 5 fields (got {len(fields)}): '{expr}'"
            )
        field_pattern = re.compile(r"^(\*|[0-9]+)([,/\-][0-9*]+)*$")
        names = ["minute", "hour", "day-of-month", "month", "day-of-week"]
        for name, f in zip(names, fields):
            if not field_pattern.match(f):
                raise ValueError(
                    f"Invalid cron field for {name}: '{f}' in expression '{expr}'"
                )
