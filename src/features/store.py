"""
Feature Store — Aimran Data Science Lab
========================================
Enterprise-grade feature store with offline materialisation (Parquet),
simulated online serving (in-memory), lineage tracking, and statistics.
No external feature-store libraries required.
"""

from __future__ import annotations

import json
import logging
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)

_FEATURE_STORE_ROOT = Path("data/feature_store")
_METADATA_FILE = _FEATURE_STORE_ROOT / "metadata.json"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _utcnow() -> str:
    return datetime.now(timezone.utc).isoformat()


def _load_metadata() -> Dict[str, Any]:
    """Load the global feature-group registry from disk."""
    if not _METADATA_FILE.exists():
        return {}
    with _METADATA_FILE.open("r", encoding="utf-8") as fh:
        return json.load(fh)


def _save_metadata(metadata: Dict[str, Any]) -> None:
    _FEATURE_STORE_ROOT.mkdir(parents=True, exist_ok=True)
    with _METADATA_FILE.open("w", encoding="utf-8") as fh:
        json.dump(metadata, fh, indent=2, default=str)


# ---------------------------------------------------------------------------
# FeatureGroup dataclass
# ---------------------------------------------------------------------------


@dataclass
class FeatureGroup:
    """Metadata descriptor for a named collection of features.

    Attributes:
        name: Unique identifier for the feature group.
        version: Semantic or numeric version string (e.g. ``"v1"``).
        description: Human-readable description.
        features: Ordered list of feature column names.
        created_at: ISO-8601 UTC timestamp of registration.
        tags: Arbitrary key-value metadata.
    """

    name: str
    version: str
    description: str = ""
    features: List[str] = field(default_factory=list)
    created_at: str = field(default_factory=_utcnow)
    tags: Dict[str, str] = field(default_factory=dict)


# ---------------------------------------------------------------------------
# Feature Store
# ---------------------------------------------------------------------------


class FeatureStore:
    """Offline + simulated-online feature store.

    The store uses the local filesystem for offline materialisation
    (Parquet files under ``data/feature_store/``) and an in-process
    dict for online key-value serving.

    Parameters
    ----------
    store_root:
        Override the default ``data/feature_store/`` directory.

    Examples
    --------
    >>> fs = FeatureStore()
    >>> fg = fs.register_feature_group("user_features", ["age", "income"], "User demographics")
    >>> fs.materialize(df, "user_features", "v1")
    >>> retrieved = fs.get_features("user_features", "v1", ["age"])
    """

    def __init__(self, store_root: Optional[str] = None) -> None:
        global _FEATURE_STORE_ROOT, _METADATA_FILE
        if store_root:
            _FEATURE_STORE_ROOT = Path(store_root)
            _METADATA_FILE = _FEATURE_STORE_ROOT / "metadata.json"
        _FEATURE_STORE_ROOT.mkdir(parents=True, exist_ok=True)

        # Simulated online store: {group_name: {entity_id: {feature: value}}}
        self._online_store: Dict[str, Dict[str, Dict[str, Any]]] = {}
        logger.info("FeatureStore initialised | root=%s", _FEATURE_STORE_ROOT)

    # ------------------------------------------------------------------
    # Registration
    # ------------------------------------------------------------------

    def register_feature_group(
        self,
        name: str,
        features: List[str],
        description: str = "",
        tags: Optional[Dict[str, str]] = None,
        version: str = "v1",
    ) -> FeatureGroup:
        """Register a new feature group in the metadata registry.

        If a group with the same ``name`` already exists the registration
        is idempotent — the existing record is returned unchanged.

        Parameters
        ----------
        name:
            Unique group identifier (e.g. ``"user_demographics"``).
        features:
            List of column names belonging to this group.
        description:
            Optional human-readable description.
        tags:
            Arbitrary key-value labels (e.g. ``{"team": "data-eng"}``).
        version:
            Version string for this group (default ``"v1"``).

        Returns
        -------
        FeatureGroup
        """
        metadata = _load_metadata()
        if name in metadata:
            existing = FeatureGroup(**metadata[name])
            logger.info(
                "Feature group '%s' already registered (version=%s) — skipping.",
                name,
                existing.version,
            )
            return existing

        fg = FeatureGroup(
            name=name,
            version=version,
            description=description,
            features=features,
            tags=tags or {},
        )
        metadata[name] = asdict(fg)
        _save_metadata(metadata)
        logger.info(
            "Registered feature group '%s' with %d features (version=%s)",
            name,
            len(features),
            version,
        )
        return fg

    def list_feature_groups(self) -> List[FeatureGroup]:
        """Return all registered feature groups.

        Returns
        -------
        List[FeatureGroup]
        """
        metadata = _load_metadata()
        return [FeatureGroup(**v) for v in metadata.values()]

    # ------------------------------------------------------------------
    # Offline materialisation
    # ------------------------------------------------------------------

    def materialize(
        self,
        df: pd.DataFrame,
        feature_group_name: str,
        version: str,
    ) -> Path:
        """Persist a DataFrame as a versioned Parquet snapshot.

        Only columns declared in the feature group's ``features`` list
        are written; any extra columns in *df* are silently dropped.

        Parameters
        ----------
        df:
            Source DataFrame containing at least the registered features.
        feature_group_name:
            Name of the target feature group (must be registered first).
        version:
            Version string for this snapshot (e.g. ``"v2"``).

        Returns
        -------
        Path
            Path to the written Parquet file.

        Raises
        ------
        KeyError
            If the feature group is not registered.
        """
        metadata = _load_metadata()
        if feature_group_name not in metadata:
            raise KeyError(
                f"Feature group '{feature_group_name}' is not registered. "
                "Call register_feature_group() first."
            )
        fg = FeatureGroup(**metadata[feature_group_name])

        # Filter to registered features that exist in df
        available_cols = [c for c in fg.features if c in df.columns]
        missing = set(fg.features) - set(available_cols)
        if missing:
            logger.warning(
                "Materialising '%s': %d registered features not in DataFrame: %s",
                feature_group_name,
                len(missing),
                missing,
            )

        out_dir = _FEATURE_STORE_ROOT / feature_group_name
        out_dir.mkdir(parents=True, exist_ok=True)
        out_path = out_dir / f"{version}.parquet"

        df[available_cols].to_parquet(out_path, index=True)

        # Populate online store (last-write-wins per entity)
        self._populate_online_store(df, feature_group_name, available_cols)

        logger.info(
            "Materialised '%s' v%s → %s (%d rows, %d features)",
            feature_group_name,
            version,
            out_path,
            len(df),
            len(available_cols),
        )
        return out_path

    # ------------------------------------------------------------------
    # Offline retrieval
    # ------------------------------------------------------------------

    def get_features(
        self,
        feature_group_name: str,
        version: str,
        feature_names: Optional[List[str]] = None,
    ) -> pd.DataFrame:
        """Load features from the offline store.

        Parameters
        ----------
        feature_group_name:
            Name of the feature group to load.
        version:
            Version snapshot to load (e.g. ``"v1"``).
        feature_names:
            Subset of columns to return.  ``None`` returns all columns.

        Returns
        -------
        pd.DataFrame

        Raises
        ------
        FileNotFoundError
            If the requested version snapshot does not exist on disk.
        """
        parquet_path = _FEATURE_STORE_ROOT / feature_group_name / f"{version}.parquet"
        if not parquet_path.exists():
            raise FileNotFoundError(
                f"No materialised snapshot found at {parquet_path}. "
                "Call materialize() first."
            )
        df = pd.read_parquet(parquet_path)
        if feature_names:
            missing = [c for c in feature_names if c not in df.columns]
            if missing:
                logger.warning(
                    "Requested features not in stored snapshot: %s", missing
                )
            df = df[[c for c in feature_names if c in df.columns]]
        logger.info(
            "Retrieved '%s' v%s — %d rows × %d cols",
            feature_group_name,
            version,
            len(df),
            len(df.columns),
        )
        return df

    # ------------------------------------------------------------------
    # Online serving (simulated)
    # ------------------------------------------------------------------

    def get_online_features(
        self,
        entity_ids: List[Any],
        feature_group_name: str,
    ) -> Dict[Any, Dict[str, Any]]:
        """Retrieve features for specific entities from the simulated online store.

        The online store is populated automatically during :meth:`materialize`.
        For each entity ID in *entity_ids*, this method returns the
        feature values stored under that entity's DataFrame index value.

        Parameters
        ----------
        entity_ids:
            List of index values (row identifiers) to look up.
        feature_group_name:
            Feature group to query.

        Returns
        -------
        Dict[entity_id, Dict[feature_name, value]]
            Missing entity IDs map to an empty dict.
        """
        group_store = self._online_store.get(feature_group_name, {})
        result: Dict[Any, Dict[str, Any]] = {}
        for eid in entity_ids:
            result[eid] = group_store.get(eid, {})
            if not result[eid]:
                logger.debug(
                    "Online store miss: entity=%s group=%s", eid, feature_group_name
                )
        logger.info(
            "Online feature lookup | group='%s' | requested=%d | hits=%d",
            feature_group_name,
            len(entity_ids),
            sum(1 for v in result.values() if v),
        )
        return result

    # ------------------------------------------------------------------
    # Lineage
    # ------------------------------------------------------------------

    def get_feature_lineage(self, feature_name: str) -> Dict[str, Any]:
        """Return which registered feature groups contain *feature_name*.

        Parameters
        ----------
        feature_name:
            Column name to trace.

        Returns
        -------
        dict
            ``{"feature": str, "found_in": [{"group": str, "version": str}], "count": int}``
        """
        metadata = _load_metadata()
        found_in: List[Dict[str, str]] = []
        for group_name, group_data in metadata.items():
            if feature_name in group_data.get("features", []):
                found_in.append(
                    {
                        "group": group_name,
                        "version": group_data.get("version", "unknown"),
                        "description": group_data.get("description", ""),
                    }
                )
        lineage = {
            "feature": feature_name,
            "found_in": found_in,
            "count": len(found_in),
        }
        logger.info(
            "Lineage for '%s': found in %d group(s)", feature_name, len(found_in)
        )
        return lineage

    # ------------------------------------------------------------------
    # Statistics
    # ------------------------------------------------------------------

    def compute_feature_statistics(
        self,
        df: pd.DataFrame,
        feature_names: Optional[List[str]] = None,
    ) -> Dict[str, Dict[str, Any]]:
        """Compute descriptive statistics for the given features.

        Parameters
        ----------
        df:
            Source DataFrame.
        feature_names:
            Columns to analyse.  ``None`` uses all columns in *df*.

        Returns
        -------
        Dict[feature_name, stats_dict]
            Each ``stats_dict`` contains:
            ``mean``, ``std``, ``min``, ``max``, ``null_count``,
            ``null_pct``, ``cardinality``, ``dtype``.
            Non-numeric columns omit ``mean``/``std``/``min``/``max``.
        """
        cols = feature_names if feature_names else list(df.columns)
        stats: Dict[str, Dict[str, Any]] = {}

        for col in cols:
            if col not in df.columns:
                logger.warning("Column '%s' not found in DataFrame — skipping.", col)
                continue

            series = df[col]
            null_count = int(series.isna().sum())
            cardinality = int(series.nunique(dropna=True))
            col_stats: Dict[str, Any] = {
                "dtype": str(series.dtype),
                "null_count": null_count,
                "null_pct": round(null_count / max(len(series), 1) * 100, 2),
                "cardinality": cardinality,
            }

            if pd.api.types.is_numeric_dtype(series):
                col_stats.update(
                    {
                        "mean": float(series.mean()) if not series.isna().all() else None,
                        "std": float(series.std()) if not series.isna().all() else None,
                        "min": float(series.min()) if not series.isna().all() else None,
                        "max": float(series.max()) if not series.isna().all() else None,
                        "median": float(series.median())
                        if not series.isna().all()
                        else None,
                        "p25": float(series.quantile(0.25))
                        if not series.isna().all()
                        else None,
                        "p75": float(series.quantile(0.75))
                        if not series.isna().all()
                        else None,
                    }
                )
            else:
                # Categorical / object
                mode_val = series.mode()
                col_stats["mode"] = str(mode_val.iloc[0]) if not mode_val.empty else None

            stats[col] = col_stats

        logger.info("Computed statistics for %d feature(s).", len(stats))
        return stats

    # ------------------------------------------------------------------
    # Private helpers
    # ------------------------------------------------------------------

    def _populate_online_store(
        self,
        df: pd.DataFrame,
        group_name: str,
        feature_cols: List[str],
    ) -> None:
        """Populate the in-memory online store from a DataFrame."""
        if group_name not in self._online_store:
            self._online_store[group_name] = {}
        for idx, row in df[feature_cols].iterrows():
            self._online_store[group_name][idx] = row.to_dict()
