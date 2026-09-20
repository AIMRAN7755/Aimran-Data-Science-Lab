"""
Multi-Language gRPC Gateway Service
Enables seamless communication between Python, C#, Rust, and other languages
"""

from concurrent import futures
import logging
import asyncio
import json
from typing import Dict, List, Optional, Any
from dataclasses import dataclass, asdict
from datetime import datetime
import uuid
import redis
import psutil
import os
from pathlib import Path

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@dataclass
class ServiceRegistry:
    """Registry for managing service information"""

    services: Dict[str, 'ServiceInstance'] = None

    def __post_init__(self):
        if self.services is None:
            self.services = {}

    def register_service(self, service_id: str, service_info: Dict[str, Any]) -> bool:
        """Register a new service"""
        try:
            self.services[service_id] = ServiceInstance(
                service_id=service_id,
                service_name=service_info.get('name', 'Unknown'),
                language=service_info.get('language', 'unknown'),
                version=service_info.get('version', '0.0.1'),
                host=service_info.get('host', 'localhost'),
                port=service_info.get('port', 0),
                status='registered',
                metadata=service_info.get('metadata', {})
            )
            logger.info(f"✅ Registered service: {service_id}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to register service {service_id}: {str(e)}")
            return False

    def deregister_service(self, service_id: str) -> bool:
        """Deregister a service"""
        if service_id in self.services:
            del self.services[service_id]
            logger.info(f"✅ Deregistered service: {service_id}")
            return True
        return False

    def get_service(self, service_id: str) -> Optional['ServiceInstance']:
        """Get service by ID"""
        return self.services.get(service_id)

    def list_services(self) -> List['ServiceInstance']:
        """List all registered services"""
        return list(self.services.values())

    def get_services_by_language(self, language: str) -> List['ServiceInstance']:
        """Get services by programming language"""
        return [s for s in self.services.values() if s.language == language]

    def get_services_by_capability(self, capability: str) -> List['ServiceInstance']:
        """Get services with specific capability"""
        return [s for s in self.services.values() if capability in s.capabilities]


@dataclass
class ServiceInstance:
    """Represents a registered service"""
    service_id: str
    service_name: str
    language: str
    version: str
    host: str
    port: int
    status: str = 'running'
    metadata: Dict[str, str] = None
    capabilities: List[str] = None
    start_time: int = None

    def __post_init__(self):
        if self.metadata is None:
            self.metadata = {}
        if self.capabilities is None:
            self.capabilities = []
        if self.start_time is None:
            self.start_time = int(datetime.now().timestamp())

    def to_dict(self) -> Dict:
        """Convert to dictionary"""
        return asdict(self)

    def get_status(self) -> Dict[str, Any]:
        """Get current service status"""
        uptime = int(datetime.now().timestamp()) - self.start_time
        return {
            'service_id': self.service_id,
            'status': self.status,
            'uptime_seconds': uptime,
            'host': self.host,
            'port': self.port,
            'language': self.language,
        }


class ServiceBroker:
    """Handles inter-service communication using Redis pub/sub"""

    def __init__(self, redis_host: str = 'localhost', redis_port: int = 6379):
        self.redis_client = redis.Redis(
            host=redis_host,
            port=redis_port,
            decode_responses=True
        )
        self.subscriptions: Dict[str, List[str]] = {}

    def publish_message(self, topic: str, payload: Dict[str, Any], 
                       source_id: str = 'system') -> bool:
        """Publish message to topic"""
        try:
            message = {
                'id': str(uuid.uuid4()),
                'source': source_id,
                'topic': topic,
                'payload': json.dumps(payload),
                'timestamp': datetime.now().isoformat(),
            }

            self.redis_client.publish(topic, json.dumps(message))
            logger.info(f"📤 Published to {topic}: {payload}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to publish: {str(e)}")
            return False

    def subscribe_to_topic(self, topic: str, callback):
        """Subscribe to topic"""
        pubsub = self.redis_client.pubsub()
        pubsub.subscribe(topic)

        logger.info(f"📥 Subscribed to {topic}")

        for message in pubsub.listen():
            if message['type'] == 'message':
                try:
                    data = json.loads(message['data'])
                    callback(data)
                except Exception as e:
                    logger.error(f"❌ Error processing message: {str(e)}")

    def broadcast_event(self, event_type: str, data: Dict[str, Any]) -> bool:
        """Broadcast system event"""
        topic = f"events:{event_type}"
        return self.publish_message(topic, data)


class ServiceHealthChecker:
    """Monitor service health and status"""

    def __init__(self, registry: ServiceRegistry, broker: ServiceBroker):
        self.registry = registry
        self.broker = broker
        self.health_status: Dict[str, Dict] = {}

    def check_service_health(self, service_id: str) -> Dict[str, Any]:
        """Check if service is healthy"""
        service = self.registry.get_service(service_id)
        if not service:
            return {'healthy': False, 'reason': 'Service not found'}

        try:
            # Create health check data
            health_data = {
                'service_id': service_id,
                'timestamp': datetime.now().isoformat(),
                'cpu_usage': self._get_service_cpu(),
                'memory_usage': self._get_service_memory(),
                'status': service.status,
            }

            self.health_status[service_id] = health_data
            return {
                'healthy': service.status == 'running',
                'data': health_data
            }
        except Exception as e:
            logger.error(f"❌ Health check failed for {service_id}: {str(e)}")
            return {'healthy': False, 'reason': str(e)}

    def get_all_health_status(self) -> Dict[str, Dict]:
        """Get health status of all services"""
        status = {}
        for service_id in self.registry.services:
            status[service_id] = self.check_service_health(service_id)
        return status

    @staticmethod
    def _get_service_cpu() -> float:
        """Get CPU usage"""
        return psutil.cpu_percent(interval=0.1)

    @staticmethod
    def _get_service_memory() -> float:
        """Get memory usage in MB"""
        return psutil.virtual_memory().percent


class ServiceController:
    """Control service lifecycle (start, stop, restart)"""

    def __init__(self, registry: ServiceRegistry, broker: ServiceBroker):
        self.registry = registry
        self.broker = broker

    def start_service(self, service_id: str) -> bool:
        """Start a service"""
        service = self.registry.get_service(service_id)
        if not service:
            logger.error(f"❌ Service not found: {service_id}")
            return False

        service.status = 'running'
        self.broker.broadcast_event('service_started', {'service_id': service_id})
        logger.info(f"✅ Started service: {service_id}")
        return True

    def stop_service(self, service_id: str) -> bool:
        """Stop a service"""
        service = self.registry.get_service(service_id)
        if not service:
            return False

        service.status = 'stopped'
        self.broker.broadcast_event('service_stopped', {'service_id': service_id})
        logger.info(f"✅ Stopped service: {service_id}")
        return True

    def restart_service(self, service_id: str) -> bool:
        """Restart a service"""
        if self.stop_service(service_id):
            asyncio.sleep(1)
            return self.start_service(service_id)
        return False

    def reload_service(self, service_id: str) -> bool:
        """Reload service configuration without restart"""
        service = self.registry.get_service(service_id)
        if not service:
            return False

        self.broker.broadcast_event('service_reload', {'service_id': service_id})
        logger.info(f"✅ Reloaded service: {service_id}")
        return True


class MultiLanguageGateway:
    """Main gateway for multi-language service communication"""

    def __init__(self, redis_host: str = 'localhost', redis_port: int = 6379):
        self.registry = ServiceRegistry()
        self.broker = ServiceBroker(redis_host, redis_port)
        self.health_checker = ServiceHealthChecker(self.registry, self.broker)
        self.controller = ServiceController(self.registry, self.broker)

    def register_service(self, service_info: Dict[str, Any]) -> bool:
        """Register a service"""
        service_id = service_info.get('id', str(uuid.uuid4()))
        return self.registry.register_service(service_id, service_info)

    def get_system_overview(self) -> Dict[str, Any]:
        """Get complete system overview"""
        services = self.registry.list_services()
        health_status = self.health_checker.get_all_health_status()

        return {
            'timestamp': datetime.now().isoformat(),
            'total_services': len(services),
            'services': [s.to_dict() for s in services],
            'health_status': health_status,
            'system_metrics': {
                'cpu': psutil.cpu_percent(interval=0.1),
                'memory': psutil.virtual_memory().percent,
                'disk': psutil.disk_usage('/').percent,
            }
        }

    def get_service_details(self, service_id: str) -> Dict[str, Any]:
        """Get detailed information about a service"""
        service = self.registry.get_service(service_id)
        if not service:
            return {}

        health = self.health_checker.check_service_health(service_id)
        return {
            'service': service.to_dict(),
            'health': health,
            'status': service.get_status(),
        }


# Global gateway instance (lazy init — call init_gateway() explicitly)
_gateway_instance = None

def get_gateway() -> MultiLanguageGateway:
    """Get or create the gateway singleton"""
    global _gateway_instance
    if _gateway_instance is None:
        _gateway_instance = MultiLanguageGateway()
    return _gateway_instance

def init_gateway() -> MultiLanguageGateway:
    """Initialize the gateway explicitly"""
    gateway = get_gateway()
    logger.info("✅ Multi-Language Gateway initialized")
    return gateway

# Backward-compatible alias
gateway = None

logger.info("✅ Multi-Language Gateway module loaded (call init_gateway() to start)")
