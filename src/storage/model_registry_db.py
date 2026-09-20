"""
SQLAlchemy ORM model for the PostgreSQL-backed model registry.

Schema
------
model_registry
├── model_id       VARCHAR(255)  PK  — unique model identifier
├── model_type     VARCHAR(255)      — class name (e.g. RandomForestModel)
├── version        INTEGER           — auto-incremented per model_id
├── task           VARCHAR(64)       — classification / regression
├── metrics_json   TEXT              — JSON-encoded evaluation metrics
├── tags_json      TEXT              — JSON-encoded key-value metadata
├── model_blob     BYTEA             — pickled model object
├── is_champion    BOOLEAN           — currently serving version
├── created_at     FLOAT             — Unix timestamp of version creation

Graceful degradation: if SQLAlchemy or PostgreSQL is unavailable, all
operations return None / empty dicts and log a warning.
"""

from __future__ import annotations

import json
import logging
import os
import time
from typing import Any, Dict, Optional, Tuple

logger = logging.getLogger(__name__)

# ── Lazily initialised globals ──────────────────────────────────────────────

_SA_AVAILABLE = False
Base = None
ModelRegistryModel = None
_engine = None
_SessionLocal = None


def _init_sa():
    global _SA_AVAILABLE, Base, ModelRegistryModel, _engine, _SessionLocal
    if _SA_AVAILABLE:
        return

    try:
        from sqlalchemy import Column, String, Integer, Boolean, LargeBinary, Text, Float, create_engine, text
        from sqlalchemy.orm import declarative_base, sessionmaker
    except ImportError:
        logger.warning("SQLAlchemy not installed — model registry will be in-memory.")
        return

    _SA_AVAILABLE = True

    _Base = declarative_base()

    class _Model(_Base):
        __tablename__ = "model_registry"

        model_id = Column(String(255), primary_key=True)
        model_type = Column(String(255), nullable=False)
        version = Column(Integer, nullable=False, default=1)
        task = Column(String(64), nullable=False, default="classification")
        metrics_json = Column(Text, nullable=True)
        tags_json = Column(Text, nullable=True)
        model_blob = Column(LargeBinary, nullable=True)
        is_champion = Column(Boolean, nullable=False, default=False)
        created_at = Column(Float, nullable=False)

        def to_dict(self):
            return {
                "model_id": self.model_id,
                "model_type": self.model_type,
                "version": self.version,
                "task": self.task,
                "metrics": json.loads(self.metrics_json) if self.metrics_json else {},
                "tags": json.loads(self.tags_json) if self.tags_json else {},
                "is_champion": self.is_champion,
                "created_at": self.created_at,
            }

    Base = _Base
    ModelRegistryModel = _Model


# ── Engine & Session ────────────────────────────────────────────────────────


def get_db_url() -> str:
    return os.getenv(
        "DATABASE_URL",
        "postgresql://airflow:airflow@localhost:5432/airflow",
    )


def init_db(db_url: Optional[str] = None) -> None:
    _init_sa()
    if not _SA_AVAILABLE:
        return

    global _engine, _SessionLocal
    url = db_url or get_db_url()

    try:
        from sqlalchemy import create_engine
        from sqlalchemy.orm import sessionmaker

        _engine = create_engine(url, pool_pre_ping=True, pool_recycle=3600)
        Base.metadata.create_all(_engine)
        _SessionLocal = sessionmaker(bind=_engine)
        logger.info("Model-registry DB initialized at %s", url)
    except Exception as exc:
        logger.warning("DB unavailable (%s); model registry will be in-memory", exc)
        _engine = None
        _SessionLocal = None


def get_session():
    if not _SA_AVAILABLE or _SessionLocal is None:
        return None
    return _SessionLocal()


def is_db_available() -> bool:
    if not _SA_AVAILABLE or _engine is None:
        return False
    try:
        from sqlalchemy import text
        with _engine.connect() as conn:
            conn.execute(text("SELECT 1"))
        return True
    except Exception:
        return False


# ── CRUD helpers (used by registry.py) ──────────────────────────────────────


def db_register_model(
    model_id: str,
    model_type: str,
    model_blob: bytes,
    task: str = "classification",
    metrics: Optional[Dict[str, float]] = None,
    tags: Optional[Dict[str, str]] = None,
) -> Optional[Dict[str, Any]]:
    if not is_db_available():
        return None

    session = get_session()
    if session is None:
        return None

    try:
        existing = session.query(ModelRegistryModel).filter_by(model_id=model_id).order_by(
            ModelRegistryModel.version.desc()
        ).first()
        next_version = (existing.version + 1) if existing else 1
        is_first = next_version == 1

        record = ModelRegistryModel(
            model_id=model_id,
            model_type=model_type,
            version=next_version,
            task=task,
            metrics_json=json.dumps(metrics or {}),
            tags_json=json.dumps(tags or {}),
            model_blob=model_blob,
            is_champion=is_first,
            created_at=time.time(),
        )
        session.add(record)
        session.commit()

        result = record.to_dict()
        logger.info("DB: registered model '%s' version %d", model_id, next_version)
        return result
    except Exception as exc:
        session.rollback()
        logger.error("DB register error for '%s': %s", model_id, exc)
        return None
    finally:
        session.close()


def db_get_model(model_id: str) -> Optional[Tuple[Dict[str, Any], bytes]]:
    if not is_db_available():
        return None

    session = get_session()
    if session is None:
        return None

    try:
        record = session.query(ModelRegistryModel).filter_by(
            model_id=model_id, is_champion=True
        ).first()
        if record is None:
            record = session.query(ModelRegistryModel).filter_by(
                model_id=model_id
            ).order_by(ModelRegistryModel.version.desc()).first()
        if record is None:
            return None
        return record.to_dict(), record.model_blob
    except Exception as exc:
        logger.error("DB get error for '%s': %s", model_id, exc)
        return None
    finally:
        session.close()


def db_list_models() -> Dict[str, str]:
    if not is_db_available():
        return {}

    session = get_session()
    if session is None:
        return {}

    try:
        subq = session.query(
            ModelRegistryModel.model_id,
            ModelRegistryModel.model_type,
        ).filter(ModelRegistryModel.is_champion == True).subquery()

        rows = session.query(subq).all()
        return {row.model_id: row.model_type for row in rows}
    except Exception as exc:
        logger.error("DB list error: %s", exc)
        return {}
    finally:
        session.close()


def db_promote_champion(model_id: str, version: int) -> bool:
    if not is_db_available():
        return False

    session = get_session()
    if session is None:
        return False

    try:
        session.query(ModelRegistryModel).filter_by(model_id=model_id).update(
            {ModelRegistryModel.is_champion: False}
        )
        session.query(ModelRegistryModel).filter_by(
            model_id=model_id, version=version
        ).update({ModelRegistryModel.is_champion: True})
        session.commit()
        logger.info("DB: promoted model '%s' version %d", model_id, version)
        return True
    except Exception as exc:
        session.rollback()
        logger.error("DB promote error for '%s': %s", model_id, exc)
        return False
    finally:
        session.close()


def db_delete_model(model_id: str) -> bool:
    if not is_db_available():
        return False

    session = get_session()
    if session is None:
        return False

    try:
        session.query(ModelRegistryModel).filter_by(model_id=model_id).delete()
        session.commit()
        logger.info("DB: deleted model '%s'", model_id)
        return True
    except Exception as exc:
        session.rollback()
        logger.error("DB delete error for '%s': %s", model_id, exc)
        return False
    finally:
        session.close()


def db_get_metadata(model_id: str) -> Optional[Dict[str, Any]]:
    result = db_get_model(model_id)
    if result is None:
        return None
    meta, _blob = result
    meta.pop("model_blob", None)
    return meta
