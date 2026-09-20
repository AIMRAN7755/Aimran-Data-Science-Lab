"""
src/observability/middleware.py — FastAPI ASGI tracing middleware.

Every HTTP request gets a trace span with route, method, status code,
duration, and (when available) the authenticated user ID.

The span is attached to the ASGI scope so downstream handlers can access it
via ``scope["span"]``.

Graceful degradation: without OpenTelemetry, the middleware is a transparent
pass-through with zero overhead.
"""

from __future__ import annotations

import logging
import time
from typing import Any, Dict, Optional

from src.observability.tracer import get_tracer, is_tracing_enabled

logger = logging.getLogger(__name__)


def trace_request(scope: Dict[str, Any]) -> None:
    """
    Create (and attach) a request span to the ASGI scope.

    Must be called inside the ASGI app (per-request).  The span is stored on
    ``scope["span"]`` and finished by the middleware after the response.
    """
    tracer = get_tracer()
    if tracer is None:
        return

    span = tracer.start_span(
        "HTTP " + scope.get("method", "UNKNOWN"),
        attributes={
            "http.method": scope.get("method", "UNKNOWN"),
            "http.url": _scope_url(scope),
            "http.route": _scope_route(scope),
            "component": "fastapi",
        },
    )
    scope["span"] = span


def _scope_url(scope: Dict[str, Any]) -> str:
    try:
        scheme = scope.get("scheme", "http")
        host, port = scope.get("server") or ("localhost", 80)
        path = scope.get("path", "/")
        query = scope.get("query_string", b"")
        qs = query.decode() if query else ""
        return f"{scheme}://{host}:{port}{path}" + (f"?{qs}" if qs else "")
    except Exception:
        return scope.get("path", "/")


def _scope_route(scope: Dict[str, Any]) -> str:
    try:
        return scope.get("route", {}).get("path", "") or scope.get("path", "/")
    except Exception:
        return scope.get("path", "/")


class OpenTelemetryMiddleware:
    """
    ASGI middleware that traces every request.

    Usage
    -----
        from src.observability import OpenTelemetryMiddleware
        app.add_middleware(OpenTelemetryMiddleware)
    """

    def __init__(self, app):
        self.app = app

    async def __call__(self, scope, receive, send):
        if scope["type"] != "http":
            await self.app(scope, receive, send)
            return

        start = time.perf_counter()
        span = None
        if is_tracing_enabled():
            trace_request(scope)
            span = scope.get("span")

        status_holder: Dict[str, Any] = {}

        async def send_wrapper(message):
            if message["type"] == "http.response.start":
                status_holder["status"] = message.get("status", 0)
            await send(message)

        try:
            await self.app(scope, receive, send_wrapper)
        finally:
            elapsed_ms = (time.perf_counter() - start) * 1000
            if span is not None:
                span.set_attribute("http.status_code", status_holder.get("status", 0))
                span.set_attribute("http.duration_ms", round(elapsed_ms, 3))
                user = scope.get("state", {}).get("user")
                if user is not None:
                    span.set_attribute("user.id", getattr(user, "user_id", ""))
                span.end()