"""
src.data — Data layer for Aimran Data Science Lab.

Public exports
--------------
DataLoader, Preprocessor        — loading and preprocessing (loaders.py)
DataVersionStore, DataVersion   — dataset versioning  (versioning.py)
DataQualityEngine, DataProfiler — quality checks and profiling (quality.py)
QualityRule, QualityReport      — quality rule/report dataclasses (quality.py)
DataCatalog, DataAsset          — metadata catalog (catalog.py)

Note: Imports are lazy (PEP 562) to avoid blocking on optional heavy
dependencies (e.g. psutil, MLflow) at package import time.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from src.data.loaders import DataLoader, Preprocessor
    from src.data.versioning import DataVersionStore, DataVersion
    from src.data.quality import (
        DataQualityEngine,
        DataProfiler,
        QualityRule,
        QualityReport,
    )
    from src.data.catalog import DataCatalog, DataAsset


_LOADERS = ("DataLoader", "Preprocessor")
_VERSIONING = ("DataVersionStore", "DataVersion")
_QUALITY = ("DataQualityEngine", "DataProfiler", "QualityRule", "QualityReport")
_CATALOG = ("DataCatalog", "DataAsset")

__all__ = [*_LOADERS, *_VERSIONING, *_QUALITY, *_CATALOG]


def __getattr__(name: str):
    """PEP 562 lazy attribute resolution for the package."""
    if name in _LOADERS:
        from src.data import loaders
        return getattr(loaders, name)

    if name in _VERSIONING:
        from src.data import versioning
        return getattr(versioning, name)

    if name in _QUALITY:
        from src.data import quality
        return getattr(quality, name)

    if name in _CATALOG:
        from src.data import catalog
        return getattr(catalog, name)

    raise AttributeError(f"module 'src.data' has no attribute {name!r}")
