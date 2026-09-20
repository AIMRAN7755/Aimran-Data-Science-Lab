"""
src/observability/instrumentors.py — Auto-instrumentation for SQLAlchemy,
Redis, and Celery.

Each instrumentor is optional and enabled via env var:

    OTEL_INSTRUMENT_SQLALCHEMY=1
    OTEL_INSTRUMENT_REDIS=1
    OTEL_INSTRUMENT_CELERY=1

All instrumentation requires the matching ``opentelemetry-instrumentation-*``
package; otherwise a warning is logged and nothing happens.
"""

from __future__ import annotations

import logging
import os

logger = logging.getLogger(__name__)


def instrument_sqlalchemy() -> bool:
    """Instrument SQLAlchemy engine calls with OpenTelemetry spans."""
    if os.getenv("OTEL_INSTRUMENT_SQLALCHEMY", "0") != "1":
        return False
    try:
        from opentelemetry.instrumentation.sqlalchemy import SQLAlchemyInstrumentor
        SQLAlchemyInstrumentor().instrument()
        logger.info("SQLAlchemy instrumentation enabled")
        return True
    except ImportError:
        logger.warning(
            "SQLAlchemy instrumentation unavailable — "
            "pip install opentelemetry-instrumentation-sqlalchemy"
        )
        return False
    except Exception as exc:  # noqa: BLE001
        logger.warning("SQLAlchemy instrumentation failed: %s", exc)
        return False


def instrument_redis() -> bool:
    """Instrument Redis client commands with OpenTelemetry spans."""
    if os.getenv("OTEL_INSTRUMENT_REDIS", "0") != "1":
        return False
    try:
        from opentelemetry.instrumentation.redis import RedisInstrumentor
        RedisInstrumentor().instrument()
        logger.info("Redis instrumentation enabled")
        return True
    except ImportError:
        logger.warning(
            "Redis instrumentation unavailable — "
            "pip install opentelemetry-instrumentation-redis"
        )
        return False
    except Exception as exc:  # noqa: BLE001
        logger.warning("Redis instrumentation failed: %s", exc)
        return False


def instrument_celery() -> bool:
    """Instrument Celery task execution with OpenTelemetry spans."""
    if os.getenv("OTEL_INSTRUMENT_CELERY", "0") != "1":
        return False
    try:
        from opentelemetry.instrumentation.celery import CeleryInstrumentor
        CeleryInstrumentor().instrument()
        logger.info("Celery instrumentation enabled")
        return True
    except ImportError:
        logger.warning(
            "Celery instrumentation unavailable — "
            "pip install opentelemetry-instrumentation-celery"
        )
        return False
    except Exception as exc:  # noqa: BLE001
        logger.warning("Celery instrumentation failed: %s", exc)
        return False


def instrument_all() -> None:
    """Enable every instrumentation module whose env flag is set."""
    instrument_sqlalchemy()
    instrument_redis()
    instrument_celery()