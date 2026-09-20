"""Performance monitoring and metrics collection"""

import time
import psutil
import logging
from typing import Dict, List, Any
from dataclasses import dataclass, asdict
from datetime import datetime
from collections import deque
import json
from pathlib import Path

logger = logging.getLogger(__name__)


@dataclass
class PerformanceMetric:
    """Performance metric data point"""
    timestamp: str
    metric_name: str
    value: float
    unit: str
    source: str = "system"


class MetricsCollector:
    """Collect and store performance metrics"""

    def __init__(self, max_history: int = 1000, log_file: str = "logs/metrics.json"):
        self.max_history = max_history
        self.log_file = Path(log_file)
        self.log_file.parent.mkdir(parents=True, exist_ok=True)

        self.metrics = deque(maxlen=max_history)
        self.api_requests = deque(maxlen=1000)
        self.db_queries = deque(maxlen=1000)

    def record_metric(self, metric_name: str, value: float, unit: str, source: str = "system"):
        """Record a performance metric"""
        metric = PerformanceMetric(
            timestamp=datetime.now().isoformat(),
            metric_name=metric_name,
            value=value,
            unit=unit,
            source=source
        )
        self.metrics.append(metric)
        logger.debug(f"Recorded metric: {metric_name} = {value} {unit}")

    def record_api_request(self, endpoint: str, method: str, duration_ms: float, 
                          status_code: int, response_size_bytes: int = 0):
        """Record API request metrics"""
        self.api_requests.append({
            'timestamp': datetime.now().isoformat(),
            'endpoint': endpoint,
            'method': method,
            'duration_ms': duration_ms,
            'status_code': status_code,
            'response_size_bytes': response_size_bytes,
        })

    def record_db_query(self, query_type: str, duration_ms: float, rows_affected: int = 0):
        """Record database query metrics"""
        self.db_queries.append({
            'timestamp': datetime.now().isoformat(),
            'query_type': query_type,
            'duration_ms': duration_ms,
            'rows_affected': rows_affected,
        })

    def get_summary(self) -> Dict[str, Any]:
        """Get metrics summary"""
        if not self.metrics:
            return {}

        cpu_metrics = [m.value for m in self.metrics if m.metric_name == 'cpu_percent']
        memory_metrics = [m.value for m in self.metrics if m.metric_name == 'memory_percent']

        api_durations = [r['duration_ms'] for r in self.api_requests]
        db_durations = [q['duration_ms'] for q in self.db_queries]

        import statistics

        return {
            'total_metrics_collected': len(self.metrics),
            'cpu': {
                'avg': statistics.mean(cpu_metrics) if cpu_metrics else 0,
                'max': max(cpu_metrics) if cpu_metrics else 0,
                'min': min(cpu_metrics) if cpu_metrics else 0,
            } if cpu_metrics else None,
            'memory': {
                'avg': statistics.mean(memory_metrics) if memory_metrics else 0,
                'max': max(memory_metrics) if memory_metrics else 0,
                'min': min(memory_metrics) if memory_metrics else 0,
            } if memory_metrics else None,
            'api_requests': {
                'total': len(self.api_requests),
                'avg_duration_ms': statistics.mean(api_durations) if api_durations else 0,
                'max_duration_ms': max(api_durations) if api_durations else 0,
                'min_duration_ms': min(api_durations) if api_durations else 0,
            } if api_durations else None,
            'db_queries': {
                'total': len(self.db_queries),
                'avg_duration_ms': statistics.mean(db_durations) if db_durations else 0,
                'max_duration_ms': max(db_durations) if db_durations else 0,
                'min_duration_ms': min(db_durations) if db_durations else 0,
            } if db_durations else None,
        }

    def save_to_file(self):
        """Save metrics to file"""
        try:
            data = {
                'timestamp': datetime.now().isoformat(),
                'metrics': [asdict(m) for m in self.metrics],
                'api_requests': list(self.api_requests),
                'db_queries': list(self.db_queries),
                'summary': self.get_summary(),
            }

            with open(self.log_file, 'w') as f:
                json.dump(data, f, indent=2)

            logger.info(f"Metrics saved to {self.log_file}")
        except Exception as e:
            logger.error(f"Failed to save metrics: {str(e)}")

    def export_to_csv(self, filepath: str):
        """Export metrics to CSV"""
        import csv

        try:
            with open(filepath, 'w', newline='') as f:
                writer = csv.DictWriter(f, fieldnames=['timestamp', 'metric_name', 'value', 'unit', 'source'])
                writer.writeheader()
                for metric in self.metrics:
                    writer.writerow(asdict(metric))

            logger.info(f"Metrics exported to {filepath}")
        except Exception as e:
            logger.error(f"Failed to export metrics: {str(e)}")


class SystemMonitor:
    """Monitor system performance"""

    def __init__(self, collector: MetricsCollector):
        self.collector = collector
        self.process = psutil.Process()

    def collect_system_metrics(self):
        """Collect current system metrics"""
        try:
            cpu_percent = psutil.cpu_percent(interval=0.1)
            memory = psutil.virtual_memory()
            disk = psutil.disk_usage('/')

            self.collector.record_metric('cpu_percent', cpu_percent, '%')
            self.collector.record_metric('memory_percent', memory.percent, '%')
            self.collector.record_metric('disk_percent', disk.percent, '%')
            self.collector.record_metric('memory_mb', memory.available / 1024 / 1024, 'MB')

            return {
                'cpu_percent': cpu_percent,
                'memory_percent': memory.percent,
                'disk_percent': disk.percent,
            }
        except Exception as e:
            logger.error(f"Failed to collect system metrics: {str(e)}")
            return None

    def collect_process_metrics(self):
        """Collect current process metrics"""
        try:
            memory_info = self.process.memory_info()
            memory_mb = memory_info.rss / 1024 / 1024

            try:
                cpu_num = self.process.cpu_num()
            except:
                cpu_num = -1

            self.collector.record_metric('process_memory_mb', memory_mb, 'MB', source='process')

            return {
                'memory_mb': memory_mb,
                'cpu_num': cpu_num,
            }
        except Exception as e:
            logger.error(f"Failed to collect process metrics: {str(e)}")
            return None


# Global collector instance
metrics_collector = MetricsCollector()
system_monitor = SystemMonitor(metrics_collector)


# Middleware for FastAPI
class PerformanceMiddleware:
    """FastAPI middleware for performance monitoring"""

    def __init__(self, app, collector: MetricsCollector):
        self.app = app
        self.collector = collector

    async def __call__(self, scope, receive, send):
        if scope["type"] != "http":
            await self.app(scope, receive, send)
            return

        start_time = time.perf_counter()

        async def send_wrapper(message):
            if message["type"] == "http.response.start":
                duration_ms = (time.perf_counter() - start_time) * 1000
                status_code = message["status"]

                # Record metrics
                endpoint = scope.get("path", "unknown")
                method = scope.get("method", "unknown")

                self.collector.record_api_request(
                    endpoint=endpoint,
                    method=method,
                    duration_ms=duration_ms,
                    status_code=status_code
                )

            await send(message)

        await self.app(scope, receive, send_wrapper)


# Decorator for tracking function performance
def track_performance(func_name: str = None):
    """Decorator to track function performance"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            name = func_name or func.__name__
            start = time.perf_counter()

            try:
                result = func(*args, **kwargs)
                return result
            finally:
                duration_ms = (time.perf_counter() - start) * 1000
                metrics_collector.record_metric(
                    f'function_{name}_duration',
                    duration_ms,
                    'ms',
                    source='function'
                )

        return wrapper
    return decorator
