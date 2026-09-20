"""
Data Catalog for Aimran Data Science Lab.

Provides metadata registration, fuzzy search, lineage tracking, and
Markdown report generation for any data asset (datasets, models, features,
reports).  All metadata is persisted to a single JSON file so the catalog
survives process restarts without a database.
"""

from __future__ import annotations

import json
import logging
import uuid
from dataclasses import dataclass, field, asdict
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Literal, Optional

logger = logging.getLogger(__name__)

_CATALOG_ROOT = Path("data/catalog")
_CATALOG_FILENAME = "catalog.json"

AssetType = Literal["dataset", "model", "feature", "report"]


# ---------------------------------------------------------------------------
# Dataclass
# ---------------------------------------------------------------------------

@dataclass
class DataAsset:
    """
    Represents any catalogued data asset.

    Attributes
    ----------
    asset_id:   Unique identifier (hex UUID).
    name:       Human-readable name.
    type:       One of ``dataset``, ``model``, ``feature``, ``report``.
    path:       Filesystem or storage path to the asset.
    description: Free-text description.
    owner:      Team or individual responsible for the asset.
    tags:       Arbitrary labels for search/filtering.
    created_at: ISO-8601 creation timestamp.
    updated_at: ISO-8601 last-update timestamp.
    schema:     Column/field descriptions (column → dtype or dict).
    lineage:    List of *asset_id* values this asset was derived from.
    quality_score: Latest quality score (0–100), or -1 if unknown.
    """

    asset_id: str
    name: str
    type: AssetType
    path: str
    description: str
    owner: str
    tags: List[str] = field(default_factory=list)
    created_at: str = field(default_factory=lambda: datetime.utcnow().isoformat())
    updated_at: str = field(default_factory=lambda: datetime.utcnow().isoformat())
    schema: Dict[str, Any] = field(default_factory=dict)
    lineage: List[str] = field(default_factory=list)
    quality_score: float = -1.0

    # ------------------------------------------------------------------
    # Serialisation helpers
    # ------------------------------------------------------------------

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)

    @classmethod
    def from_dict(cls, d: Dict[str, Any]) -> "DataAsset":
        return cls(**d)


# ---------------------------------------------------------------------------
# Catalog
# ---------------------------------------------------------------------------

class DataCatalog:
    """
    Central registry for all data assets in the lab.

    The catalog is stored as a JSON file under ``data/catalog/catalog.json``
    and is loaded into memory on construction.  Every mutating operation
    immediately persists to disk.

    Parameters
    ----------
    catalog_dir:
        Override the default storage directory (useful for testing).
    """

    def __init__(self, catalog_dir: Optional[Path] = None) -> None:
        self._dir = Path(catalog_dir) if catalog_dir else _CATALOG_ROOT
        self._dir.mkdir(parents=True, exist_ok=True)
        self._path = self._dir / _CATALOG_FILENAME
        self._assets: Dict[str, DataAsset] = {}
        self._load()

    # ------------------------------------------------------------------
    # CRUD
    # ------------------------------------------------------------------

    def register_asset(
        self,
        name: str,
        type: AssetType,
        path: str,
        description: str = "",
        owner: str = "",
        tags: Optional[List[str]] = None,
        schema: Optional[Dict[str, Any]] = None,
    ) -> DataAsset:
        """
        Create and persist a new ``DataAsset``.

        Returns
        -------
        DataAsset
            The newly registered asset.
        """
        asset_id = uuid.uuid4().hex[:16]
        now = datetime.utcnow().isoformat()
        asset = DataAsset(
            asset_id=asset_id,
            name=name,
            type=type,
            path=path,
            description=description,
            owner=owner,
            tags=tags or [],
            created_at=now,
            updated_at=now,
            schema=schema or {},
            lineage=[],
            quality_score=-1.0,
        )
        self._assets[asset_id] = asset
        self._save()
        logger.info("Registered asset '%s' (%s) with id=%s.", name, type, asset_id)
        return asset

    def get_asset(self, asset_id: str) -> DataAsset:
        """Retrieve an asset by its *asset_id*.  Raises ``KeyError`` if not found."""
        if asset_id not in self._assets:
            raise KeyError(f"Asset '{asset_id}' not found in catalog.")
        return self._assets[asset_id]

    def update_quality_score(self, asset_id: str, score: float) -> None:
        """Set the quality score for an asset and update *updated_at*."""
        asset = self.get_asset(asset_id)
        asset.quality_score = round(float(score), 2)
        asset.updated_at = datetime.utcnow().isoformat()
        self._save()
        logger.info("Updated quality score for '%s' to %.2f.", asset.name, score)

    def add_lineage(self, asset_id: str, parent_asset_id: str) -> None:
        """
        Declare that *asset_id* was derived from *parent_asset_id*.

        Both assets must already be registered.  Duplicate edges are silently
        ignored.
        """
        # Validate both exist
        self.get_asset(parent_asset_id)
        asset = self.get_asset(asset_id)

        if parent_asset_id not in asset.lineage:
            asset.lineage.append(parent_asset_id)
            asset.updated_at = datetime.utcnow().isoformat()
            self._save()
            logger.debug("Added lineage edge %s → %s.", parent_asset_id, asset_id)

    # ------------------------------------------------------------------
    # Search and filtering
    # ------------------------------------------------------------------

    def search(
        self,
        query: str,
        type: Optional[AssetType] = None,
        tags: Optional[List[str]] = None,
    ) -> List[DataAsset]:
        """
        Fuzzy text search on name and description.

        Matching is case-insensitive substring matching (no external library
        needed).  Results are sorted by relevance (number of query terms
        matched), then by name.

        Parameters
        ----------
        query:
            Space-separated keywords.
        type:
            Restrict results to a specific asset type.
        tags:
            Restrict results to assets that carry **all** listed tags.
        """
        tokens = [t.lower() for t in query.split() if t]
        results: List[tuple[int, DataAsset]] = []

        for asset in self._assets.values():
            # Type filter
            if type and asset.type != type:
                continue
            # Tag filter (all supplied tags must be present)
            if tags and not all(t in asset.tags for t in tags):
                continue
            # Score by token matches in name + description
            haystack = f"{asset.name} {asset.description}".lower()
            score = sum(1 for t in tokens if t in haystack)
            if score > 0 or not tokens:
                results.append((score, asset))

        results.sort(key=lambda x: (-x[0], x[1].name))
        return [a for _, a in results]

    def list_by_owner(self, owner: str) -> List[DataAsset]:
        """Return all assets owned by *owner* (case-insensitive)."""
        owner_lower = owner.lower()
        return [a for a in self._assets.values() if a.owner.lower() == owner_lower]

    def list_by_tag(self, tag: str) -> List[DataAsset]:
        """Return all assets that carry *tag*."""
        return [a for a in self._assets.values() if tag in a.tags]

    # ------------------------------------------------------------------
    # Lineage
    # ------------------------------------------------------------------

    def get_full_lineage(self, asset_id: str) -> Dict[str, Any]:
        """
        Build a recursive lineage tree for *asset_id*.

        Returns
        -------
        dict
            ``{"asset_id": ..., "name": ..., "parents": [<same structure>, ...]}``
        """
        asset = self.get_asset(asset_id)
        return {
            "asset_id": asset.asset_id,
            "name": asset.name,
            "type": asset.type,
            "parents": [
                self.get_full_lineage(pid)
                for pid in asset.lineage
                if pid in self._assets
            ],
        }

    # ------------------------------------------------------------------
    # Reporting
    # ------------------------------------------------------------------

    def generate_catalog_report(self) -> str:
        """
        Render a full catalog inventory as a Markdown string.

        Includes a summary table and per-type breakdowns.
        """
        assets = sorted(self._assets.values(), key=lambda a: a.name)

        by_type: Dict[str, List[DataAsset]] = {}
        for a in assets:
            by_type.setdefault(a.type, []).append(a)

        lines: List[str] = [
            "# Data Catalog Report",
            "",
            f"**Generated:** {datetime.utcnow().isoformat()}",
            f"**Total assets:** {len(assets)}",
            "",
            "## Summary by Type",
            "",
            "| Type    | Count |",
            "|---------|-------|",
        ]
        for t, group in sorted(by_type.items()):
            lines.append(f"| {t} | {len(group)} |")

        lines += ["", "## Asset Inventory", ""]

        for t, group in sorted(by_type.items()):
            lines += [f"### {t.capitalize()}s", ""]
            lines += [
                "| Name | Owner | Tags | Quality Score | Path |",
                "|------|-------|------|---------------|------|",
            ]
            for a in group:
                score = f"{a.quality_score:.1f}" if a.quality_score >= 0 else "N/A"
                tag_str = ", ".join(a.tags) if a.tags else "—"
                lines.append(
                    f"| {a.name} | {a.owner or '—'} | {tag_str} | {score} | `{a.path}` |"
                )
            lines.append("")

        return "\n".join(lines)

    # ------------------------------------------------------------------
    # Persistence
    # ------------------------------------------------------------------

    def _load(self) -> None:
        if self._path.exists():
            try:
                with self._path.open("r", encoding="utf-8") as fh:
                    raw = json.load(fh)
                self._assets = {
                    asset_id: DataAsset.from_dict(data)
                    for asset_id, data in raw.items()
                }
                logger.debug("Loaded %d assets from catalog.", len(self._assets))
            except (json.JSONDecodeError, TypeError) as exc:
                logger.warning("Could not parse catalog file (%s); starting fresh.", exc)
                self._assets = {}

    def _save(self) -> None:
        payload = {aid: a.to_dict() for aid, a in self._assets.items()}
        with self._path.open("w", encoding="utf-8") as fh:
            json.dump(payload, fh, indent=2, default=str)
