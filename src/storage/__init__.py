"""
Storage package — Redis client, DB initialisation, and job/drift stores.

All classes degrade gracefully when Redis is unavailable (fall back to
in-process dicts) so the API still boots in dev/test without Docker.
"""

from __future__ import annotations

import json
import logging
import time
from typing import Any, Dict, Optional

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Redis client (lazy, graceful)
# ---------------------------------------------------------------------------

class RedisClient:
    """
    Thin wrapper around ``redis.Redis`` that:
    - Connects lazily on first use
    - Falls back to an in-memory dict when Redis is unreachable
    - Exposes get / set / delete with optional TTL
    """

    def __init__(
        self,
        url: Optional[str] = None,
        fallback_to_memory: bool = True,
    ):
        import os
        self._url = url or os.getenv("REDIS_URL", "redis://localhost:6379/0")
        self._fallback = fallback_to_memory
        self._client: Optional[Any] = None
        self._memory: Dict[str, Any] = {}
        self._memory_ttl: Dict[str, float] = {}
        self._redis_available: Optional[bool] = None   # None = not yet tried

    def _connect(self) -> bool:
        if self._redis_available is not None:
            return self._redis_available
        try:
            import redis
            c = redis.from_url(self._url, socket_connect_timeout=1, socket_timeout=1)
            c.ping()
            self._client = c
            self._redis_available = True
            logger.info("RedisClient: connected to %s", self._url)
        except Exception as exc:
            self._redis_available = False
            logger.warning("RedisClient: Redis unavailable (%s) — using in-memory fallback", exc)
        return self._redis_available

    def get(self, key: str) -> Optional[Any]:
        if self._connect() and self._client:
            raw = self._client.get(key)
            if raw is None:
                return None
            try:
                return json.loads(raw)
            except Exception:
                return raw
        # Memory fallback
        ttl = self._memory_ttl.get(key)
        if ttl and time.time() > ttl:
            self._memory.pop(key, None)
            self._memory_ttl.pop(key, None)
            return None
        return self._memory.get(key)

    def set(self, key: str, value: Any, ttl_seconds: int = 86400) -> None:
        raw = json.dumps(value)
        if self._connect() and self._client:
            self._client.setex(key, ttl_seconds, raw)
            return
        self._memory[key] = value
        self._memory_ttl[key] = time.time() + ttl_seconds

    def delete(self, key: str) -> None:
        if self._connect() and self._client:
            self._client.delete(key)
            return
        self._memory.pop(key, None)
        self._memory_ttl.pop(key, None)


# ---------------------------------------------------------------------------
# AutoML job store
# ---------------------------------------------------------------------------

class AutoJobStore:
    """Key-value store for AutoML job results (24h TTL)."""

    _TTL = 60 * 60 * 24   # 24 hours

    def __init__(self, redis: Optional[RedisClient] = None):
        self._redis = redis or RedisClient()
        self._prefix = "aimran:automl:job:"

    def set(self, job_id: str, data: Dict[str, Any]) -> None:
        self._redis.set(f"{self._prefix}{job_id}", data, ttl_seconds=self._TTL)

    def get(self, job_id: str) -> Optional[Dict[str, Any]]:
        return self._redis.get(f"{self._prefix}{job_id}")


# ---------------------------------------------------------------------------
# Drift store
# ---------------------------------------------------------------------------

class DriftStore:
    """Key-value store for drift reports (7d TTL)."""

    _TTL = 60 * 60 * 24 * 7

    def __init__(self, redis: Optional[RedisClient] = None):
        self._redis = redis or RedisClient()
        self._prefix = "aimran:drift:report:"

    def set(self, model_id: str, report: Dict[str, Any]) -> None:
        self._redis.set(f"{self._prefix}{model_id}", report, ttl_seconds=self._TTL)

    def get(self, model_id: str) -> Optional[Dict[str, Any]]:
        return self._redis.get(f"{self._prefix}{model_id}")


# ---------------------------------------------------------------------------
# DB initialisation
# ---------------------------------------------------------------------------

def init_db() -> None:
    """
    Initialise the relational database via Alembic migrations.

    Degrades gracefully when the DB URL is not configured (dev / test).
    """
    import os
    db_url = os.getenv("DATABASE_URL", "")
    if not db_url:
        logger.info("DATABASE_URL not set — skipping DB initialisation (using in-memory stores)")
        return

    try:
        from alembic.config import Config as AlembicConfig
        from alembic import command as alembic_cmd

        cfg = AlembicConfig("alembic.ini")
        alembic_cmd.upgrade(cfg, "head")
        logger.info("Database migrations applied successfully")
    except Exception as exc:
        logger.warning("DB init failed (non-fatal in dev): %s", exc)


__all__ = ["RedisClient", "AutoJobStore", "DriftStore", "init_db"]
