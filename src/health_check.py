"""
src/health_check.py — Comprehensive system health diagnostics.

HealthManager performs local checks for:
- Redis connectivity
- PostgreSQL / database connectivity
- FastAPI server responsiveness
- Disk space usage
- Model registry contents

Usage
-----
    from src.health_check import health_manager
    report = health_manager.full_report()
"""

from __future__ import annotations

import asyncio
import logging
import os
import time
from typing import Any, Dict, Optional

import psutil

from src.storage.model_registry_db import is_db_available as _db_available
from src.storage.redis_client import RedisClient
from src.models.registry import list_models

logger = logging.getLogger(__name__)


class HealthManager:
    """Local health diagnostics for all platform dependencies."""

    def __init__(self) -> None:
        self._redis = RedisClient()

    # ── Individual checks ─────────────────────────────────────────────────

    def check_redis(self) -> Dict[str, Any]:
        """Ping Redis and return latency + connection status."""
        start = time.perf_counter()
        ok = self._redis.connected
        elapsed_ms = (time.perf_counter() - start) * 1000
        return {
            "status": "ok" if ok else "unavailable",
            "latency_ms": round(elapsed_ms, 2),
            "detail": "Redis connection established" if ok else "Redis not reachable (in-memory fallback active)",
        }

    def check_database(self) -> Dict[str, Any]:
        """Check PostgreSQL connectivity via SQLAlchemy."""
        start = time.perf_counter()
        ok = _db_available()
        elapsed_ms = (time.perf_counter() - start) * 1000
        return {
            "status": "ok" if ok else "unavailable",
            "latency_ms": round(elapsed_ms, 2),
            "detail": "PostgreSQL connected" if ok else "Database unavailable (in-memory fallback active)",
        }

    def check_api(self) -> Dict[str, Any]:
        """Verify the FastAPI server responds on /health."""
        start = time.perf_counter()
        try:
            import requests as _req
            base = os.getenv("API_BASE", "http://localhost:8000")
            r = _req.get(f"{base}/health", timeout=5)
            elapsed_ms = (time.perf_counter() - start) * 1000
            if r.status_code == 200:
                data = r.json()
                return {
                    "status": "ok",
                    "latency_ms": round(elapsed_ms, 2),
                    "detail": f"API healthy — {data.get('status', 'unknown')}",
                    "version": data.get("version", "unknown"),
                    "environment": data.get("environment", "unknown"),
                }
            return {
                "status": "degraded",
                "latency_ms": round(elapsed_ms, 2),
                "detail": f"API returned HTTP {r.status_code}",
            }
        except Exception as exc:
            elapsed_ms = (time.perf_counter() - start) * 1000
            return {
                "status": "unreachable",
                "latency_ms": round(elapsed_ms, 2),
                "detail": f"API unreachable: {exc}",
            }

    def check_disk(self) -> Dict[str, Any]:
        """Check disk usage on the primary partition."""
        try:
            usage = psutil.disk_usage("/")
            pct = usage.percent
            free_gb = usage.free / (1024 ** 3)
            total_gb = usage.total / (1024 ** 3)
            return {
                "status": "ok" if pct < 90 else "warning",
                "percent": round(pct, 1),
                "free_gb": round(free_gb, 2),
                "total_gb": round(total_gb, 2),
                "detail": f"{pct}% used ({free_gb:.1f} GB free of {total_gb:.1f} GB)",
            }
        except Exception as exc:
            return {"status": "error", "detail": str(exc)}

    def check_model_registry(self) -> Dict[str, Any]:
        """Check model registry contents."""
        try:
            models = list_models()
            count = len(models)
            return {
                "status": "ok",
                "count": count,
                "models": list(models.keys()),
                "detail": f"{count} model(s) registered" if count else "No models registered",
            }
        except Exception as exc:
            return {"status": "error", "detail": str(exc)}

    # ── Aggregated report ────────────────────────────────────────────────

    def full_report(self) -> Dict[str, Any]:
        """Run all checks and return a combined report dict."""
        checks = {
            "redis": self.check_redis(),
            "database": self.check_database(),
            "api": self.check_api(),
            "disk": self.check_disk(),
            "model_registry": self.check_model_registry(),
        }
        all_ok = all(c["status"] == "ok" for c in checks.values())
        return {
            "overall": "healthy" if all_ok else "degraded",
            "timestamp": time.time(),
            "checks": checks,
        }

    # ── Async wrapper for Celery task compatibility ──────────────────────

    async def run_all_checks(self) -> Dict[str, Any]:
        """Async variant of full_report() for Celery periodic tasks."""
        loop = asyncio.get_event_loop()
        return await loop.run_in_executor(None, self.full_report)


# Module-level singleton for convenient import
health_manager = HealthManager()
