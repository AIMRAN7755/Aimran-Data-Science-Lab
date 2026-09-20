"""
Structured audit logger — records all security-relevant actions
(logins, model operations, user management) in a FIFO in-memory log.

In production: persist entries to a DB table or a SIEM-compatible log sink.
"""

from __future__ import annotations

import logging
import time
import uuid
from collections import deque
from typing import Any, Deque, Dict, List, Optional

logger = logging.getLogger(__name__)

_MAX_ENTRIES = 10_000


def _entry(
    action: str,
    actor_id: str,
    actor_name: str,
    resource_type: str = "",
    resource_id: str = "",
    outcome: str = "success",
    details: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    return {
        "entry_id":     str(uuid.uuid4()),
        "timestamp":    time.time(),
        "action":       action,
        "actor_id":     actor_id,
        "actor_name":   actor_name,
        "resource_type": resource_type,
        "resource_id":  resource_id,
        "outcome":      outcome,
        "details":      details or {},
    }


class AuditLogger:
    """Thread-safe in-memory audit log."""

    def __init__(self, max_entries: int = _MAX_ENTRIES):
        self._log: Deque[Dict[str, Any]] = deque(maxlen=max_entries)

    # ------------------------------------------------------------------
    # Auth events
    # ------------------------------------------------------------------

    def login(self, username: str, success: bool, details: Optional[Dict] = None) -> None:
        self._record(_entry(
            action="auth.login",
            actor_id="", actor_name=username,
            resource_type="session", resource_id="",
            outcome="success" if success else "failure",
            details=details,
        ))

    def token_refresh(self, user_id: str, username: str, success: bool) -> None:
        self._record(_entry(
            action="auth.token_refresh",
            actor_id=user_id, actor_name=username,
            outcome="success" if success else "failure",
        ))

    # ------------------------------------------------------------------
    # Model events
    # ------------------------------------------------------------------

    def model_registered(
        self, user_id: str, username: str,
        model_id: str, model_type: str, task: str,
    ) -> None:
        self._record(_entry(
            action="model.registered",
            actor_id=user_id, actor_name=username,
            resource_type="model", resource_id=model_id,
            details={"model_type": model_type, "task": task},
        ))

    def model_trained(
        self, user_id: str, username: str,
        model_id: str, rows: int, features: int,
        metrics: Dict[str, float],
    ) -> None:
        self._record(_entry(
            action="model.trained",
            actor_id=user_id, actor_name=username,
            resource_type="model", resource_id=model_id,
            details={"rows": rows, "features": features, "metrics": metrics},
        ))

    def model_deleted(self, user_id: str, username: str, model_id: str) -> None:
        self._record(_entry(
            action="model.deleted",
            actor_id=user_id, actor_name=username,
            resource_type="model", resource_id=model_id,
        ))

    def champion_promoted(
        self, user_id: str, username: str,
        model_id: str, version_info: Dict[str, Any],
    ) -> None:
        self._record(_entry(
            action="model.champion_promoted",
            actor_id=user_id, actor_name=username,
            resource_type="model", resource_id=model_id,
            details={"version_info": version_info},
        ))

    # ------------------------------------------------------------------
    # AutoML events
    # ------------------------------------------------------------------

    def automl_run(
        self, user_id: str, username: str,
        job_id: str, task: str, outcome: str,
        details: Optional[Dict] = None,
    ) -> None:
        self._record(_entry(
            action="automl.run",
            actor_id=user_id, actor_name=username,
            resource_type="automl_job", resource_id=job_id,
            outcome=outcome,
            details={"task": task, **(details or {})},
        ))

    # ------------------------------------------------------------------
    # Experiment events
    # ------------------------------------------------------------------

    def experiment_created(
        self, user_id: str, username: str,
        experiment_id: str, name: str,
    ) -> None:
        self._record(_entry(
            action="experiment.created",
            actor_id=user_id, actor_name=username,
            resource_type="experiment", resource_id=experiment_id,
            details={"name": name},
        ))

    def run_started(
        self, user_id: str, username: str,
        experiment_id: str, run_id: str,
    ) -> None:
        self._record(_entry(
            action="experiment.run_started",
            actor_id=user_id, actor_name=username,
            resource_type="run", resource_id=run_id,
            details={"experiment_id": experiment_id},
        ))

    # ------------------------------------------------------------------
    # User management events
    # ------------------------------------------------------------------

    def user_created(
        self, actor_id: str, actor_name: str,
        user_id: str, username: str, role: str,
    ) -> None:
        self._record(_entry(
            action="user.created",
            actor_id=actor_id, actor_name=actor_name,
            resource_type="user", resource_id=user_id,
            details={"username": username, "role": role},
        ))

    def user_deactivated(self, actor_id: str, actor_name: str, user_id: str) -> None:
        self._record(_entry(
            action="user.deactivated",
            actor_id=actor_id, actor_name=actor_name,
            resource_type="user", resource_id=user_id,
        ))

    # ------------------------------------------------------------------
    # Generic
    # ------------------------------------------------------------------

    def custom(
        self,
        actor_id: str, actor_name: str,
        action: str,
        resource_type: str = "",
        resource_id: str = "",
        outcome: str = "success",
        details: Optional[Dict] = None,
    ) -> None:
        self._record(_entry(
            action=action,
            actor_id=actor_id, actor_name=actor_name,
            resource_type=resource_type, resource_id=resource_id,
            outcome=outcome, details=details,
        ))

    # ------------------------------------------------------------------
    # Query
    # ------------------------------------------------------------------

    def get_recent(self, limit: int = 100) -> List[Dict[str, Any]]:
        entries = list(self._log)
        return entries[-limit:]

    def get_by_action(self, action: str, limit: int = 100) -> List[Dict[str, Any]]:
        return [e for e in self._log if e["action"] == action][-limit:]

    def get_by_user(self, user_id: str, limit: int = 100) -> List[Dict[str, Any]]:
        return [e for e in self._log if e["actor_id"] == user_id][-limit:]

    # ------------------------------------------------------------------
    # Internal
    # ------------------------------------------------------------------

    def _record(self, entry: Dict[str, Any]) -> None:
        self._log.append(entry)
        logger.debug("AUDIT [%s] %s by %s", entry["outcome"], entry["action"], entry["actor_name"])


# ---------------------------------------------------------------------------
# Singleton
# ---------------------------------------------------------------------------

_audit_logger_instance: Optional[AuditLogger] = None


def get_audit_logger() -> AuditLogger:
    """Return the process-wide singleton AuditLogger."""
    global _audit_logger_instance
    if _audit_logger_instance is None:
        _audit_logger_instance = AuditLogger()
    return _audit_logger_instance
