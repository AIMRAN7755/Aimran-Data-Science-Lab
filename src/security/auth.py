"""
Enterprise authentication and authorisation.

Provides:
- JWT access + refresh token pair (HS256)
- API-key authentication (bcrypt-hashed keys stored in memory)
- Role-based access control (RBAC) via UserRole enum
- Per-key sliding-window rate limiter
- Graceful fallback when PyJWT / passlib are not installed
"""

from __future__ import annotations

import hashlib
import logging
import os
import secrets
import time
import uuid
from collections import deque
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional, Tuple

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Optional heavy dependencies — fail gracefully
# ---------------------------------------------------------------------------

try:
    import jwt as _jwt
    _JWT_AVAILABLE = True
except ImportError:
    _jwt = None         # type: ignore[assignment]
    _JWT_AVAILABLE = False
    logger.warning("PyJWT not installed — JWT tokens will be opaque pseudo-tokens")

try:
    from passlib.context import CryptContext as _CryptContext
    _pwd_context = _CryptContext(schemes=["bcrypt"], deprecated="auto")
    _BCRYPT_AVAILABLE = True
except Exception:
    _pwd_context = None     # type: ignore[assignment]
    _BCRYPT_AVAILABLE = False
    logger.warning("passlib/bcrypt not available — using SHA-256 fallback for passwords")


# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

_DEV_SECRET = "aimran-dev-secret-change-in-production"
_SECRET_KEY = os.getenv("SECRET_KEY") or _DEV_SECRET
if _SECRET_KEY == _DEV_SECRET and os.getenv("ENVIRONMENT", "development").lower() == "production":
    raise RuntimeError(
        "SECRET_KEY must be set in production — refusing to start with the insecure dev default "
        "(a known key lets anyone forge valid JWTs)."
    )
_ACCESS_TOKEN_TTL   = int(os.getenv("ACCESS_TOKEN_TTL_SECONDS",   str(60 * 60)))       # 1h
_REFRESH_TOKEN_TTL  = int(os.getenv("REFRESH_TOKEN_TTL_SECONDS",  str(60 * 60 * 24 * 7)))  # 7d
_ALGORITHM = "HS256"


# ---------------------------------------------------------------------------
# RBAC
# ---------------------------------------------------------------------------

class UserRole(str, Enum):
    ADMIN  = "admin"
    DATA_SCIENTIST = "data_scientist"
    VIEWER = "viewer"


# ---------------------------------------------------------------------------
# User model
# ---------------------------------------------------------------------------

@dataclass
class User:
    user_id: str
    username: str
    email: str
    role: UserRole
    is_active: bool = True
    created_at: float = field(default_factory=time.time)
    _hashed_password: str = field(default="", repr=False)
    _hashed_api_key: str  = field(default="", repr=False)
    _raw_api_key: str     = field(default="", repr=False)   # only set immediately after creation

    def to_dict(self) -> Dict[str, Any]:
        return {
            "user_id":    self.user_id,
            "username":   self.username,
            "email":      self.email,
            "role":       self.role.value,
            "is_active":  self.is_active,
            "created_at": self.created_at,
        }


# ---------------------------------------------------------------------------
# Password / key helpers
# ---------------------------------------------------------------------------

def _hash_secret(secret: str) -> str:
    if _BCRYPT_AVAILABLE and _pwd_context:
        return _pwd_context.hash(secret)
    return hashlib.sha256(secret.encode()).hexdigest()


def _verify_secret(plain: str, hashed: str) -> bool:
    if _BCRYPT_AVAILABLE and _pwd_context:
        try:
            return _pwd_context.verify(plain, hashed)
        except Exception:
            pass
    return hashlib.sha256(plain.encode()).hexdigest() == hashed


# ---------------------------------------------------------------------------
# JWT helpers
# ---------------------------------------------------------------------------

def _create_token(payload: Dict[str, Any], ttl_seconds: int) -> str:
    import copy
    p = copy.copy(payload)
    p["exp"] = int(time.time()) + ttl_seconds
    p["iat"] = int(time.time())
    if _JWT_AVAILABLE and _jwt:
        return _jwt.encode(p, _SECRET_KEY, algorithm=_ALGORITHM)
    # Opaque pseudo-token (base64 of payload hash)
    import base64, json
    raw = json.dumps(p, sort_keys=True).encode()
    return base64.urlsafe_b64encode(raw).decode()


def _decode_token(token: str) -> Optional[Dict[str, Any]]:
    if _JWT_AVAILABLE and _jwt:
        try:
            return _jwt.decode(token, _SECRET_KEY, algorithms=[_ALGORITHM])
        except Exception:
            return None
    # Opaque pseudo-token path
    import base64, json
    try:
        raw = base64.urlsafe_b64decode(token.encode() + b"==")
        payload = json.loads(raw)
        if payload.get("exp", 0) < time.time():
            return None
        return payload
    except Exception:
        return None


# ---------------------------------------------------------------------------
# Auth Manager
# ---------------------------------------------------------------------------

class AuthManager:
    """
    Manages users, API keys, and JWT tokens in memory.

    In production, swap ``self._users`` for a DB-backed store.
    """

    def __init__(self):
        self._users: Dict[str, User] = {}               # user_id -> User
        self._by_username: Dict[str, str] = {}          # username -> user_id
        self._refresh_tokens: Dict[str, str] = {}       # refresh_token -> user_id
        self._api_key_index: Dict[str, str] = {}        # key_prefix -> user_id
        self._bootstrap_admin()

    def _bootstrap_admin(self) -> None:
        """Create a default admin account if none exists."""
        _prod = os.getenv("ENVIRONMENT", "development").lower() == "production"
        admin_pass = os.getenv("ADMIN_PASSWORD") or ("" if _prod else "admin")
        if not admin_pass:
            raise RuntimeError(
                "ADMIN_PASSWORD must be set in production — refusing to bootstrap the admin "
                "account with a default password."
            )
        admin_key  = os.getenv("ADMIN_API_KEY",  "")
        self._create_user_internal("admin", "admin@aimran.ai", UserRole.ADMIN, admin_pass, admin_key)

    def _create_user_internal(
        self,
        username: str,
        email: str,
        role: UserRole,
        password: str = "",
        api_key: str = "",
    ) -> User:
        uid = str(uuid.uuid4())
        raw_key = api_key or secrets.token_urlsafe(32)
        user = User(
            user_id=uid,
            username=username,
            email=email,
            role=role,
            _hashed_password=_hash_secret(password) if password else "",
            _hashed_api_key=_hash_secret(raw_key),
            _raw_api_key=raw_key,
        )
        self._users[uid] = user
        self._by_username[username] = uid
        # Index by first 8 chars for O(1) lookup
        prefix = raw_key[:8]
        self._api_key_index[prefix] = uid
        return user

    # ------------------------------------------------------------------
    # Login / token management
    # ------------------------------------------------------------------

    def login(
        self, username: str, password: str
    ) -> Optional[Tuple[str, str, User]]:
        """
        Authenticate with username + password.

        Returns
        -------
        (access_token, refresh_token, user) or None on failure.
        """
        uid = self._by_username.get(username)
        if uid is None:
            return None
        user = self._users.get(uid)
        if user is None or not user.is_active:
            return None
        if not _verify_secret(password, user._hashed_password):
            return None

        access  = _create_token({"sub": uid, "role": user.role.value, "type": "access"},  _ACCESS_TOKEN_TTL)
        refresh = _create_token({"sub": uid, "type": "refresh"}, _REFRESH_TOKEN_TTL)
        self._refresh_tokens[refresh] = uid
        return access, refresh, user

    def refresh_access_token(
        self, refresh_token: str
    ) -> Optional[Tuple[str, str]]:
        """Exchange a valid refresh token for a new (access, refresh) pair."""
        payload = _decode_token(refresh_token)
        if payload is None or payload.get("type") != "refresh":
            return None
        uid = payload.get("sub")
        if uid not in self._users:
            return None
        user = self._users[uid]
        # Rotate refresh token
        del self._refresh_tokens[refresh_token]
        new_access  = _create_token({"sub": uid, "role": user.role.value, "type": "access"}, _ACCESS_TOKEN_TTL)
        new_refresh = _create_token({"sub": uid, "type": "refresh"}, _REFRESH_TOKEN_TTL)
        self._refresh_tokens[new_refresh] = uid
        return new_access, new_refresh

    def get_user_from_token(self, token: str) -> Optional[User]:
        """Validate a JWT access token and return the associated user."""
        payload = _decode_token(token)
        if payload is None or payload.get("type") != "access":
            return None
        uid = payload.get("sub")
        user = self._users.get(uid)
        return user if user and user.is_active else None

    def authenticate_api_key(self, api_key: str) -> Optional[User]:
        """Validate an API key and return the associated user."""
        if len(api_key) < 8:
            return None
        prefix = api_key[:8]
        uid = self._api_key_index.get(prefix)
        if uid is None:
            return None
        user = self._users.get(uid)
        if user is None or not user.is_active:
            return None
        if _verify_secret(api_key, user._hashed_api_key):
            return user
        return None

    # ------------------------------------------------------------------
    # User management
    # ------------------------------------------------------------------

    def create_user(
        self,
        username: str,
        email: str,
        role: UserRole = UserRole.VIEWER,
        password: Optional[str] = None,
    ) -> User:
        if username in self._by_username:
            raise ValueError(f"User '{username}' already exists")
        return self._create_user_internal(
            username=username,
            email=email,
            role=role,
            password=password or secrets.token_urlsafe(16),
        )

    def deactivate_user(self, user_id: str) -> bool:
        user = self._users.get(user_id)
        if user is None:
            return False
        user.is_active = False
        return True

    def list_users(self) -> List[Dict[str, Any]]:
        return [u.to_dict() for u in self._users.values()]

    # ------------------------------------------------------------------
    # RBAC
    # ------------------------------------------------------------------

    _ROLE_HIERARCHY = {
        UserRole.VIEWER: 0,
        UserRole.DATA_SCIENTIST: 1,
        UserRole.ADMIN: 2,
    }

    def require_role(self, user: User, required_role: UserRole) -> bool:
        return self._ROLE_HIERARCHY.get(user.role, 0) >= self._ROLE_HIERARCHY.get(required_role, 99)


# ---------------------------------------------------------------------------
# Rate Limiter
# ---------------------------------------------------------------------------

class RateLimiter:
    """
    Sliding-window in-memory rate limiter.

    Parameters
    ----------
    max_requests : int
        Number of requests allowed per window.
    window_seconds : int
        Window duration.
    storage_uri : str or None
        Accepted but unused (placeholder for Redis backend).
    """

    def __init__(
        self,
        max_requests: int = 200,
        window_seconds: int = 60,
        storage_uri: Optional[str] = None,
    ):
        self.max_requests = max_requests
        self.window_seconds = window_seconds
        self._windows: Dict[str, deque] = {}

    def is_allowed(self, client_id: str) -> bool:
        now = time.time()
        window_start = now - self.window_seconds
        if client_id not in self._windows:
            self._windows[client_id] = deque()
        q = self._windows[client_id]
        while q and q[0] < window_start:
            q.popleft()
        if len(q) >= self.max_requests:
            return False
        q.append(now)
        return True
