"""
Unified Management API for Aimran Data Science Lab
Provides REST API for service control, monitoring, and real-time updates
"""

from fastapi import FastAPI, HTTPException, WebSocket, Query
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import logging
import json
from typing import Optional, List, Dict, Any
from datetime import datetime
import asyncio

from src.multi_language.service_manager import (
    ServiceManager, HealthMonitor, ServiceConfig, create_service_manager
)
from src.multi_language.service_registry import ServiceRegistry, service_discovery, load_balancer
from src.multi_language.ipc_broker import get_event_bus, get_ipc_broker
from src.multi_language.gateway import get_gateway

logger = logging.getLogger(__name__)

# Lazy singletons — initialized on first use so a missing Redis
# server does not crash the module at import time.
_event_bus = None
_ipc_broker = None
_gateway = None


def _get_event_bus():
    global _event_bus
    if _event_bus is None:
        try:
            _event_bus = get_event_bus()
        except Exception as exc:
            logger.warning(f"event_bus unavailable: {exc}")
    return _event_bus


def _get_ipc_broker():
    global _ipc_broker
    if _ipc_broker is None:
        try:
            _ipc_broker = get_ipc_broker()
        except Exception as exc:
            logger.warning(f"ipc_broker unavailable: {exc}")
    return _ipc_broker


def _get_gateway():
    global _gateway
    if _gateway is None:
        try:
            _gateway = get_gateway()
        except Exception as exc:
            logger.warning(f"gateway unavailable: {exc}")
    return _gateway

# Create FastAPI app for management
management_app = FastAPI(
    title="Aimran Data Science Lab - Management API",
    description="Unified control plane for multi-language services",
    version="1.0.0"
)

# Add CORS
management_app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize service manager
service_manager = create_service_manager()
health_monitor = HealthMonitor(service_manager, _get_event_bus())

# WebSocket connections
active_connections: List[WebSocket] = []


# ============================================================
# Service Management Endpoints
# ============================================================

@management_app.get("/api/services", tags=["Services"])
async def list_services(language: Optional[str] = None, 
                       capability: Optional[str] = None):
    """List all registered services"""
    try:
        if language:
            services = ServiceRegistry().list_by_language(language)
        elif capability:
            services = service_discovery.discover_by_capability(capability)
        else:
            services = service_manager.registry.list_all()

        return {
            "success": True,
            "count": len(services),
            "services": services
        }
    except Exception as e:
        logger.error(f"❌ Failed to list services: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/register", tags=["Services"])
async def register_service(service_data: Dict[str, Any]):
    """Register a new service"""
    try:
        service_config = ServiceConfig(
            service_id=service_data.get('service_id'),
            name=service_data.get('name'),
            language=service_data.get('language'),
            version=service_data.get('version', '1.0.0'),
            host=service_data.get('host', 'localhost'),
            port=service_data.get('port', 8000),
        )

        success = service_manager.register_service(service_config)

        if success:
            return {
                "success": True,
                "message": f"Service {service_config.name} registered",
                "service_id": service_config.service_id
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to register service")
    except Exception as e:
        logger.error(f"❌ Registration failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.get("/api/services/{service_id}", tags=["Services"])
async def get_service(service_id: str):
    """Get service details"""
    try:
        status = service_manager.get_service_status(service_id)
        if not status:
            raise HTTPException(status_code=404, detail="Service not found")

        return {
            "success": True,
            "service": status
        }
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"❌ Failed to get service: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/{service_id}/start", tags=["Control"])
async def start_service(service_id: str):
    """Start a service"""
    try:
        success = service_manager.start_service(service_id)

        if success:
            return {
                "success": True,
                "message": f"Service {service_id} started",
                "timestamp": datetime.now().isoformat()
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to start service")
    except Exception as e:
        logger.error(f"❌ Start failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/{service_id}/stop", tags=["Control"])
async def stop_service(service_id: str):
    """Stop a service"""
    try:
        success = service_manager.stop_service(service_id)

        if success:
            return {
                "success": True,
                "message": f"Service {service_id} stopped",
                "timestamp": datetime.now().isoformat()
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to stop service")
    except Exception as e:
        logger.error(f"❌ Stop failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/{service_id}/restart", tags=["Control"])
async def restart_service(service_id: str):
    """Restart a service"""
    try:
        success = service_manager.restart_service(service_id)

        if success:
            return {
                "success": True,
                "message": f"Service {service_id} restarted",
                "timestamp": datetime.now().isoformat()
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to restart service")
    except Exception as e:
        logger.error(f"❌ Restart failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/{service_id}/enable", tags=["Control"])
async def enable_service(service_id: str):
    """Enable a service"""
    try:
        success = service_manager.enable_service(service_id)

        if success:
            return {
                "success": True,
                "message": f"Service {service_id} enabled",
                "timestamp": datetime.now().isoformat()
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to enable service")
    except Exception as e:
        logger.error(f"❌ Enable failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/services/{service_id}/disable", tags=["Control"])
async def disable_service(service_id: str):
    """Disable a service"""
    try:
        success = service_manager.disable_service(service_id)

        if success:
            return {
                "success": True,
                "message": f"Service {service_id} disabled",
                "timestamp": datetime.now().isoformat()
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to disable service")
    except Exception as e:
        logger.error(f"❌ Disable failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# Monitoring and Health Endpoints
# ============================================================

@management_app.get("/api/health", tags=["Health"])
async def get_health():
    """Get overall system health"""
    try:
        overview = service_manager.get_system_overview()

        return {
            "success": True,
            "health": overview
        }
    except Exception as e:
        logger.error(f"❌ Health check failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.get("/api/services/{service_id}/health", tags=["Health"])
async def get_service_health(service_id: str):
    """Get service health status"""
    try:
        health = health_monitor.check_service_health(service_id)

        return {
            "success": True,
            "health": health
        }
    except Exception as e:
        logger.error(f"❌ Health check failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.get("/api/services/{service_id}/health/history", tags=["Health"])
async def get_health_history(service_id: str, limit: int = Query(100, ge=1, le=1000)):
    """Get service health history"""
    try:
        history = health_monitor.get_health_history(service_id, limit)

        return {
            "success": True,
            "service_id": service_id,
            "count": len(history),
            "history": history
        }
    except Exception as e:
        logger.error(f"❌ Failed to get history: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/health/check-all", tags=["Health"])
async def check_all_services_health():
    """Check health of all services"""
    try:
        results = health_monitor.check_all_services_health()

        return {
            "success": True,
            "timestamp": datetime.now().isoformat(),
            "results": results
        }
    except Exception as e:
        logger.error(f"❌ Health check failed: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# System Overview Endpoints
# ============================================================

@management_app.get("/api/system/overview", tags=["System"])
async def get_system_overview():
    """Get complete system overview"""
    try:
        overview = service_manager.get_system_overview()

        return {
            "success": True,
            "overview": overview
        }
    except Exception as e:
        logger.error(f"❌ Failed to get overview: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.get("/api/system/status", tags=["System"])
async def get_system_status():
    """Get system status summary"""
    try:
        overview = service_manager.get_system_overview()

        return {
            "success": True,
            "status": "operational",
            "timestamp": datetime.now().isoformat(),
            "services": {
                "total": overview.get('total_services', 0),
                "running": overview.get('running_services', 0),
                "stopped": overview.get('stopped_services', 0),
            },
            "system": overview.get('system_health', {})
        }
    except Exception as e:
        logger.error(f"❌ Failed to get status: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# Topics and Messaging Endpoints
# ============================================================

@management_app.get("/api/topics", tags=["Messaging"])
async def list_topics():
    """List all messaging topics"""
    try:
        broker = _get_ipc_broker()
        if broker is None:
            raise HTTPException(status_code=503, detail="IPC broker unavailable")
        topics = broker.list_topics()

        return {
            "success": True,
            "count": len(topics),
            "topics": topics
        }
    except Exception as e:
        logger.error(f"❌ Failed to list topics: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.get("/api/topics/{topic_name}", tags=["Messaging"])
async def get_topic_stats(topic_name: str):
    """Get topic statistics"""
    try:
        broker = _get_ipc_broker()
        if broker is None:
            raise HTTPException(status_code=503, detail="IPC broker unavailable")
        stats = broker.get_topic_stats(topic_name)

        if not stats:
            raise HTTPException(status_code=404, detail="Topic not found")

        return {
            "success": True,
            "stats": stats
        }
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"❌ Failed to get topic stats: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@management_app.post("/api/topics", tags=["Messaging"])
async def create_topic(topic_data: Dict[str, Any]):
    """Create a new messaging topic"""
    try:
        topic_name = topic_data.get('name')
        description = topic_data.get('description', '')
        retention = topic_data.get('retention', 3600)

        broker = _get_ipc_broker()
        if broker is None:
            raise HTTPException(status_code=503, detail="IPC broker unavailable")
        success = broker.create_topic(topic_name, description, retention)

        if success:
            return {
                "success": True,
                "message": f"Topic {topic_name} created",
                "topic": topic_name
            }
        else:
            raise HTTPException(status_code=400, detail="Failed to create topic")
    except Exception as e:
        logger.error(f"❌ Failed to create topic: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# WebSocket for Real-time Updates
# ============================================================

@management_app.websocket("/ws/metrics")
async def websocket_metrics(websocket: WebSocket):
    """WebSocket endpoint for real-time metrics"""
    await websocket.accept()
    active_connections.append(websocket)

    try:
        while True:
            # Send system overview every 2 seconds
            overview = service_manager.get_system_overview()
            await websocket.send_json({
                "type": "metrics_update",
                "data": overview
            })
            await asyncio.sleep(2)
    except Exception as e:
        logger.error(f"❌ WebSocket error: {str(e)}")
    finally:
        active_connections.remove(websocket)


@management_app.websocket("/ws/events")
async def websocket_events(websocket: WebSocket):
    """WebSocket endpoint for real-time events"""
    await websocket.accept()
    active_connections.append(websocket)

    # Subscribe to all system events
    def send_event(message):
        # Send to all connected clients
        asyncio.create_task(broadcast_event(message))

    # For this simplified implementation, just keep connection open
    try:
        while True:
            data = await websocket.receive_text()
            # Echo back for now
            await websocket.send_json({
                "type": "event_received",
                "data": data
            })
    except Exception as e:
        logger.error(f"❌ WebSocket error: {str(e)}")
    finally:
        active_connections.remove(websocket)


async def broadcast_event(event_data: Dict[str, Any]):
    """Broadcast event to all connected WebSocket clients"""
    for connection in active_connections:
        try:
            await connection.send_json(event_data)
        except Exception as e:
            logger.error(f"❌ Failed to send event: {str(e)}")


# ============================================================
# Root Endpoints
# ============================================================

@management_app.get("/", tags=["Root"])
async def root():
    """Root endpoint"""
    return {
        "service": "Aimran Data Science Lab - Management API",
        "version": "1.0.0",
        "status": "operational",
        "docs": "/docs",
        "redoc": "/redoc"
    }


@management_app.get("/api", tags=["Root"])
async def api_root():
    """API root endpoint"""
    return {
        "service": "Aimran Data Science Lab - Management API",
        "version": "1.0.0",
        "endpoints": {
            "services": "/api/services",
            "health": "/api/health",
            "system": "/api/system",
            "topics": "/api/topics",
        },
        "websockets": {
            "metrics": "/ws/metrics",
            "events": "/ws/events"
        }
    }


logger.info("✅ Management API initialized")
