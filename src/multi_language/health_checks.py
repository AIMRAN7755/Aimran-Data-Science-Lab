"""
Health Checks and Alerting System
Proactive monitoring of services with automated notifications
"""

import logging
from typing import Dict, List, Any, Optional, Callable
from datetime import datetime, timedelta
from dataclasses import dataclass
from enum import Enum
import time
import threading
import json

logger = logging.getLogger(__name__)


class AlertSeverity(Enum):
    """Alert severity levels"""
    INFO = "info"
    WARNING = "warning"
    CRITICAL = "critical"


class HealthCheckType(Enum):
    """Types of health checks"""
    ENDPOINT = "endpoint"
    PROCESS = "process"
    RESOURCE = "resource"
    DATABASE = "database"
    DEPENDENCY = "dependency"
    CUSTOM = "custom"


@dataclass
class HealthCheckResult:
    """Result of a health check"""
    check_id: str
    service_id: str
    check_type: HealthCheckType
    passed: bool
    timestamp: str
    message: str = ""
    response_time_ms: int = 0
    details: Dict[str, Any] = None

    def to_dict(self) -> Dict:
        return {
            'check_id': self.check_id,
            'service_id': self.service_id,
            'check_type': self.check_type.value,
            'passed': self.passed,
            'timestamp': self.timestamp,
            'message': self.message,
            'response_time_ms': self.response_time_ms,
            'details': self.details or {},
        }


@dataclass
class Alert:
    """System alert"""
    alert_id: str
    service_id: str
    severity: AlertSeverity
    title: str
    description: str
    timestamp: str
    resolved: bool = False
    resolved_at: Optional[str] = None
    triggered_by: Optional[str] = None  # Health check or metric that triggered it
    metadata: Dict[str, Any] = None

    def to_dict(self) -> Dict:
        return {
            'alert_id': self.alert_id,
            'service_id': self.service_id,
            'severity': self.severity.value,
            'title': self.title,
            'description': self.description,
            'timestamp': self.timestamp,
            'resolved': self.resolved,
            'resolved_at': self.resolved_at,
            'triggered_by': self.triggered_by,
            'metadata': self.metadata or {},
        }


class HealthChecker:
    """Performs health checks on services"""

    def __init__(self):
        self.checks: Dict[str, Dict[str, Any]] = {}
        self.check_results: Dict[str, List[HealthCheckResult]] = {}
        self.max_results = 1000

    def register_check(self, service_id: str, check_id: str, 
                      check_type: HealthCheckType, 
                      check_func: Callable) -> bool:
        """Register a health check"""
        try:
            key = f"{service_id}:{check_id}"
            self.checks[key] = {
                'service_id': service_id,
                'check_id': check_id,
                'check_type': check_type,
                'check_func': check_func,
                'last_run': None,
                'last_passed': None,
            }
            logger.info(f"✅ Registered health check: {check_id} for {service_id}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to register check: {str(e)}")
            return False

    async def run_check(self, service_id: str, check_id: str) -> HealthCheckResult:
        """Run a specific health check"""
        try:
            key = f"{service_id}:{check_id}"
            check_info = self.checks.get(key)

            if not check_info:
                logger.error(f"❌ Check not found: {check_id}")
                return None

            start_time = time.time()
            result = await self._execute_check(check_info)
            elapsed_ms = int((time.time() - start_time) * 1000)

            # Record result
            if key not in self.check_results:
                self.check_results[key] = []

            health_result = HealthCheckResult(
                check_id=check_id,
                service_id=service_id,
                check_type=check_info['check_type'],
                passed=result['passed'],
                timestamp=datetime.now().isoformat(),
                message=result.get('message', ''),
                response_time_ms=elapsed_ms,
                details=result.get('details', {})
            )

            self.check_results[key].append(health_result)

            # Keep only recent results
            if len(self.check_results[key]) > self.max_results:
                self.check_results[key] = self.check_results[key][-self.max_results:]

            # Update last run
            check_info['last_run'] = datetime.now().isoformat()
            if result['passed']:
                check_info['last_passed'] = datetime.now().isoformat()

            return health_result
        except Exception as e:
            logger.error(f"❌ Health check failed: {str(e)}")
            return None

    async def _execute_check(self, check_info: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a health check function"""
        try:
            check_func = check_info['check_func']
            result = await check_func() if callable(check_func) else check_func
            return result
        except Exception as e:
            logger.error(f"❌ Check execution failed: {str(e)}")
            return {'passed': False, 'message': str(e)}

    def get_check_result(self, service_id: str, check_id: str) -> Optional[HealthCheckResult]:
        """Get latest result of a check"""
        key = f"{service_id}:{check_id}"
        if key in self.check_results and self.check_results[key]:
            return self.check_results[key][-1]
        return None

    def get_check_history(self, service_id: str, check_id: str, 
                         limit: int = 100) -> List[HealthCheckResult]:
        """Get check result history"""
        key = f"{service_id}:{check_id}"
        return self.check_results.get(key, [])[-limit:]

    def get_service_checks(self, service_id: str) -> List[Dict]:
        """Get all checks for a service"""
        result = []
        for key, check_info in self.checks.items():
            if check_info['service_id'] == service_id:
                latest_result = self.get_check_result(service_id, check_info['check_id'])
                result.append({
                    'check_id': check_info['check_id'],
                    'check_type': check_info['check_type'].value,
                    'last_run': check_info.get('last_run'),
                    'last_passed': check_info.get('last_passed'),
                    'latest_result': latest_result.to_dict() if latest_result else None,
                })
        return result


class AlertManager:
    """Manages alerts and notifications"""

    def __init__(self):
        self.alerts: Dict[str, Alert] = {}
        self.alert_handlers: List[Callable] = []
        self.max_alerts = 2000
        self.suppressed_alerts: Dict[str, datetime] = {}
        self.alert_suppression_duration = 300  # 5 minutes

    def create_alert(self, service_id: str, severity: AlertSeverity, 
                    title: str, description: str, 
                    triggered_by: str = None) -> Alert:
        """Create a new alert"""
        try:
            alert_id = f"{service_id}_{int(time.time() * 1000)}"

            # Check if alert is suppressed
            suppression_key = f"{service_id}:{title}"
            if suppression_key in self.suppressed_alerts:
                if datetime.now() - self.suppressed_alerts[suppression_key] < timedelta(seconds=self.alert_suppression_duration):
                    logger.debug(f"⏭️ Alert suppressed: {title}")
                    return None

            alert = Alert(
                alert_id=alert_id,
                service_id=service_id,
                severity=severity,
                title=title,
                description=description,
                timestamp=datetime.now().isoformat(),
                triggered_by=triggered_by,
            )

            self.alerts[alert_id] = alert

            # Limit alert count
            if len(self.alerts) > self.max_alerts:
                # Remove oldest alerts
                oldest_ids = sorted(self.alerts.keys(), 
                                  key=lambda x: self.alerts[x].timestamp)[:100]
                for old_id in oldest_ids:
                    del self.alerts[old_id]

            # Log alert
            self._log_alert(alert)

            # Trigger handlers
            self._trigger_alert_handlers(alert)

            logger.info(f"🚨 Alert created: {alert.title} ({severity.value})")

            return alert
        except Exception as e:
            logger.error(f"❌ Failed to create alert: {str(e)}")
            return None

    def resolve_alert(self, alert_id: str) -> bool:
        """Resolve an alert"""
        try:
            if alert_id in self.alerts:
                alert = self.alerts[alert_id]
                alert.resolved = True
                alert.resolved_at = datetime.now().isoformat()
                logger.info(f"✅ Alert resolved: {alert.title}")
                return True
            return False
        except Exception as e:
            logger.error(f"❌ Failed to resolve alert: {str(e)}")
            return False

    def get_active_alerts(self) -> List[Alert]:
        """Get all active (unresolved) alerts"""
        return [a for a in self.alerts.values() if not a.resolved]

    def get_service_alerts(self, service_id: str) -> List[Alert]:
        """Get alerts for a service"""
        return [a for a in self.alerts.values() if a.service_id == service_id]

    def get_alerts_by_severity(self, severity: AlertSeverity) -> List[Alert]:
        """Get alerts by severity"""
        return [a for a in self.alerts.values() if a.severity == severity]

    def register_alert_handler(self, handler: Callable) -> bool:
        """Register an alert handler"""
        try:
            self.alert_handlers.append(handler)
            logger.info("✅ Alert handler registered")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to register handler: {str(e)}")
            return False

    def _trigger_alert_handlers(self, alert: Alert):
        """Trigger all registered alert handlers"""
        for handler in self.alert_handlers:
            try:
                handler(alert)
            except Exception as e:
                logger.error(f"❌ Alert handler failed: {str(e)}")

    def _log_alert(self, alert: Alert):
        """Log alert based on severity"""
        if alert.severity == AlertSeverity.CRITICAL:
            logger.critical(f"🚨 CRITICAL: {alert.title} - {alert.description}")
        elif alert.severity == AlertSeverity.WARNING:
            logger.warning(f"⚠️ WARNING: {alert.title} - {alert.description}")
        else:
            logger.info(f"ℹ️ INFO: {alert.title} - {alert.description}")

    def suppress_alert_type(self, service_id: str, alert_title: str, 
                           duration_seconds: int = None):
        """Suppress specific alert type"""
        if duration_seconds:
            self.alert_suppression_duration = duration_seconds

        suppression_key = f"{service_id}:{alert_title}"
        self.suppressed_alerts[suppression_key] = datetime.now()


class HealthCheckScheduler:
    """Schedules and executes health checks periodically"""

    def __init__(self, health_checker: HealthChecker, alert_manager: AlertManager):
        self.health_checker = health_checker
        self.alert_manager = alert_manager
        self.schedules: Dict[str, Dict[str, Any]] = {}
        self.scheduler_thread = None

    def schedule_check(self, service_id: str, check_id: str, 
                      interval_seconds: int) -> bool:
        """Schedule a periodic health check"""
        try:
            key = f"{service_id}:{check_id}"
            self.schedules[key] = {
                'service_id': service_id,
                'check_id': check_id,
                'interval': interval_seconds,
                'next_run': datetime.now().isoformat(),
            }
            logger.info(f"✅ Scheduled check: {check_id} every {interval_seconds}s")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to schedule check: {str(e)}")
            return False

    def start_scheduler(self):
        """Start the scheduler thread"""
        if self.scheduler_thread is None or not self.scheduler_thread.is_alive():
            self.scheduler_thread = threading.Thread(
                target=self._scheduler_loop,
                daemon=True
            )
            self.scheduler_thread.start()
            logger.info("✅ Health check scheduler started")

    def stop_scheduler(self):
        """Stop the scheduler thread"""
        if self.scheduler_thread:
            self.scheduler_thread.join(timeout=5)
            logger.info("✅ Health check scheduler stopped")

    def _scheduler_loop(self):
        """Main scheduler loop"""
        while True:
            try:
                now = datetime.now()

                for key, schedule in self.schedules.items():
                    next_run = datetime.fromisoformat(schedule['next_run'])

                    if now >= next_run:
                        # Run check in background
                        threading.Thread(
                            target=self._run_scheduled_check,
                            args=(schedule['service_id'], schedule['check_id'], schedule['interval']),
                            daemon=True
                        ).start()

                time.sleep(5)  # Check every 5 seconds
            except Exception as e:
                logger.error(f"❌ Scheduler error: {str(e)}")
                time.sleep(10)

    def _run_scheduled_check(self, service_id: str, check_id: str, interval: int):
        """Run scheduled check and handle results"""
        try:
            # Note: This is simplified; real implementation would be async
            result = self.health_checker.get_check_result(service_id, check_id)

            if result and not result.passed:
                # Create alert
                self.alert_manager.create_alert(
                    service_id=service_id,
                    severity=AlertSeverity.WARNING,
                    title=f"Health check failed: {check_id}",
                    description=result.message,
                    triggered_by=check_id
                )

            # Update next run time
            key = f"{service_id}:{check_id}"
            self.schedules[key]['next_run'] = (
                datetime.now() + timedelta(seconds=interval)
            ).isoformat()
        except Exception as e:
            logger.error(f"❌ Scheduled check error: {str(e)}")


class IntegratedHealthSystem:
    """Integrated health check and alerting system"""

    def __init__(self):
        self.health_checker = HealthChecker()
        self.alert_manager = AlertManager()
        self.scheduler = HealthCheckScheduler(self.health_checker, self.alert_manager)
        self.scheduler.start_scheduler()

    def register_service_check(self, service_id: str, check_id: str, 
                              check_type: HealthCheckType, 
                              check_func: Callable,
                              interval_seconds: int = 60) -> bool:
        """Register and schedule a service health check"""
        if self.health_checker.register_check(
            service_id, check_id, check_type, check_func
        ):
            return self.scheduler.schedule_check(service_id, check_id, interval_seconds)
        return False

    def get_system_health_report(self) -> Dict[str, Any]:
        """Generate comprehensive health report"""
        return {
            'timestamp': datetime.now().isoformat(),
            'active_alerts': len(self.alert_manager.get_active_alerts()),
            'critical_alerts': len(self.alert_manager.get_alerts_by_severity(AlertSeverity.CRITICAL)),
            'warning_alerts': len(self.alert_manager.get_alerts_by_severity(AlertSeverity.WARNING)),
            'checks_registered': len(self.health_checker.checks),
            'recent_alerts': [a.to_dict() for a in self.alert_manager.get_active_alerts()[:10]],
        }


# Global health system instance (lazy init — call init_health_system() explicitly)
_health_system_instance = None

def get_health_system() -> IntegratedHealthSystem:
    """Get or create the health system singleton"""
    global _health_system_instance
    if _health_system_instance is None:
        _health_system_instance = IntegratedHealthSystem()
    return _health_system_instance

def init_health_system() -> IntegratedHealthSystem:
    """Initialize and start the health system explicitly"""
    system = get_health_system()
    logger.info("✅ Health Checks and Alerting System initialized")
    return system

# Backward-compatible alias
health_system = None

logger.info("✅ Health Checks and Alerting System module loaded (call init_health_system() to start)")
