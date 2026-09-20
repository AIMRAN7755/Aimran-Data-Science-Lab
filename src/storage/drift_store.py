import logging
from typing import Any, Optional

from src.storage.redis_client import RedisClient

logger = logging.getLogger(__name__)

_DRIFT_TTL = 86400 * 7  # 7 days (drift detectors are expensive to retrain)


class DriftStore:
    """Redis-backed drift detector storage using pickle serialization.

    Keys are stored as ``drift:detector:{detector_id}``.  The serialized
    payload is the full ``DataDriftDetector`` instance.
    """

    KEY_PREFIX = "drift:detector:"

    def __init__(self, redis: RedisClient):
        self._redis = redis

    # ── Write ────────────────────────────────────────────────────────────

    def save(self, detector_id: str, detector: Any, ttl: int = _DRIFT_TTL) -> None:
        key = f"{self.KEY_PREFIX}{detector_id}"
        self._redis.set_pickle(key, detector, ttl=ttl)
        logger.info("Saved drift detector '%s' (%s)", detector_id, type(detector).__name__)

    # ── Read ─────────────────────────────────────────────────────────────

    def load(self, detector_id: str) -> Optional[Any]:
        obj = self._redis.get_pickle(f"{self.KEY_PREFIX}{detector_id}")
        if obj is None:
            logger.warning("Drift detector '%s' not found", detector_id)
        return obj

    def list_ids(self) -> list[str]:
        raw = self._redis.keys(f"{self.KEY_PREFIX}*")
        prefix_len = len(self.KEY_PREFIX)
        return [k[prefix_len:] for k in raw]

    # ── Delete ───────────────────────────────────────────────────────────

    def delete(self, detector_id: str) -> None:
        self._redis.delete(f"{self.KEY_PREFIX}{detector_id}")
        logger.info("Deleted drift detector '%s'", detector_id)
