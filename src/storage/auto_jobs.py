import logging
from typing import Any, Optional

from src.storage.redis_client import RedisClient

logger = logging.getLogger(__name__)

_AUTO_JOB_TTL = 86400  # 24 hours


class AutoJobStore:
    """Redis-backed AutoML job metadata storage with 24-hour TTL.

    Keys are stored as ``automl:job:{job_id}`` with JSON-serialized
    job metadata (status, leaderboard, best-model reference, …).
    """

    KEY_PREFIX = "automl:job:"

    def __init__(self, redis: RedisClient):
        self._redis = redis

    # ── Write ────────────────────────────────────────────────────────────

    def set(self, job_id: str, data: dict, ttl: int = _AUTO_JOB_TTL) -> None:
        key = f"{self.KEY_PREFIX}{job_id}"
        self._redis.set_json(key, data, ttl=ttl)

    def update(self, job_id: str, **fields: Any) -> None:
        existing = self.get(job_id) or {}
        existing.update(fields)
        self.set(job_id, existing)

    # ── Read ─────────────────────────────────────────────────────────────

    def get(self, job_id: str) -> Optional[dict]:
        return self._redis.get_json(f"{self.KEY_PREFIX}{job_id}")

    def list_ids(self) -> list[str]:
        raw = self._redis.keys(f"{self.KEY_PREFIX}*")
        prefix_len = len(self.KEY_PREFIX)
        return [k[prefix_len:] for k in raw]

    # ── Delete ───────────────────────────────────────────────────────────

    def delete(self, job_id: str) -> None:
        self._redis.delete(f"{self.KEY_PREFIX}{job_id}")
