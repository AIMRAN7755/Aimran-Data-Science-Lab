"""
src/api/dependencies.py — Shared FastAPI dependencies.

Centralizes reusable dependencies so route modules do not re-implement
common concerns:

- Authentication: re-exports of the strict auth dependency
- Pagination: cursor / offset pagination parameters
- Common query parameters: shared filters for list endpoints

Usage
-----
    from fastapi import Depends
    from src.api.dependencies import PaginationParams

    @app.get("/items")
    async def list_items(pagination: PaginationParams = Depends()):
        ...
"""

from __future__ import annotations

from typing import Optional

from fastapi import Depends, Query

from src.api.auth import require_auth, require_analyst, require_admin

# ── Auth dependencies (single source of truth for route protection) ─────────

__all__ = [
    "require_auth",
    "require_analyst",
    "require_admin",
    "PaginationParams",
    "CommonQueryParams",
]


class PaginationParams:
    """Offset-based pagination dependency (limit + offset)."""

    def __init__(
        self,
        limit: int = Query(100, ge=1, le=1000, description="Max items to return"),
        offset: int = Query(0, ge=0, description="Items to skip"),
    ):
        self.limit = limit
        self.offset = offset

    def to_dict(self) -> dict:
        return {"limit": self.limit, "offset": self.offset}


class CommonQueryParams:
    """
    Shared filters for list endpoints: optional resource-name search and
    optional status filter (e.g. ``status=running|completed|failed``).
    """

    def __init__(
        self,
        name: Optional[str] = Query(None, description="Case-insensitive name search"),
        status: Optional[str] = Query(None, description="Filter by status"),
    ):
        self.name = name
        self.status = status

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "status": self.status,
        }