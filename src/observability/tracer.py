"""
src/observability/tracer.py — OpenTelemetry SDK initialization.

Sets up a TracerProvider with an OTLP exporter (Jaeger / Grafana Tempo by
default, configurable via ``OTEL_EXPORTER_OTLP_ENDPOINT``).

When OpenTelemetry is not installed, all functions become no-ops so the
platform keeps running without tracing.
"""

from __future__ import annotations

import logging
import os
from typing import Any, Optional

logger = logging.getLogger(__name__)

_TRACER_AVAILABLE = False
_tracer_provider = None
_tracer = None


def _init_otlp():
    global _TRACER_AVAILABLE
    try:
        from opentelemetry import trace
        from opentelemetry.sdk.trace import TracerProvider
        from opentelemetry.sdk.trace.export import BatchSpanProcessor
        from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
    except ImportError:
        logger.warning(
            "OpenTelemetry not installed — tracing disabled. "
            "Install with: pip install opentelemetry-sdk opentelemetry-exporter-otlp"
        )
        return

    _TRACER_AVAILABLE = True
    global _tracer_provider, _tracer

    service_name = os.getenv("OTEL_SERVICE_NAME", "aimran-api")
    _tracer_provider = TracerProvider()
    trace.set_tracer_provider(_tracer_provider)

    endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")
    if os.getenv("OTEL_TRACES_EXPORTER", "otlp").lower() != "none":
        try:
            exporter = OTLPSpanExporter(endpoint=endpoint)
            _tracer_provider.add_span_processor(BatchSpanProcessor(exporter))
            logger.info("OTLP trace exporter configured at %s", endpoint)
        except Exception as exc:  # noqa: BLE001
            logger.warning("OTLP exporter init failed (%s); traces stay in-memory", exc)

    _tracer = trace.get_tracer(service_name)
    logger.info("OpenTelemetry tracer initialized (service=%s)", service_name)


def get_tracer():
    """Return the global tracer or a null tracer when unavailable."""
    if _TRACER_AVAILABLE:
        return _tracer
    try:
        from opentelemetry.trace import get_tracer as _get_tracer
        return _get_tracer("aimran-null-tracer")
    except ImportError:
        return None


def is_tracing_enabled() -> bool:
    return _TRACER_AVAILABLE


def start_span(name: str, attributes: Optional[dict] = None):
    """Start a span, or return a no-op context manager when tracing is off."""
    tracer = get_tracer()
    if tracer is None:
        from contextlib import nullcontext
        return nullcontext()
    return tracer.start_as_current_span(name, attributes=attributes or {})


_init_otlp()