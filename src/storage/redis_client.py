import logging
import os
import pickle
from typing import Any, Optional

logger = logging.getLogger(__name__)


class RedisClient:
    """Shared Redis connection pool with graceful in-memory fallback."""

    def __init__(self, url: Optional[str] = None):
        self._url = url or os.getenv("REDIS_URL", "redis://localhost:6379/0")
        self._redis = None
        self._connected = False
        self._fallback: dict[str, Any] = {}
        self._init()

    def _init(self):
        try:
            import redis as _redis

            self._redis = _redis.from_url(self._url, decode_responses=False, socket_timeout=2)
            self._redis.ping()
            self._connected = True
            logger.info("Connected to Redis at %s", self._url)
        except Exception as exc:
            logger.warning("Redis unavailable (%s); using in-memory fallback", exc)
            self._redis = None
            self._connected = False

    # ── Generic helpers ──────────────────────────────────────────────────

    def _r(self):
        if self._redis is None:
            return None
        try:
            self._redis.ping()
            return self._redis
        except Exception:
            self._connected = False
            return None

    # ── String / JSON ────────────────────────────────────────────────────

    def set_json(self, key: str, value: dict, ttl: int = 86400) -> None:
        import json

        client = self._r()
        if client:
            try:
                client.setex(key, ttl, json.dumps(value))
                return
            except Exception as exc:
                logger.error("Redis set_json error: %s", exc)
        self._fallback[key] = value

    def get_json(self, key: str) -> Optional[dict]:
        import json

        client = self._r()
        if client:
            try:
                raw = client.get(key)
                return json.loads(raw) if raw else None
            except Exception as exc:
                logger.error("Redis get_json error: %s", exc)
        return self._fallback.get(key)

    def delete(self, key: str) -> None:
        client = self._r()
        if client:
            try:
                client.delete(key)
                return
            except Exception as exc:
                logger.error("Redis delete error: %s", exc)
        self._fallback.pop(key, None)

    def keys(self, pattern: str) -> list[str]:
        client = self._r()
        if client:
            try:
                return [k.decode() if isinstance(k, bytes) else k for k in client.keys(pattern)]
            except Exception as exc:
                logger.error("Redis keys error: %s", exc)
        return [k for k in self._fallback if k.startswith(pattern.replace("*", ""))]

    # ── Pickle / Binary ──────────────────────────────────────────────────

    def set_pickle(self, key: str, obj: Any, ttl: int = 86400) -> None:
        client = self._r()
        if client:
            try:
                client.setex(key, ttl, pickle.dumps(obj, protocol=pickle.HIGHEST_PROTOCOL))
                return
            except Exception as exc:
                logger.error("Redis set_pickle error: %s", exc)
        self._fallback[key] = obj

    def get_pickle(self, key: str) -> Optional[Any]:
        client = self._r()
        if client:
            try:
                raw = client.get(key)
                return pickle.loads(raw) if raw else None
            except Exception as exc:
                logger.error("Redis get_pickle error: %s", exc)
        return self._fallback.get(key)

    @property
    def connected(self) -> bool:
        return self._connected

    @property
    def client(self):
        return self._redis
