"""
Observability package — OpenTelemetry middleware and Prometheus helpers.

Both are optional: the middleware is a no-op when the SDK is disabled or
not installed, so the API boots cleanly in test / minimal environments.
"""

from __future__ import annotations

import logging
import os
import time

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# OpenTelemetry — optional
# ---------------------------------------------------------------------------

_OTEL_DISABLED = os.getenv("OTEL_SDK_DISABLED", "false").lower() in ("true", "1", "yes")

_otel_available = False
if not _OTEL_DISABLED:
    try:
        from opentelemetry import trace as _trace
        from opentelemetry.sdk.trace import TracerProvider as _TracerProvider
        from opentelemetry.sdk.trace.export import BatchSpanProcessor as _BatchSpanProcessor

        _tracer_provider = _TracerProvider()
        _trace.set_tracer_provider(_tracer_provider)

        # OTLP exporter — only attach when endpoint is configured
        _otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT", "")
        if _otlp_endpoint:
            from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import (
                OTLPSpanExporter as _OTLPSpanExporter,
            )
            _tracer_provider.add_span_processor(
                _BatchSpanProcessor(_OTLPSpanExporter(endpoint=_otlp_endpoint, insecure=True))
            )
            logger.info("OTel: OTLP exporter configured → %s", _otlp_endpoint)

        _tracer = _trace.get_tracer("aimran.api")
        _otel_available = True
        logger.info("OpenTelemetry SDK initialised")
    except Exception as exc:
        logger.info("OpenTelemetry not available (%s) — tracing disabled", exc)


# ---------------------------------------------------------------------------
# Middleware
# ---------------------------------------------------------------------------

class OpenTelemetryMiddleware(BaseHTTPMiddleware):
    """
    Lightweight OTel tracing middleware for FastAPI.

    - Creates one span per HTTP request when OTel is available.
    - Falls through silently (no overhead) when OTel is disabled / absent.
    """

    async def dispatch(self, request: Request, call_next):
        if not _otel_available:
            return await call_next(request)

        span_name = f"{request.method} {request.url.path}"
        with _tracer.start_as_current_span(span_name) as span:  # type: ignore[name-defined]
            try:
                span.set_attribute("http.method", request.method)
                span.set_attribute("http.url", str(request.url))
                response = await call_next(request)
                span.set_attribute("http.status_code", response.status_code)
                return response
            except Exception as exc:
                span.record_exception(exc)
                raise


__all__ = ["OpenTelemetryMiddleware"]
