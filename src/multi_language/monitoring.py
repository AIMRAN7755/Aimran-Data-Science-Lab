"""
Real-Time Monitoring System for Multi-Language Services
Tracks metrics, performance, and health across all services
"""

import logging
from typing import Dict, List, Any, Optional
from datetime import datetime, timedelta
from dataclasses import dataclass, field, asdict
from collections import defaultdict
import psutil
import threading
import time

logger = logging.getLogger(__name__)


@dataclass
class ServiceMetric:
    """Service metric data point"""
    service_id: str
    timestamp: str
    cpu_percent: float = 0.0
    memory_mb: float = 0.0
    memory_percent: float = 0.0
    requests_per_second: float = 0.0
    average_latency_ms: float = 0.0
    error_rate: float = 0.0
    uptime_seconds: int = 0
    request_count: int = 0
    error_count: int = 0

    def to_dict(self) -> Dict:
        return asdict(self)


@dataclass
class ServiceHealthSnapshot:
    """Service health status at a point in time"""
    service_id: str
    language: str
    status: str  # running, stopped, error
    timestamp: str
    is_healthy: bool
    cpu_usage: float
    memory_usage: float
    uptime_seconds: int
    last_error: Optional[str] = None
    request_latency_ms: float = 0.0


class MetricsCollector:
    """Collects and stores service metrics"""

    def __init__(self, max_history: int = 1000):
        self.metrics: Dict[str, List[ServiceMetric]] = defaultdict(list)
        self.max_history = max_history

    def record_metric(self, metric: ServiceMetric) -> bool:
        """Record a metric for a service"""
        try:
            service_id = metric.service_id
            self.metrics[service_id].append(metric)

            # Keep only recent metrics
            if len(self.metrics[service_id]) > self.max_history:
                self.metrics[service_id] = self.metrics[service_id][-self.max_history:]

            return True
        except Exception as e:
            logger.error(f"❌ Failed to record metric: {str(e)}")
            return False

    def get_latest_metric(self, service_id: str) -> Optional[ServiceMetric]:
        """Get latest metric for service"""
        if service_id in self.metrics and self.metrics[service_id]:
            return self.metrics[service_id][-1]
        return None

    def get_metrics_range(self, service_id: str, start_index: int = 0, 
                         end_index: Optional[int] = None) -> List[ServiceMetric]:
        """Get range of metrics"""
        if service_id not in self.metrics:
            return []

        if end_index is None:
            end_index = len(self.metrics[service_id])

        return self.metrics[service_id][start_index:end_index]

    def get_metrics_since(self, service_id: str, minutes: int = 60) -> List[ServiceMetric]:
        """Get metrics from last N minutes"""
        if service_id not in self.metrics:
            return []

        cutoff_time = datetime.now() - timedelta(minutes=minutes)
        result = []

        for metric in self.metrics[service_id]:
            metric_time = datetime.fromisoformat(metric.timestamp)
            if metric_time >= cutoff_time:
                result.append(metric)

        return result

    def get_average_metrics(self, service_id: str, minutes: int = 60) -> Dict[str, float]:
        """Get average metrics over time period"""
        metrics = self.get_metrics_since(service_id, minutes)

        if not metrics:
            return {}

        return {
            'avg_cpu_percent': sum(m.cpu_percent for m in metrics) / len(metrics),
            'avg_memory_mb': sum(m.memory_mb for m in metrics) / len(metrics),
            'avg_memory_percent': sum(m.memory_percent for m in metrics) / len(metrics),
            'avg_latency_ms': sum(m.average_latency_ms for m in metrics) / len(metrics),
            'avg_error_rate': sum(m.error_rate for m in metrics) / len(metrics),
        }

    def get_all_services_latest(self) -> Dict[str, ServiceMetric]:
        """Get latest metric for all services"""
        result = {}
        for service_id in self.metrics:
            if self.metrics[service_id]:
                result[service_id] = self.metrics[service_id][-1]
        return result

    def clear_service_metrics(self, service_id: str) -> bool:
        """Clear metrics for service"""
        try:
            if service_id in self.metrics:
                del self.metrics[service_id]
            return True
        except Exception as e:
            logger.error(f"❌ Failed to clear metrics: {str(e)}")
            return False


class HealthMonitor:
    """Monitors service health and generates alerts"""

    def __init__(self, metrics_collector: MetricsCollector):
        self.metrics = metrics_collector
        self.health_snapshots: Dict[str, List[ServiceHealthSnapshot]] = defaultdict(list)
        self.alerts: List[Dict[str, Any]] = []
        self.max_snapshots = 500
        self.thresholds = {
            'cpu_percent': 80.0,
            'memory_percent': 85.0,
            'error_rate': 0.05,
        }

    def take_health_snapshot(self, service_id: str, language: str, 
                           status: str, is_healthy: bool) -> ServiceHealthSnapshot:
        """Take a health snapshot of a service"""
        try:
            latest_metric = self.metrics.get_latest_metric(service_id)

            snapshot = ServiceHealthSnapshot(
                service_id=service_id,
                language=language,
                status=status,
                timestamp=datetime.now().isoformat(),
                is_healthy=is_healthy,
                cpu_usage=latest_metric.cpu_percent if latest_metric else 0.0,
                memory_usage=latest_metric.memory_percent if latest_metric else 0.0,
                uptime_seconds=latest_metric.uptime_seconds if latest_metric else 0,
                request_latency_ms=latest_metric.average_latency_ms if latest_metric else 0.0,
            )

            # Store snapshot
            self.health_snapshots[service_id].append(snapshot)

            # Keep only recent snapshots
            if len(self.health_snapshots[service_id]) > self.max_snapshots:
                self.health_snapshots[service_id] = self.health_snapshots[service_id][-self.max_snapshots:]

            # Check for alerts
            self._check_health_conditions(snapshot)

            return snapshot
        except Exception as e:
            logger.error(f"❌ Failed to take health snapshot: {str(e)}")
            return None

    def _check_health_conditions(self, snapshot: ServiceHealthSnapshot):
        """Check for health conditions that trigger alerts"""
        alerts = []

        if snapshot.cpu_usage > self.thresholds['cpu_percent']:
            alerts.append({
                'type': 'HIGH_CPU',
                'service_id': snapshot.service_id,
                'value': snapshot.cpu_usage,
                'threshold': self.thresholds['cpu_percent'],
            })

        if snapshot.memory_usage > self.thresholds['memory_percent']:
            alerts.append({
                'type': 'HIGH_MEMORY',
                'service_id': snapshot.service_id,
                'value': snapshot.memory_usage,
                'threshold': self.thresholds['memory_percent'],
            })

        if not snapshot.is_healthy:
            alerts.append({
                'type': 'SERVICE_UNHEALTHY',
                'service_id': snapshot.service_id,
                'status': snapshot.status,
            })

        # Record alerts
        for alert in alerts:
            alert['timestamp'] = datetime.now().isoformat()
            self.alerts.append(alert)
            logger.warning(f"⚠️ Alert: {alert['type']} for {snapshot.service_id}")

    def get_service_health(self, service_id: str) -> Optional[ServiceHealthSnapshot]:
        """Get latest health snapshot"""
        if service_id in self.health_snapshots and self.health_snapshots[service_id]:
            return self.health_snapshots[service_id][-1]
        return None

    def get_health_history(self, service_id: str, limit: int = 100) -> List[ServiceHealthSnapshot]:
        """Get health history for service"""
        return self.health_snapshots.get(service_id, [])[-limit:]

    def get_all_health_status(self) -> Dict[str, ServiceHealthSnapshot]:
        """Get latest health for all services"""
        result = {}
        for service_id in self.health_snapshots:
            if self.health_snapshots[service_id]:
                result[service_id] = self.health_snapshots[service_id][-1]
        return result

    def get_recent_alerts(self, minutes: int = 60, limit: int = 100) -> List[Dict]:
        """Get recent alerts"""
        cutoff_time = datetime.now() - timedelta(minutes=minutes)
        result = []

        for alert in self.alerts[-limit:]:
            alert_time = datetime.fromisoformat(alert.get('timestamp', ''))
            if alert_time >= cutoff_time:
                result.append(alert)

        return result

    def set_threshold(self, metric_name: str, value: float):
        """Set alert threshold"""
        if metric_name in self.thresholds:
            self.thresholds[metric_name] = value
            logger.info(f"✅ Set {metric_name} threshold to {value}")


class PerformanceAnalyzer:
    """Analyzes service performance"""

    def __init__(self, metrics_collector: MetricsCollector):
        self.metrics = metrics_collector

    def get_service_performance_report(self, service_id: str, 
                                      minutes: int = 60) -> Dict[str, Any]:
        """Generate performance report for service"""
        try:
            metrics = self.metrics.get_metrics_since(service_id, minutes)
            if not metrics:
                return {}

            # Calculate statistics
            cpu_values = [m.cpu_percent for m in metrics]
            memory_values = [m.memory_percent for m in metrics]
            latency_values = [m.average_latency_ms for m in metrics if m.average_latency_ms > 0]
            error_rates = [m.error_rate for m in metrics]

            return {
                'service_id': service_id,
                'period_minutes': minutes,
                'data_points': len(metrics),
                'cpu': {
                    'min': min(cpu_values) if cpu_values else 0,
                    'max': max(cpu_values) if cpu_values else 0,
                    'avg': sum(cpu_values) / len(cpu_values) if cpu_values else 0,
                },
                'memory': {
                    'min': min(memory_values) if memory_values else 0,
                    'max': max(memory_values) if memory_values else 0,
                    'avg': sum(memory_values) / len(memory_values) if memory_values else 0,
                },
                'latency': {
                    'min': min(latency_values) if latency_values else 0,
                    'max': max(latency_values) if latency_values else 0,
                    'avg': sum(latency_values) / len(latency_values) if latency_values else 0,
                },
                'error_rate': {
                    'min': min(error_rates) if error_rates else 0,
                    'max': max(error_rates) if error_rates else 0,
                    'avg': sum(error_rates) / len(error_rates) if error_rates else 0,
                },
            }
        except Exception as e:
            logger.error(f"❌ Failed to generate report: {str(e)}")
            return {}

    def get_comparative_analysis(self, service_ids: List[str]) -> Dict[str, Any]:
        """Compare performance across services"""
        try:
            result = {}
            for service_id in service_ids:
                report = self.get_service_performance_report(service_id)
                if report:
                    result[service_id] = report

            return result
        except Exception as e:
            logger.error(f"❌ Failed to compare: {str(e)}")
            return {}

    def identify_bottlenecks(self, service_id: str) -> List[Dict[str, Any]]:
        """Identify performance bottlenecks"""
        try:
            report = self.get_service_performance_report(service_id)
            bottlenecks = []

            if report.get('cpu', {}).get('avg', 0) > 70:
                bottlenecks.append({
                    'type': 'HIGH_CPU',
                    'severity': 'high',
                    'avg_value': report['cpu']['avg'],
                    'recommendation': 'Optimize CPU-intensive operations',
                })

            if report.get('memory', {}).get('avg', 0) > 75:
                bottlenecks.append({
                    'type': 'HIGH_MEMORY',
                    'severity': 'high',
                    'avg_value': report['memory']['avg'],
                    'recommendation': 'Review memory usage and optimize data structures',
                })

            if report.get('latency', {}).get('avg', 0) > 1000:
                bottlenecks.append({
                    'type': 'HIGH_LATENCY',
                    'severity': 'medium',
                    'avg_value': report['latency']['avg'],
                    'recommendation': 'Optimize slow operations or add caching',
                })

            return bottlenecks
        except Exception as e:
            logger.error(f"❌ Failed to identify bottlenecks: {str(e)}")
            return []


class RealtimeMonitoringSystem:
    """Unified real-time monitoring system"""

    def __init__(self):
        self.metrics_collector = MetricsCollector()
        self.health_monitor = HealthMonitor(self.metrics_collector)
        self.performance_analyzer = PerformanceAnalyzer(self.metrics_collector)
        self.monitoring_thread = None

    def start_monitoring(self):
        """Start monitoring thread"""
        if self.monitoring_thread is None or not self.monitoring_thread.is_alive():
            self.monitoring_thread = threading.Thread(
                target=self._monitoring_loop,
                daemon=True
            )
            self.monitoring_thread.start()
            logger.info("✅ Real-time monitoring started")

    def stop_monitoring(self):
        """Stop monitoring thread"""
        if self.monitoring_thread:
            self.monitoring_thread.join(timeout=5)
            logger.info("✅ Real-time monitoring stopped")

    def _monitoring_loop(self):
        """Main monitoring loop"""
        while True:
            try:
                # Collect system metrics
                metric = ServiceMetric(
                    service_id='system',
                    timestamp=datetime.now().isoformat(),
                    cpu_percent=psutil.cpu_percent(interval=0.1),
                    memory_percent=psutil.virtual_memory().percent,
                    memory_mb=psutil.virtual_memory().used / (1024 * 1024),
                )

                self.metrics_collector.record_metric(metric)

                time.sleep(5)
            except Exception as e:
                logger.error(f"❌ Monitoring loop error: {str(e)}")
                time.sleep(10)

    def get_system_status(self) -> Dict[str, Any]:
        """Get current system status"""
        try:
            metric = self.metrics_collector.get_latest_metric('system')
            health = self.health_monitor.get_all_health_status()
            recent_alerts = self.health_monitor.get_recent_alerts(minutes=30)

            return {
                'timestamp': datetime.now().isoformat(),
                'system_metrics': metric.to_dict() if metric else {},
                'service_health': {sid: s.__dict__ for sid, s in health.items()},
                'recent_alerts': recent_alerts,
            }
        except Exception as e:
            logger.error(f"❌ Failed to get status: {str(e)}")
            return {}


# Global monitoring system instance (lazy init — call init_monitoring() explicitly)
_monitoring_system_instance = None

def get_monitoring_system() -> RealtimeMonitoringSystem:
    """Get or create the monitoring system singleton"""
    global _monitoring_system_instance
    if _monitoring_system_instance is None:
        _monitoring_system_instance = RealtimeMonitoringSystem()
    return _monitoring_system_instance

def init_monitoring():
    """Initialize and start the monitoring system explicitly"""
    system = get_monitoring_system()
    system.start_monitoring()
    logger.info("✅ Real-Time Monitoring System initialized")
    return system

# Backward-compatible alias
monitoring_system = None

logger.info("✅ Real-Time Monitoring System module loaded (call init_monitoring() to start)")
