"""Database connection pooling and caching utilities"""

import asyncpg
from sqlalchemy import create_engine
from sqlalchemy.pool import QueuePool, NullPool
import logging
from typing import Optional, Dict, Any
import json
from functools import wraps

logger = logging.getLogger(__name__)


class DatabasePool:
    """Manage database connection pooling"""

    def __init__(self, db_url: str, max_connections: int = 20, echo: bool = False):
        self.db_url = db_url
        self.max_connections = max_connections
        self.engine = None
        self.async_pool = None

    def init_sync_pool(self):
        """Initialize synchronous connection pool"""
        try:
            self.engine = create_engine(
                self.db_url,
                poolclass=QueuePool,
                pool_size=self.max_connections,
                max_overflow=10,
                pool_pre_ping=True,  # Test connections before use
                pool_recycle=3600,   # Recycle connections hourly
                echo=False
            )
            logger.info(f"Initialized sync database pool with max {self.max_connections} connections")
            return self.engine
        except Exception as e:
            logger.error(f"Failed to initialize database pool: {str(e)}")
            raise

    async def init_async_pool(self, min_size: int = 5, max_size: int = 20):
        """Initialize asynchronous connection pool"""
        try:
            # Convert SQLAlchemy URL to asyncpg format
            db_url = self.db_url.replace("postgresql://", "postgresql+asyncpg://")

            self.async_pool = await asyncpg.create_pool(
                db_url,
                min_size=min_size,
                max_size=max_size,
                command_timeout=60
            )
            logger.info(f"Initialized async connection pool with {min_size}-{max_size} connections")
            return self.async_pool
        except Exception as e:
            logger.error(f"Failed to initialize async pool: {str(e)}")
            # Fallback to non-async
            logger.warning("Falling back to non-pooled connections")

    def close(self):
        """Close connection pool"""
        if self.engine:
            self.engine.dispose()
            logger.info("Closed database connection pool")

    async def close_async(self):
        """Close async connection pool"""
        if self.async_pool:
            await self.async_pool.close()
            logger.info("Closed async connection pool")


class RedisCache:
    """Redis caching layer"""

    def __init__(self, redis_url: str = "redis://localhost:6379/0"):
        self.redis_url = redis_url
        self.redis = None

    async def init(self):
        """Initialize Redis connection"""
        try:
            import aioredis
            self.redis = await aioredis.create_redis_pool(self.redis_url)
            logger.info("Connected to Redis cache")
        except ImportError:
            logger.warning("aioredis not installed, using in-memory cache")
            self.redis = None
        except Exception as e:
            logger.error(f"Failed to connect to Redis: {str(e)}")
            self.redis = None

    async def get(self, key: str) -> Optional[Any]:
        """Get value from cache"""
        if not self.redis:
            return None

        try:
            value = await self.redis.get(key)
            if value:
                logger.info(f"Cache hit: {key}")
                return json.loads(value)
        except Exception as e:
            logger.error(f"Cache get error: {str(e)}")
        return None

    async def set(self, key: str, value: Any, ttl: int = 3600):
        """Set value in cache with TTL"""
        if not self.redis:
            return

        try:
            await self.redis.setex(key, ttl, json.dumps(value))
            logger.info(f"Cache set: {key} (TTL: {ttl}s)")
        except Exception as e:
            logger.error(f"Cache set error: {str(e)}")

    async def delete(self, key: str):
        """Delete value from cache"""
        if not self.redis:
            return

        try:
            await self.redis.delete(key)
            logger.info(f"Cache deleted: {key}")
        except Exception as e:
            logger.error(f"Cache delete error: {str(e)}")

    async def flush_all(self):
        """Flush all cache"""
        if not self.redis:
            return

        try:
            await self.redis.flushdb()
            logger.info("Cache flushed")
        except Exception as e:
            logger.error(f"Cache flush error: {str(e)}")

    async def close(self):
        """Close Redis connection"""
        if self.redis:
            self.redis.close()
            await self.redis.wait_closed()


class QueryCache:
    """Simple in-memory query cache"""

    def __init__(self):
        self.cache = {}

    def cache_result(self, query_hash: str, result: Any, ttl: int = 3600):
        """Cache query result"""
        import time
        self.cache[query_hash] = {
            'result': result,
            'timestamp': time.time(),
            'ttl': ttl
        }

    def get_cached(self, query_hash: str) -> Optional[Any]:
        """Get cached query result"""
        import time

        if query_hash not in self.cache:
            return None

        cached = self.cache[query_hash]
        if time.time() - cached['timestamp'] > cached['ttl']:
            del self.cache[query_hash]
            return None

        return cached['result']

    def clear(self):
        """Clear cache"""
        self.cache.clear()


# Global instances
db_pool: Optional[DatabasePool] = None
redis_cache: Optional[RedisCache] = None
query_cache = QueryCache()


async def init_caches(db_url: str = None, redis_url: str = None):
    """Initialize all caching layers"""
    global db_pool, redis_cache

    if db_url:
        db_pool = DatabasePool(db_url)
        db_pool.init_sync_pool()
        await db_pool.init_async_pool()

    if redis_url:
        redis_cache = RedisCache(redis_url)
        await redis_cache.init()


async def close_caches():
    """Close all caching layers"""
    global db_pool, redis_cache

    if db_pool:
        await db_pool.close_async()
        db_pool.close()

    if redis_cache:
        await redis_cache.close()


def cached_query(ttl: int = 3600):
    """Decorator for caching query results"""
    def decorator(func):
        @wraps(func)
        async def wrapper(*args, **kwargs):
            import hashlib
            query_hash = hashlib.md5(f"{func.__name__}{args}{kwargs}".encode()).hexdigest()

            # Try Redis first
            if redis_cache:
                cached_result = await redis_cache.get(query_hash)
                if cached_result:
                    return cached_result

            # Try in-memory cache
            cached_result = query_cache.get_cached(query_hash)
            if cached_result:
                return cached_result

            # Execute query
            result = await func(*args, **kwargs)

            # Cache result
            if redis_cache:
                await redis_cache.set(query_hash, result, ttl)
            query_cache.cache_result(query_hash, result, ttl)

            return result

        return wrapper
    return decorator
