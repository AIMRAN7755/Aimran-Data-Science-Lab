"""API-level auth helpers (separate from security.auth business logic)."""

from __future__ import annotations

import logging
import os

logger = logging.getLogger(__name__)


def init_auth() -> None:
    """
    Perform any startup-time auth initialisation.

    Currently a no-op — the AuthManager bootstraps itself lazily.
    Called from the FastAPI lifespan if needed.
    """
    logger.info("Auth subsystem initialised (REQUIRE_API_KEY=%s)",
                os.getenv("REQUIRE_API_KEY", "false"))
