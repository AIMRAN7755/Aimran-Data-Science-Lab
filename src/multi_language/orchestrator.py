"""
Multi-Language Service Factory and Orchestrator
Factory for creating and managing service bridges for Python, C#, and Rust
"""

import logging
from typing import Dict, Any, Optional, List
from enum import Enum

from src.multi_language.csharp_bridge import CSharpServiceBridge, CSharpServiceConfig
from src.multi_language.rust_bridge import RustServiceBridge, RustServiceConfig

logger = logging.getLogger(__name__)


class ServiceLanguage(Enum):
    """Supported service languages"""
    PYTHON = "python"
    CSHARP = "csharp"
    RUST = "rust"
    GO = "go"
    JAVA = "java"


class ServiceBridgeFactory:
    """Factory for creating language-specific service bridges"""

    def __init__(self):
        self.bridges: Dict[str, Any] = {}

    def create_bridge(self, language: str, config: Dict[str, Any]) -> Optional[Any]:
        """Create a service bridge for specified language"""
        try:
            if language == ServiceLanguage.CSHARP.value:
                return self._create_csharp_bridge(config)
            elif language == ServiceLanguage.RUST.value:
                return self._create_rust_bridge(config)
            elif language == ServiceLanguage.PYTHON.value:
                return self._create_python_bridge(config)
            else:
                logger.warning(f"⚠️ Unsupported language: {language}")
                return None
        except Exception as e:
            logger.error(f"❌ Failed to create bridge: {str(e)}")
            return None

    def _create_csharp_bridge(self, config: Dict[str, Any]) -> CSharpServiceBridge:
        """Create C# service bridge"""
        csharp_config = CSharpServiceConfig(
            service_id=config.get('service_id'),
            service_name=config.get('name'),
            version=config.get('version', '1.0.0'),
            host=config.get('host', 'localhost'),
            port=config.get('port', 8000),
            dll_path=config.get('dll_path', ''),
            assembly_name=config.get('assembly_name', ''),
        )
        bridge = CSharpServiceBridge(csharp_config)
        self.bridges[config.get('service_id')] = bridge
        return bridge

    def _create_rust_bridge(self, config: Dict[str, Any]) -> RustServiceBridge:
        """Create Rust service bridge"""
        rust_config = RustServiceConfig(
            service_id=config.get('service_id'),
            service_name=config.get('name'),
            version=config.get('version', '1.0.0'),
            host=config.get('host', 'localhost'),
            port=config.get('port', 8000),
            binary_path=config.get('binary_path', ''),
        )
        bridge = RustServiceBridge(rust_config)
        self.bridges[config.get('service_id')] = bridge
        return bridge

    def _create_python_bridge(self, config: Dict[str, Any]) -> Dict[str, Any]:
        """Create Python service bridge"""
        # Python services don't need a bridge as they're already in the same process
        bridge = {
            'type': 'python',
            'service_id': config.get('service_id'),
            'service_name': config.get('name'),
            'config': config,
        }
        self.bridges[config.get('service_id')] = bridge
        return bridge

    def get_bridge(self, service_id: str) -> Optional[Any]:
        """Get bridge by service ID"""
        return self.bridges.get(service_id)

    def list_bridges(self) -> Dict[str, Any]:
        """List all active bridges"""
        return self.bridges.copy()


class MultiLanguageOrchestrator:
    """Orchestrates multi-language services"""

    def __init__(self):
        self.factory = ServiceBridgeFactory()
        self.services: Dict[str, Dict[str, Any]] = {}

    def register_service(self, config: Dict[str, Any]) -> bool:
        """Register a new service"""
        try:
            service_id = config.get('service_id')
            language = config.get('language')

            # Create bridge
            bridge = self.factory.create_bridge(language, config)
            if not bridge:
                return False

            # Store service info
            self.services[service_id] = {
                'config': config,
                'bridge': bridge,
                'language': language,
                'status': 'registered',
            }

            logger.info(f"✅ Registered {language} service: {config.get('name')}")
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

            bridge = service.get('bridge')
            if bridge and hasattr(bridge, 'start'):
                success = bridge.start()
                if success:
                    service['status'] = 'running'
                    return True
            else:
                # Python service or service without start method
                service['status'] = 'running'
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

            bridge = service.get('bridge')
            if bridge and hasattr(bridge, 'stop'):
                success = bridge.stop()
                if success:
                    service['status'] = 'stopped'
                    return True
            else:
                service['status'] = 'stopped'
                return True
        except Exception as e:
            logger.error(f"❌ Failed to stop service: {str(e)}")
            return False

    def call_service_method(self, service_id: str, method_name: str, 
                          parameters: Dict[str, Any] = None) -> Optional[Dict]:
        """Call a method on a service"""
        try:
            service = self.services.get(service_id)
            if not service:
                logger.error(f"❌ Service not found: {service_id}")
                return None

            bridge = service.get('bridge')
            if bridge and hasattr(bridge, 'call_method'):
                return bridge.call_method(method_name, parameters)
            else:
                logger.warning(f"⚠️ Service {service_id} does not support method calls")
                return None
        except Exception as e:
            logger.error(f"❌ Failed to call method: {str(e)}")
            return None

    def get_service_status(self, service_id: str) -> Dict[str, Any]:
        """Get service status"""
        try:
            service = self.services.get(service_id)
            if not service:
                return {}

            bridge = service.get('bridge')
            if bridge and hasattr(bridge, 'get_status'):
                return bridge.get_status()
            else:
                return {
                    'service_id': service_id,
                    'status': service.get('status'),
                    'language': service.get('language'),
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

    def list_services(self) -> List[Dict]:
        """List all services"""
        return [
            {
                'service_id': service_id,
                'name': service.get('config', {}).get('name'),
                'language': service.get('language'),
                'status': service.get('status'),
            }
            for service_id, service in self.services.items()
        ]


# Global orchestrator instance (lazy init — call init_orchestrator() explicitly)
_orchestrator_instance = None
_bridge_factory_instance = None

def get_orchestrator() -> MultiLanguageOrchestrator:
    """Get or create the orchestrator singleton"""
    global _orchestrator_instance
    if _orchestrator_instance is None:
        _orchestrator_instance = MultiLanguageOrchestrator()
    return _orchestrator_instance

def get_bridge_factory() -> ServiceBridgeFactory:
    """Get or create the bridge factory singleton"""
    global _bridge_factory_instance
    if _bridge_factory_instance is None:
        _bridge_factory_instance = ServiceBridgeFactory()
    return _bridge_factory_instance

def init_orchestrator():
    """Initialize all orchestrator components explicitly"""
    orch = get_orchestrator()
    factory = get_bridge_factory()
    logger.info("✅ Multi-Language Service Factory and Orchestrator initialized")
    return orch, factory

# Backward-compatible aliases
orchestrator = None
bridge_factory = None

logger.info("✅ Multi-Language Service Factory and Orchestrator module loaded (call init_orchestrator() to start)")
