"""Experiment execution endpoints."""

import uuid
from fastapi import APIRouter, HTTPException
from schemas import ExperimentSubmitRequest, ExperimentResult, ExperimentStatus

router = APIRouter()

# In-memory run registry (will be replaced with task queue in production)
_runs: dict[str, dict] = {}


@router.post("/submit", response_model=ExperimentResult)
async def submit_experiment(request: ExperimentSubmitRequest):
    run_id = str(uuid.uuid4())
    _runs[run_id] = {
        "experiment_id": request.experiment_id,
        "name": request.name,
        "dataset_path": request.dataset_path,
        "status": "accepted",
        "progress": 0.0,
    }
    return ExperimentResult(run_id=run_id, accepted=True)


@router.get("/{run_id}/status", response_model=ExperimentStatus)
async def get_status(run_id: str):
    run = _runs.get(run_id)
    if run is None:
        raise HTTPException(status_code=404, detail="Run not found")
    return ExperimentStatus(
        run_id=run_id,
        status=run["status"],
        progress=run.get("progress"),
    )


@router.post("/{run_id}/cancel")
async def cancel_experiment(run_id: str):
    run = _runs.get(run_id)
    if run is None:
        raise HTTPException(status_code=404, detail="Run not found")
    run["status"] = "cancelled"
    return {"cancelled": True}
