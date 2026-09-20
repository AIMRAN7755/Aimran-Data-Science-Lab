"""
Service Registry and Discovery System
Enables automatic service discovery and management
"""

import json
from typing import Dict, List, Optional, Any
from datetime import datetime, timedelta
import redis
import logging
from dataclasses import dataclass, asdict, field
import uuid

logger = logging.getLogger(__name__)


@dataclass
class ServiceEndpoint:
    """Service endpoint definition"""
    host: str
    port: int
    protocol: str = "grpc"  # grpc, http, rest
    tls_enabled: bool = False

    def get_address(self) -> str:
        """Get service address"""
        protocol_prefix = "grpcs" if (self.protocol == "grpc" and self.tls_enabled) else self.protocol
        return f"{protocol_prefix}://{self.host}:{self.port}"


@dataclass
class ServiceCapability:
    """Service capability definition"""
    name: str
    version: str = "1.0"
    parameters: Dict[str, Any] = field(default_factory=dict)


@dataclass
class ServiceMetadata:
    """Service metadata"""
    language: str  # python, csharp, rust, go, java
    framework: str  # fastapi, django, grpc, etc
    version: str
    author: str = ""
    description: str = ""
    tags: List[str] = field(default_factory=list)
    environment: str = "development"


class ServiceRegistry:
    """Centralized service registry using Redis"""

    def __init__(self, redis_host: str = 'localhost', redis_port: int = 6379):
        self.redis = redis.Redis(
            host=redis_host,
            port=redis_port,
            decode_responses=True
        )
        self.prefix = "aimran:services:"
        self.timeout = 300  # 5 minutes TTL

    def register(self, service_id: str, service_data: Dict[str, Any], 
                ttl: int = None) -> bool:
        """Register a service in the registry"""
        try:
            if ttl is None:
                ttl = self.timeout

            service_key = f"{self.prefix}{service_id}"
            service_json = json.dumps(service_data)

            # Store service info
            self.redis.setex(service_key, ttl, service_json)

            # Add to service index
            self.redis.sadd(f"{self.prefix}index", service_id)

            # Add to language index
            language = service_data.get('language', 'unknown')
            self.redis.sadd(f"{self.prefix}languages:{language}", service_id)

            # Add to capability index
            for capability in service_data.get('capabilities', []):
                self.redis.sadd(f"{self.prefix}capabilities:{capability}", service_id)

            logger.info(f"✅ Registered service: {service_id} ({language})")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to register service: {str(e)}")
            return False

    def deregister(self, service_id: str) -> bool:
        """Deregister a service"""
        try:
            service_key = f"{self.prefix}{service_id}"
            service_data = self.get(service_id)

            # Remove service info
            self.redis.delete(service_key)

            # Remove from indices
            self.redis.srem(f"{self.prefix}index", service_id)

            if service_data:
                language = service_data.get('language')
                if language:
                    self.redis.srem(f"{self.prefix}languages:{language}", service_id)

                for capability in service_data.get('capabilities', []):
                    self.redis.srem(f"{self.prefix}capabilities:{capability}", service_id)

            logger.info(f"✅ Deregistered service: {service_id}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to deregister service: {str(e)}")
            return False

    def get(self, service_id: str) -> Optional[Dict]:
        """Get service information"""
        try:
            service_key = f"{self.prefix}{service_id}"
            data = self.redis.get(service_key)
            if data:
                return json.loads(data)
            return None
        except Exception as e:
            logger.error(f"❌ Failed to get service: {str(e)}")
            return None

    def list_all(self) -> List[Dict]:
        """List all registered services"""
        try:
            service_ids = self.redis.smembers(f"{self.prefix}index")
            services = []
            for service_id in service_ids:
                service_data = self.get(service_id)
                if service_data:
                    services.append(service_data)
            return services
        except Exception as e:
            logger.error(f"❌ Failed to list services: {str(e)}")
            return []

    def list_by_language(self, language: str) -> List[Dict]:
        """List services by programming language"""
        try:
            service_ids = self.redis.smembers(f"{self.prefix}languages:{language}")
            services = []
            for service_id in service_ids:
                service_data = self.get(service_id)
                if service_data:
                    services.append(service_data)
            return services
        except Exception as e:
            logger.error(f"❌ Failed to list services by language: {str(e)}")
            return []

    def list_by_capability(self, capability: str) -> List[Dict]:
        """List services with specific capability"""
        try:
            service_ids = self.redis.smembers(f"{self.prefix}capabilities:{capability}")
            services = []
            for service_id in service_ids:
                service_data = self.get(service_id)
                if service_data:
                    services.append(service_data)
            return services
        except Exception as e:
            logger.error(f"❌ Failed to list services by capability: {str(e)}")
            return []

    def update_status(self, service_id: str, status: str) -> bool:
        """Update service status"""
        try:
            service_data = self.get(service_id)
            if service_data:
                service_data['status'] = status
                service_data['last_updated'] = datetime.now().isoformat()
                return self.register(service_id, service_data)
            return False
        except Exception as e:
            logger.error(f"❌ Failed to update status: {str(e)}")
            return False

    def heartbeat(self, service_id: str) -> bool:
        """Update service heartbeat (keep-alive)"""
        try:
            service_key = f"{self.prefix}{service_id}"
            ttl = self.timeout
            return self.redis.expire(service_key, ttl)
        except Exception as e:
            logger.error(f"❌ Heartbeat failed: {str(e)}")
            return False

    def get_registry_stats(self) -> Dict[str, Any]:
        """Get registry statistics"""
        try:
            all_services = self.list_all()
            by_language = {}

            for language in ['python', 'csharp', 'rust']:
                services = self.list_by_language(language)
                by_language[language] = len(services)

            return {
                'total_services': len(all_services),
                'by_language': by_language,
                'timestamp': datetime.now().isoformat(),
            }
        except Exception as e:
            logger.error(f"❌ Failed to get stats: {str(e)}")
            return {}


class ServiceDiscovery:
    """Service discovery for finding and connecting to services"""

    def __init__(self, registry: ServiceRegistry):
        self.registry = registry

    def discover_service(self, service_name: str) -> Optional[Dict]:
        """Discover a service by name"""
        services = self.registry.list_all()
        for service in services:
            if service.get('name') == service_name:
                return service
        return None

    def discover_by_capability(self, capability: str) -> List[Dict]:
        """Discover services with specific capability"""
        return self.registry.list_by_capability(capability)

    def discover_by_language(self, language: str) -> List[Dict]:
        """Discover services by language"""
        return self.registry.list_by_language(language)

    def find_healthy_service(self, service_name: str) -> Optional[Dict]:
        """Find a healthy instance of service"""
        services = self.registry.list_all()
        for service in services:
            if service.get('name') == service_name and service.get('status') == 'running':
                return service
        return None

    def get_endpoints(self, language: str = None, 
                     capability: str = None) -> List[str]:
        """Get service endpoints filtered by criteria"""
        if language:
            services = self.registry.list_by_language(language)
        elif capability:
            services = self.registry.list_by_capability(capability)
        else:
            services = self.registry.list_all()

        endpoints = []
        for service in services:
            endpoint_data = service.get('endpoint')
            if endpoint_data:
                endpoint = ServiceEndpoint(**endpoint_data)
                endpoints.append(endpoint.get_address())

        return endpoints


class ServiceLoadBalancer:
    """Load balancing for service discovery"""

    def __init__(self, registry: ServiceRegistry):
        self.registry = registry
        self.request_count: Dict[str, int] = {}

    def get_least_loaded_service(self, service_name: str) -> Optional[Dict]:
        """Get least loaded instance of service"""
        services = self.registry.list_all()
        matching_services = [s for s in services if s.get('name') == service_name and s.get('status') == 'running']

        if not matching_services:
            return None

        # Find service with least requests
        least_loaded = min(
            matching_services,
            key=lambda s: self.request_count.get(s.get('id'), 0)
        )

        self.request_count[least_loaded['id']] = self.request_count.get(least_loaded['id'], 0) + 1
        return least_loaded

    def get_random_service(self, service_name: str) -> Optional[Dict]:
        """Get random healthy service instance"""
        import random
        services = self.registry.list_all()
        matching_services = [s for s in services if s.get('name') == service_name and s.get('status') == 'running']

        if matching_services:
            return random.choice(matching_services)
        return None

    def get_round_robin_service(self, service_name: str) -> Optional[Dict]:
        """Get service using round-robin selection"""
        services = self.registry.list_all()
        matching_services = [s for s in services if s.get('name') == service_name and s.get('status') == 'running']

        if not matching_services:
            return None

        # Rotate through services
        key = f"rr:{service_name}"
        if key not in self.request_count:
            self.request_count[key] = 0

        index = self.request_count[key] % len(matching_services)
        self.request_count[key] += 1

        return matching_services[index]


# Global registry instance (lazy init — call init_registry() explicitly)
_service_registry_instance = None
_service_discovery_instance = None
_load_balancer_instance = None

def get_service_registry() -> ServiceRegistry:
    """Get or create the service registry singleton"""
    global _service_registry_instance
    if _service_registry_instance is None:
        _service_registry_instance = ServiceRegistry()
    return _service_registry_instance

def get_service_discovery() -> ServiceDiscovery:
    """Get or create the service discovery singleton"""
    global _service_discovery_instance
    if _service_discovery_instance is None:
        _service_discovery_instance = ServiceDiscovery(get_service_registry())
    return _service_discovery_instance

def get_load_balancer() -> ServiceLoadBalancer:
    """Get or create the load balancer singleton"""
    global _load_balancer_instance
    if _load_balancer_instance is None:
        _load_balancer_instance = ServiceLoadBalancer(get_service_registry())
    return _load_balancer_instance

def init_registry():
    """Initialize all registry components explicitly"""
    reg = get_service_registry()
    disc = get_service_discovery()
    lb = get_load_balancer()
    logger.info("✅ Service Registry and Discovery initialized")
    return reg, disc, lb

# Backward-compatible aliases
service_registry = None
service_discovery = None
load_balancer = None

logger.info("✅ Service Registry and Discovery module loaded (call init_registry() to start)")
