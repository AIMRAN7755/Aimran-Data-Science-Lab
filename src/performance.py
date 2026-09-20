"""Performance profiling and optimization utilities"""

import time
import psutil
import tracemalloc
from functools import wraps
from typing import Callable, Any, Dict
import logging
import json
from datetime import datetime
from pathlib import Path

logger = logging.getLogger(__name__)


class PerformanceProfiler:
    """Profile function execution time and memory usage"""

    def __init__(self, name: str = "profile", save_path: str = "logs/profiles"):
        self.name = name
        self.save_path = Path(save_path)
        self.save_path.mkdir(parents=True, exist_ok=True)
        self.results = {}

    def profile_function(self, func: Callable) -> Callable:
        """Decorator to profile a function"""
        @wraps(func)
        def wrapper(*args, **kwargs) -> Any:
            # Start memory tracking
            tracemalloc.start()
            start_time = time.perf_counter()
            start_memory = psutil.Process().memory_info().rss / 1024 / 1024  # MB

            try:
                result = func(*args, **kwargs)
                return result
            finally:
                # End profiling
                end_time = time.perf_counter()
                end_memory = psutil.Process().memory_info().rss / 1024 / 1024  # MB
                current, peak = tracemalloc.get_traced_memory()
                tracemalloc.stop()

                execution_time = end_time - start_time
                memory_used = end_memory - start_memory
                peak_memory = peak / 1024 / 1024  # MB

                # Log results
                profile_data = {
                    'function': func.__name__,
                    'execution_time_ms': execution_time * 1000,
                    'memory_used_mb': memory_used,
                    'peak_memory_mb': peak_memory,
                    'timestamp': datetime.now().isoformat()
                }

                logger.info(f"Profile {func.__name__}: {execution_time*1000:.2f}ms, "
                          f"Memory: {memory_used:.2f}MB, Peak: {peak_memory:.2f}MB")

                self.results[func.__name__] = profile_data
                self._save_profile(profile_data)

        return wrapper

    def _save_profile(self, data: Dict):
        """Save profile data to file"""
        filepath = self.save_path / f"profile_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        with open(filepath, 'w') as f:
            json.dump(data, f, indent=2)


class PerformanceOptimizer:
    """Optimize code performance"""

    @staticmethod
    def optimize_dataframe_loading(filepath: str, chunksize: int = 10000):
        """Load large dataframes in chunks for memory efficiency"""
        import pandas as pd
        chunks = []
        start = time.perf_counter()

        for chunk in pd.read_csv(filepath, chunksize=chunksize):
            chunks.append(chunk)

        df = pd.concat(chunks, ignore_index=True)
        elapsed = time.perf_counter() - start
        logger.info(f"Loaded {len(df)} rows in {elapsed:.2f}s using chunked reading")
        return df

    @staticmethod
    def vectorize_operations(func: Callable) -> Callable:
        """Decorator to ensure numpy vectorization"""
        @wraps(func)
        def wrapper(data, *args, **kwargs):
            import numpy as np
            if not isinstance(data, np.ndarray):
                data = np.array(data)
            return func(data, *args, **kwargs)
        return wrapper

    @staticmethod
    def parallel_process(data, func: Callable, n_jobs: int = -1):
        """Process data in parallel"""
        from joblib import Parallel, delayed
        import pandas as pd

        if isinstance(data, pd.DataFrame):
            chunks = np.array_split(data, max(1, n_jobs if n_jobs > 0 else psutil.cpu_count()))
            results = Parallel(n_jobs=n_jobs)(delayed(func)(chunk) for chunk in chunks)
            return pd.concat(results, ignore_index=True)
        else:
            return Parallel(n_jobs=n_jobs)(delayed(func)(item) for item in data)


class CacheOptimizer:
    """Caching utilities for performance"""

    def __init__(self, ttl: int = 3600):
        self.cache = {}
        self.ttl = ttl
        self.timestamps = {}

    def cache_result(self, key: str, value: Any, ttl: int = None):
        """Cache a result with TTL"""
        self.cache[key] = value
        self.timestamps[key] = time.time()
        logger.info(f"Cached {key}")

    def get_cached(self, key: str) -> Any:
        """Get cached result if still valid"""
        if key not in self.cache:
            return None

        ttl = self.timestamps.get(key, 0) + (self.ttl)
        if time.time() > ttl:
            del self.cache[key]
            del self.timestamps[key]
            return None

        logger.info(f"Retrieved from cache: {key}")
        return self.cache[key]

    def clear_cache(self):
        """Clear all cache"""
        self.cache.clear()
        self.timestamps.clear()


class QueryOptimizer:
    """Optimize database queries"""

    @staticmethod
    def index_suggestion(df, columns):
        """Suggest columns to index"""
        for col in columns:
            unique_ratio = df[col].nunique() / len(df)
            if unique_ratio > 0.5:
                logger.warning(f"Column '{col}' has {unique_ratio*100:.1f}% unique values - consider indexing")

    @staticmethod
    def batch_query(query_func: Callable, items: list, batch_size: int = 100):
        """Execute queries in batches"""
        results = []
        for i in range(0, len(items), batch_size):
            batch = items[i:i+batch_size]
            results.extend(query_func(batch))
        logger.info(f"Processed {len(items)} items in {len(items)//batch_size + 1} batches")
        return results


# Decorators for easy use
def profile(func: Callable) -> Callable:
    """Quick profile decorator"""
    profiler = PerformanceProfiler()
    return profiler.profile_function(func)


def cache(ttl: int = 3600):
    """Caching decorator"""
    cache_manager = CacheOptimizer(ttl=ttl)

    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            cache_key = f"{func.__name__}_{str(args)}_{str(kwargs)}"
            cached_value = cache_manager.get_cached(cache_key)

            if cached_value is not None:
                return cached_value

            result = func(*args, **kwargs)
            cache_manager.cache_result(cache_key, result)
            return result

        return wrapper
    return decorator


def time_it(func: Callable) -> Callable:
    """Simple timing decorator"""
    @wraps(func)
    def wrapper(*args, **kwargs):
        start = time.perf_counter()
        result = func(*args, **kwargs)
        elapsed = time.perf_counter() - start
        logger.info(f"{func.__name__} took {elapsed*1000:.2f}ms")
        return result
    return wrapper
