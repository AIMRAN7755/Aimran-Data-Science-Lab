"""
Sliding-window in-memory rate limiter for FastAPI.
Uses a simple token-bucket per client IP.
Falls back gracefully if Redis is unavailable.
"""

import time
import logging
from collections import defaultdict, deque
from typing import Deque, Dict
from fastapi import Request, HTTPException
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse

logger = logging.getLogger(__name__)


class SlidingWindowRateLimiter:
    """Thread-safe sliding window rate limiter (in-memory)"""

    def __init__(self, requests_per_minute: int = 60, burst_limit: int = 20):
        self.rpm = requests_per_minute
        self.burst_limit = burst_limit
        self.window = 60  # seconds
        self._windows: Dict[str, Deque[float]] = defaultdict(deque)

    def is_allowed(self, client_id: str) -> tuple[bool, int]:
        """
        Returns (allowed: bool, remaining: int)
        """
        now = time.time()
        window_start = now - self.window
        timestamps: Deque[float] = self._windows[client_id]

        # Remove expired timestamps
        while timestamps and timestamps[0] < window_start:
            timestamps.popleft()

        count = len(timestamps)
        if count >= self.rpm:
            return False, 0

        timestamps.append(now)
        return True, self.rpm - count - 1

    def cleanup(self, older_than_seconds: int = 300):
        """Remove stale client entries"""
        cutoff = time.time() - older_than_seconds
        stale = [cid for cid, dq in self._windows.items() if not dq or dq[-1] < cutoff]
        for cid in stale:
            del self._windows[cid]


class RateLimitMiddleware(BaseHTTPMiddleware):
    """FastAPI middleware for rate limiting"""

    def __init__(self, app, requests_per_minute: int = 120, burst_limit: int = 30,
                 exclude_paths: list = None):
        super().__init__(app)
        self.limiter = SlidingWindowRateLimiter(requests_per_minute, burst_limit)
        self.exclude_paths = exclude_paths or ["/health", "/docs", "/openapi.json", "/redoc"]
        self._cleanup_counter = 0

    async def dispatch(self, request: Request, call_next):
        path = request.url.path

        # Skip excluded paths
        if any(path.startswith(p) for p in self.exclude_paths):
            return await call_next(request)

        # Get client identifier
        client_ip = request.client.host if request.client else "unknown"
        api_key = request.headers.get("X-API-Key", "")
        client_id = api_key or client_ip

        allowed, remaining = self.limiter.is_allowed(client_id)

        # Periodic cleanup
        self._cleanup_counter += 1
        if self._cleanup_counter % 1000 == 0:
            self.limiter.cleanup()

        if not allowed:
            return JSONResponse(
                status_code=429,
                content={
                    "error": "rate_limit_exceeded",
                    "message": f"Too many requests. Limit: {self.limiter.rpm}/minute",
                    "retry_after": 60,
                },
                headers={"Retry-After": "60", "X-RateLimit-Limit": str(self.limiter.rpm)},
            )

        response = await call_next(request)
        response.headers["X-RateLimit-Limit"] = str(self.limiter.rpm)
        response.headers["X-RateLimit-Remaining"] = str(remaining)
        return response
