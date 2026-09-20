"""
Data Versioning System for Aimran Data Science Lab.

Provides DVC-style dataset versioning in pure Python — no subprocess calls.
Versions are stored as Parquet files; metadata is persisted in a JSON store.
"""

from __future__ import annotations

import hashlib
import json
import logging
import uuid
from dataclasses import dataclass, field, asdict
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional

import pandas as pd

logger = logging.getLogger(__name__)

_METADATA_FILENAME = "metadata.json"
_VERSIONED_ROOT = Path("data/versioned")


# ---------------------------------------------------------------------------
# Dataclass
# ---------------------------------------------------------------------------

@dataclass
class DataVersion:
    """Immutable record describing a single dataset snapshot."""

    version_id: str
    dataset_name: str
    file_path: str
    hash_md5: str
    size_bytes: int
    rows: int
    columns: int
    created_at: str                         # ISO-8601 string
    tags: List[str] = field(default_factory=list)
    description: str = ""
    parent_version_id: Optional[str] = None

    # ------------------------------------------------------------------
    # Serialisation helpers
    # ------------------------------------------------------------------

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)

    @classmethod
    def from_dict(cls, d: Dict[str, Any]) -> "DataVersion":
        return cls(**d)


# ---------------------------------------------------------------------------
# Store
# ---------------------------------------------------------------------------

class DataVersionStore:
    """
    Manages dataset versions on the local filesystem.

    Directory layout::

        data/versioned/
            metadata.json          ← all version records + "current" pointers
            {dataset_name}/
                {version_id}.parquet

    Parameters
    ----------
    root_dir:
        Override the default storage root (useful for testing).
    """

    def __init__(self, root_dir: Optional[Path] = None) -> None:
        self._root = Path(root_dir) if root_dir else _VERSIONED_ROOT
        self._root.mkdir(parents=True, exist_ok=True)
        self._metadata_path = self._root / _METADATA_FILENAME
        self._meta: Dict[str, Any] = self._load_metadata()

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def register_version(
        self,
        dataset_name: str,
        df: pd.DataFrame,
        description: str = "",
        tags: Optional[List[str]] = None,
        parent_version_id: Optional[str] = None,
    ) -> DataVersion:
        """
        Persist *df* as a new versioned snapshot of *dataset_name*.

        The DataFrame is saved as Parquet.  MD5 is computed on the raw
        Parquet bytes so the hash is deterministic given the same data.

        Returns
        -------
        DataVersion
            The newly created version record.
        """
        tags = tags or []
        version_id = uuid.uuid4().hex[:12]

        dataset_dir = self._root / dataset_name
        dataset_dir.mkdir(parents=True, exist_ok=True)

        parquet_path = dataset_dir / f"{version_id}.parquet"
        df.to_parquet(parquet_path, index=False)

        raw_bytes = parquet_path.read_bytes()
        hash_md5 = hashlib.md5(raw_bytes).hexdigest()  # noqa: S324 (non-crypto use)

        version = DataVersion(
            version_id=version_id,
            dataset_name=dataset_name,
            file_path=str(parquet_path),
            hash_md5=hash_md5,
            size_bytes=len(raw_bytes),
            rows=len(df),
            columns=len(df.columns),
            created_at=datetime.utcnow().isoformat(),
            tags=tags,
            description=description,
            parent_version_id=parent_version_id,
        )

        # Update in-memory metadata
        if dataset_name not in self._meta["datasets"]:
            self._meta["datasets"][dataset_name] = {"versions": [], "current": None}

        self._meta["datasets"][dataset_name]["versions"].append(version.to_dict())
        self._meta["datasets"][dataset_name]["current"] = version_id
        self._save_metadata()

        logger.info(
            "Registered version %s for dataset '%s' (%d rows, %d cols).",
            version_id, dataset_name, version.rows, version.columns,
        )
        return version

    def get_version(self, dataset_name: str, version_id: str) -> pd.DataFrame:
        """Load the DataFrame for a specific version."""
        version = self._get_version_record(dataset_name, version_id)
        path = Path(version.file_path)
        if not path.exists():
            raise FileNotFoundError(f"Parquet file not found: {path}")
        logger.debug("Loading version %s of '%s'.", version_id, dataset_name)
        return pd.read_parquet(path)

    def list_versions(self, dataset_name: str) -> List[DataVersion]:
        """Return all registered versions for *dataset_name*, oldest first."""
        self._assert_dataset_exists(dataset_name)
        records = self._meta["datasets"][dataset_name]["versions"]
        return [DataVersion.from_dict(r) for r in records]

    def diff(self, dataset_name: str, v1: str, v2: str) -> Dict[str, Any]:
        """
        Compare two versions of a dataset.

        Returns a dict with::

            {
                "rows":    {"v1": int, "v2": int, "delta": int},
                "columns": {"v1": int, "v2": int, "delta": int},
                "new_columns":     List[str],
                "removed_columns": List[str],
            }
        """
        r1 = self._get_version_record(dataset_name, v1)
        r2 = self._get_version_record(dataset_name, v2)

        df1 = pd.read_parquet(Path(r1.file_path))
        df2 = pd.read_parquet(Path(r2.file_path))

        cols1, cols2 = set(df1.columns), set(df2.columns)

        return {
            "rows": {
                "v1": r1.rows,
                "v2": r2.rows,
                "delta": r2.rows - r1.rows,
            },
            "columns": {
                "v1": r1.columns,
                "v2": r2.columns,
                "delta": r2.columns - r1.columns,
            },
            "new_columns": sorted(cols2 - cols1),
            "removed_columns": sorted(cols1 - cols2),
        }

    def rollback(self, dataset_name: str, version_id: str) -> None:
        """
        Set *version_id* as the current version for *dataset_name*.

        The version record must already exist; no data is copied.
        """
        # Validate existence
        self._get_version_record(dataset_name, version_id)
        self._meta["datasets"][dataset_name]["current"] = version_id
        self._save_metadata()
        logger.info("Rolled back '%s' to version %s.", dataset_name, version_id)

    def get_current(self, dataset_name: str) -> pd.DataFrame:
        """Load the DataFrame for the current version of *dataset_name*."""
        self._assert_dataset_exists(dataset_name)
        current_id = self._meta["datasets"][dataset_name].get("current")
        if current_id is None:
            raise ValueError(f"No current version set for dataset '{dataset_name}'.")
        return self.get_version(dataset_name, current_id)

    def get_lineage(self, dataset_name: str) -> List[DataVersion]:
        """
        Return versions ordered chronologically (oldest → newest).

        Each version links to its predecessor via *parent_version_id*.
        """
        versions = self.list_versions(dataset_name)
        versions.sort(key=lambda v: v.created_at)
        return versions

    # ------------------------------------------------------------------
    # Private helpers
    # ------------------------------------------------------------------

    def _load_metadata(self) -> Dict[str, Any]:
        if self._metadata_path.exists():
            try:
                with self._metadata_path.open("r", encoding="utf-8") as fh:
                    return json.load(fh)
            except json.JSONDecodeError:
                logger.warning("Corrupt metadata file; starting fresh.")
        return {"datasets": {}}

    def _save_metadata(self) -> None:
        with self._metadata_path.open("w", encoding="utf-8") as fh:
            json.dump(self._meta, fh, indent=2, default=str)

    def _assert_dataset_exists(self, dataset_name: str) -> None:
        if dataset_name not in self._meta["datasets"]:
            raise KeyError(f"Dataset '{dataset_name}' has no registered versions.")

    def _get_version_record(self, dataset_name: str, version_id: str) -> DataVersion:
        self._assert_dataset_exists(dataset_name)
        for record in self._meta["datasets"][dataset_name]["versions"]:
            if record["version_id"] == version_id:
                return DataVersion.from_dict(record)
        raise KeyError(
            f"Version '{version_id}' not found for dataset '{dataset_name}'."
        )
