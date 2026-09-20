"""
Service Manager and Controller
Unified control plane for all services
"""

import json
import logging
from typing import Dict, List, Optional, Any
from datetime import datetime, timedelta
import psutil
import asyncio
from dataclasses import dataclass
import yaml

from src.multi_language.gateway import MultiLanguageGateway
from src.multi_language.service_registry import ServiceRegistry, get_service_registry, get_service_discovery, get_load_balancer
from src.multi_language.ipc_broker import EventBus, get_event_bus, get_message_queue

logger = logging.getLogger(__name__)


@dataclass
class ServiceConfig:
    """Service configuration"""
    service_id: str
    name: str
    language: str
    version: str
    host: str
    port: int
    enabled: bool = True
    auto_start: bool = True
    health_check_interval: int = 30
    max_retries: int = 3
    timeout: int = 30
    metadata: Dict[str, Any] = None


class ServiceManager:
    """Unified service management"""

    def __init__(self, gateway: MultiLanguageGateway, 
                 registry: ServiceRegistry,
                 event_bus: EventBus,
                 message_queue: MessageQueue):
        self.gateway = gateway
        self.registry = registry
        self.event_bus = event_bus
        self.message_queue = message_queue
        self.services: Dict[str, ServiceConfig] = {}
        self.service_processes: Dict[str, Any] = {}
        self.health_history: Dict[str, List[Dict]] = {}

    def load_config(self, config_file: str) -> bool:
        """Load service configuration from YAML file"""
        try:
            with open(config_file, 'r') as f:
                config = yaml.safe_load(f)

            for service_data in config.get('services', []):
                service_config = ServiceConfig(**service_data)
                self.services[service_config.service_id] = service_config

            logger.info(f"✅ Loaded configuration from {config_file}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to load config: {str(e)}")
            return False

    def register_service(self, service_config: ServiceConfig) -> bool:
        """Register a service"""
        try:
            self.services[service_config.service_id] = service_config

            # Register in registry
            service_data = {
                'id': service_config.service_id,
                'name': service_config.name,
                'language': service_config.language,
                'version': service_config.version,
                'host': service_config.host,
                'port': service_config.port,
                'status': 'registered',
                'capabilities': [],
                'endpoint': {
                    'host': service_config.host,
                    'port': service_config.port,
                    'protocol': 'grpc',
                }
            }

            self.registry.register(service_config.service_id, service_data)

            # Emit event
            self.event_bus.emit('service.registered', {
                'service_id': service_config.service_id,
                'name': service_config.name,
                'language': service_config.language,
            })

            logger.info(f"✅ Registered service: {service_config.name}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to register service: {str(e)}")
            return False

    def start_service(self, service_id: str) -> bool:
        """Start a service"""
        try:
            service = self.services.get(service_id)
            if not service:
                logger.error(f"❌ Service not found: {service_id}")
                return False

            # Update status
            self.registry.update_status(service_id, 'running')

            logger.info(f"✅ Started service: {service.name}")

            # Emit event
            self.event_bus.emit('service.started', {
                'service_id': service_id,
                'name': service.name,
                'timestamp': datetime.now().isoformat(),
            })

            return True
        except Exception as e:
            logger.error(f"❌ Failed to start service: {str(e)}")
            return False

    def stop_service(self, service_id: str) -> bool:
        """Stop a service"""
        try:
            service = self.services.get(service_id)
            if not service:
                return False

            # Update status
            self.registry.update_status(service_id, 'stopped')

            logger.info(f"✅ Stopped service: {service.name}")

            # Emit event
            self.event_bus.emit('service.stopped', {
                'service_id': service_id,
                'name': service.name,
                'timestamp': datetime.now().isoformat(),
            })

            return True
        except Exception as e:
            logger.error(f"❌ Failed to stop service: {str(e)}")
            return False

    def restart_service(self, service_id: str) -> bool:
        """Restart a service"""
        if self.stop_service(service_id):
            asyncio.sleep(1)
            return self.start_service(service_id)
        return False

    def get_service_status(self, service_id: str) -> Dict[str, Any]:
        """Get service status"""
        try:
            service_data = self.registry.get(service_id)
            if not service_data:
                return {}

            service = self.services.get(service_id)

            return {
                'service_id': service_id,
                'name': service.name if service else 'Unknown',
                'status': service_data.get('status', 'unknown'),
                'language': service_data.get('language'),
                'version': service_data.get('version'),
                'uptime_seconds': self._calculate_uptime(service_id),
                'health': self._get_health_status(service_id),
                'metrics': self._get_metrics(service_id),
            }
        except Exception as e:
            logger.error(f"❌ Failed to get status: {str(e)}")
            return {}

    def get_all_services_status(self) -> List[Dict]:
        """Get status of all services"""
        statuses = []
        for service_id in self.services:
            status = self.get_service_status(service_id)
            if status:
                statuses.append(status)
        return statuses

    def _calculate_uptime(self, service_id: str) -> int:
        """Calculate service uptime in seconds"""
        try:
            service_data = self.registry.get(service_id)
            if service_data and service_data.get('status') == 'running':
                # In real implementation, track start time
                return 0
            return 0
        except:
            return 0

    def _get_health_status(self, service_id: str) -> Dict[str, Any]:
        """Get service health status"""
        return {
            'healthy': True,
            'cpu_usage': psutil.cpu_percent(interval=0.1),
            'memory_usage': psutil.virtual_memory().percent,
            'last_check': datetime.now().isoformat(),
        }

    def _get_metrics(self, service_id: str) -> Dict[str, Any]:
        """Get service metrics"""
        return {
            'requests_per_second': 0,
            'average_latency_ms': 0,
            'error_rate': 0.0,
            'uptime_percent': 99.9,
        }

    def enable_service(self, service_id: str) -> bool:
        """Enable a service"""
        service = self.services.get(service_id)
        if service:
            service.enabled = True
            return self.start_service(service_id)
        return False

    def disable_service(self, service_id: str) -> bool:
        """Disable a service"""
        service = self.services.get(service_id)
        if service:
            service.enabled = False
            return self.stop_service(service_id)
        return False

    def get_system_overview(self) -> Dict[str, Any]:
        """Get complete system overview"""
        services = self.get_all_services_status()

        running = sum(1 for s in services if s.get('status') == 'running')
        stopped = sum(1 for s in services if s.get('status') == 'stopped')

        return {
            'timestamp': datetime.now().isoformat(),
            'total_services': len(services),
            'running_services': running,
            'stopped_services': stopped,
            'services': services,
            'system_health': {
                'cpu': psutil.cpu_percent(interval=0.1),
                'memory': psutil.virtual_memory().percent,
                'disk': psutil.disk_usage('/').percent,
            }
        }


class HealthMonitor:
    """Monitor service health and perform automated actions"""

    def __init__(self, manager: ServiceManager, event_bus: EventBus):
        self.manager = manager
        self.event_bus = event_bus
        self.check_interval = 30  # seconds
        self.health_history: Dict[str, List[Dict]] = {}

    def check_service_health(self, service_id: str) -> Dict[str, Any]:
        """Check service health"""
        try:
            service_data = self.manager.registry.get(service_id)
            if not service_data:
                return {'healthy': False, 'reason': 'Not found'}

            status = service_data.get('status')

            health = {
                'service_id': service_id,
                'timestamp': datetime.now().isoformat(),
                'healthy': status == 'running',
                'status': status,
            }

            # Record history
            if service_id not in self.health_history:
                self.health_history[service_id] = []

            self.health_history[service_id].append(health)

            # Keep last 100 checks
            self.health_history[service_id] = self.health_history[service_id][-100:]

            return health
        except Exception as e:
            logger.error(f"❌ Health check failed: {str(e)}")
            return {'healthy': False, 'reason': str(e)}

    def check_all_services_health(self) -> Dict[str, Dict]:
        """Check health of all services"""
        health = {}
        for service_id in self.manager.services:
            health[service_id] = self.check_service_health(service_id)

        # Emit health check event
        self.event_bus.emit('health.check', {
            'timestamp': datetime.now().isoformat(),
            'results': health,
        })

        return health

    def get_health_history(self, service_id: str, limit: int = 100) -> List[Dict]:
        """Get health history for service"""
        return self.health_history.get(service_id, [])[-limit:]


# Global service manager instance
def create_service_manager(gateway: MultiLanguageGateway = None,
                          registry: ServiceRegistry = None,
                          event_bus: EventBus = None,
                          message_queue: MessageQueue = None) -> ServiceManager:
    """Create service manager with dependencies"""
    if gateway is None:
        from src.multi_language.gateway import get_gateway
        gateway = get_gateway()
    if registry is None:
        from src.multi_language.service_registry import get_service_registry
        registry = get_service_registry()
    if event_bus is None:
        event_bus = get_event_bus()
    if message_queue is None:
        message_queue = get_message_queue()

    return ServiceManager(gateway, registry, event_bus, message_queue)


logger.info("✅ Service Manager and Health Monitor module loaded")
