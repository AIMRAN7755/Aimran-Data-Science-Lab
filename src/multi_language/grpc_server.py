"""
gRPC server implementing ``proto/services.proto`` (package ``aimran.services``).

Serves three services:

- ``ServiceRegistry`` — register / deregister / discover / control services
- ``ServiceBroker``    — pub/sub messaging + cross-service function execution
- ``ServiceHeartbeat`` — ping + health checks

The registry and broker run in-memory so the server works without Redis;
``ExecuteFunction`` dispatches through the :class:`MultiLanguageOrchestrator`.

Requires ``grpcio``.  A clear ``ImportError`` is raised when grpcio or the
generated stubs (``grpc/services_pb2*.py``) are missing.
"""

from __future__ import annotations

import json
import logging
import queue
import sys
import threading
import time
import uuid
from concurrent import futures
from pathlib import Path
from typing import Dict, Iterator, List, Optional

import grpc

from src.multi_language.orchestrator import MultiLanguageOrchestrator

logger = logging.getLogger(__name__)

_GRPC_STUBS_DIR = Path(__file__).parent / "grpc"
if _GRPC_STUBS_DIR.is_dir():
    sys.path.insert(0, str(_GRPC_STUBS_DIR))

try:
    import services_pb2 as pb2
    import services_pb2_grpc as pb2_grpc
except ImportError as exc:  # pragma: no cover
    raise ImportError(
        "gRPC stubs not found. Regenerate them with:\n"
        "  python -m grpc_tools.protoc -I proto "
        "--python_out=src/multi_language/grpc "
        "--grpc_python_out=src/multi_language/grpc proto/services.proto"
    ) from exc


# ── ServiceRegistry ───────────────────────────────────────────────────────────

class ServiceRegistryServicer(pb2_grpc.ServiceRegistryServicer):
    """In-memory service registry with status, metrics and log tracking."""

    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._services: Dict[str, pb2.ServiceInfo] = {}
        self._start_times: Dict[str, float] = {}
        self._request_counts: Dict[str, int] = {}
        self._logs: List[pb2.LogEntry] = []
        self._max_logs = 500

    def _record_log(self, level: str, message: str) -> None:
        entry = pb2.LogEntry(
            timestamp=int(time.time() * 1000),
            level=level,
            message=message,
        )
        with self._lock:
            self._logs.append(entry)
            if len(self._logs) > self._max_logs:
                self._logs = self._logs[-self._max_logs:]

    def RegisterService(self, request, context) -> pb2.ServiceResponse:
        with self._lock:
            self._services[request.service_id] = request
            self._start_times.setdefault(request.service_id, time.time())
            self._request_counts[request.service_id] = self._request_counts.get(request.service_id, 0) + 1
        self._record_log("INFO", f"Service registered: {request.service_id}")
        logger.info("gRPC: registered service %s (%s)", request.service_id, request.language)
        return pb2.ServiceResponse(success=True, message=f"Service '{request.service_id}' registered")

    def DeregisterService(self, request, context) -> pb2.ServiceResponse:
        with self._lock:
            removed = self._services.pop(request.service_id, None) is not None
            self._start_times.pop(request.service_id, None)
            if removed:
                self._record_log("INFO", f"Service deregistered: {request.service_id}")
                return pb2.ServiceResponse(success=True, message=f"Service '{request.service_id}' deregistered")
        return pb2.ServiceResponse(success=False, message=f"Service '{request.service_id}' not found",
                                   error_code="NOT_FOUND")

    def GetService(self, request, context) -> pb2.ServiceInfo:
        with self._lock:
            service = self._services.get(request.service_id)
        if service is None:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f"Service '{request.service_id}' not found")
            return pb2.ServiceInfo()
        return service

    def ListServices(self, request, context) -> pb2.ServiceList:
        with self._lock:
            services = list(self._services.values())
        return pb2.ServiceList(services=services, total_count=len(services))

    def GetServiceStatus(self, request, context) -> pb2.ServiceStatus:
        with self._lock:
            service = self._services.get(request.service_id)
            if service is None:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Service '{request.service_id}' not found")
                return pb2.ServiceStatus()
            start = self._start_times.get(request.service_id, time.time())
            count = self._request_counts.get(request.service_id, 0)
            uptime = int(time.time() - start)
        return pb2.ServiceStatus(
            service_id=request.service_id,
            status=service.status or "running",
            request_count=count,
            uptime_seconds=uptime,
        )

    def ControlService(self, request, context) -> pb2.ServiceResponse:
        action = (request.action or "").lower()
        if action not in {"start", "stop", "restart", "reload"}:
            return pb2.ServiceResponse(success=False, message=f"Unknown action '{request.action}'",
                                       error_code="INVALID_ACTION")
        with self._lock:
            service = self._services.get(request.service_id)
            if service is None:
                return pb2.ServiceResponse(success=False, message=f"Service '{request.service_id}' not found",
                                           error_code="NOT_FOUND")
            service.status = "running" if action == "start" else "stopped"
            if action == "restart":
                service.status = "running"
            self._record_log("INFO", f"Service {request.service_id} {action} requested")
        logger.info("gRPC: control %s -> %s", request.service_id, action)
        return pb2.ServiceResponse(success=True, message=f"Service '{request.service_id}' {action} requested")

    def StreamMetrics(self, request, context) -> Iterator[pb2.ServiceMetrics]:
        while context.is_active():
            with self._lock:
                service = self._services.get(request.service_id)
                count = self._request_counts.get(request.service_id, 0)
            if service is not None:
                yield pb2.ServiceMetrics(
                    service_id=request.service_id,
                    timestamp=int(time.time() * 1000),
                    requests_per_second=count,
                )
            time.sleep(1)

    def GetLogs(self, request, context) -> pb2.LogResponse:
        with self._lock:
            entries = [e for e in self._logs if (not request.level or e.level == request.level)
                       and (request.since_timestamp == 0 or e.timestamp >= request.since_timestamp)]
            if request.limit > 0:
                entries = entries[-request.limit:]
        return pb2.LogResponse(entries=entries)


# ── ServiceBroker ─────────────────────────────────────────────────────────────

class ServiceBrokerServicer(pb2_grpc.ServiceBrokerServicer):
    """In-memory pub/sub broker + cross-service function execution."""

    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._subscribers: Dict[str, List[queue.Queue]] = {}
        self._orchestrator = MultiLanguageOrchestrator()

    def _deliver(self, topic: str, message: pb2.Message) -> bool:
        with self._lock:
            subscribers = list(self._subscribers.get(topic, []))
        delivered = False
        for sub in subscribers:
            try:
                sub.put_nowait(message)
                delivered = True
            except queue.Full:
                logger.warning("gRPC: subscriber queue full for topic '%s'", topic)
        return delivered

    def SendMessage(self, request, context) -> pb2.MessageResponse:
        delivered = self._deliver(request.topic, request)
        return pb2.MessageResponse(
            message_id=request.message_id or str(uuid.uuid4()),
            delivered=delivered,
            status="delivered" if delivered else "no_subscribers",
            processing_time_ms=0,
        )

    def Publish(self, request, context) -> pb2.MessageResponse:
        message = pb2.Message(
            message_id=str(uuid.uuid4()),
            source_service=request.source_service,
            topic=request.topic,
            payload=request.payload,
            timestamp=int(time.time() * 1000),
            priority=request.priority,
        )
        delivered = self._deliver(request.topic, message)
        return pb2.MessageResponse(
            message_id=message.message_id,
            delivered=delivered,
            status="delivered" if delivered else "no_subscribers",
            processing_time_ms=0,
        )

    def Subscribe(self, request, context) -> Iterator[pb2.Message]:
        sub_queue: queue.Queue = queue.Queue(maxsize=100)
        topics = list(request.topics or [])
        with self._lock:
            for topic in topics:
                self._subscribers.setdefault(topic, []).append(sub_queue)
        try:
            while context.is_active():
                try:
                    yield sub_queue.get(timeout=30)
                except queue.Empty:
                    continue
        finally:
            with self._lock:
                for topic in topics:
                    subs = self._subscribers.get(topic, [])
                    if sub_queue in subs:
                        subs.remove(sub_queue)

    def ExecuteFunction(self, request, context) -> pb2.FunctionResult:
        started = time.time()
        call_id = str(uuid.uuid4())
        try:
            args = json.loads(request.arguments) if request.arguments else {}
        except json.JSONDecodeError as exc:
            return pb2.FunctionResult(
                call_id=call_id, success=False,
                error_message=f"Invalid JSON arguments: {exc}",
                execution_time_ms=int((time.time() - started) * 1000),
            )
        try:
            result = self._orchestrator.call_service_method(
                request.target_service, request.function_name, args
            )
        except Exception as exc:
            logger.error("gRPC: ExecuteFunction failed: %s", exc)
            return pb2.FunctionResult(
                call_id=call_id, success=False,
                error_message=str(exc),
                execution_time_ms=int((time.time() - started) * 1000),
            )
        if result is None:
            return pb2.FunctionResult(
                call_id=call_id, success=False,
                error_message=f"Service '{request.target_service}' not found",
                execution_time_ms=int((time.time() - started) * 1000),
            )
        return pb2.FunctionResult(
            call_id=call_id,
            success=bool(result.get("success")),
            result=json.dumps(result.get("result", result), default=str),
            error_message=result.get("error", ""),
            execution_time_ms=int((time.time() - started) * 1000),
        )


# ── ServiceHeartbeat ──────────────────────────────────────────────────────────

class ServiceHeartbeatServicer(pb2_grpc.ServiceHeartbeatServicer):
    """Ping and health-check endpoints."""

    _server_started = time.time()

    def Ping(self, request, context) -> pb2.PingResponse:
        started = time.time()
        latency_ms = int((time.time() - started) * 1000)
        return pb2.PingResponse(
            service_id=request.service_id or "grpc-server",
            timestamp=int(time.time() * 1000),
            latency_ms=latency_ms,
            status="ok",
        )

    def HealthCheck(self, request, context) -> pb2.HealthCheckResponse:
        return pb2.HealthCheckResponse(
            healthy=True,
            status="serving",
            details=f"Aimran gRPC server up for {int(time.time() - self._server_started)}s",
            issues=[],
        )


# ── Server ────────────────────────────────────────────────────────────────────

class GrpcServer:
    """gRPC server exposing the three services from ``proto/services.proto``."""

    def __init__(self, port: int = 50051, max_workers: int = 10) -> None:
        self.port = port
        self.max_workers = max_workers
        self.server = grpc.server(futures.ThreadPoolExecutor(max_workers=max_workers))
        self.registry = ServiceRegistryServicer()
        self.broker = ServiceBrokerServicer()
        self.heartbeat = ServiceHeartbeatServicer()
        pb2_grpc.add_ServiceRegistryServicer_to_server(self.registry, self.server)
        pb2_grpc.add_ServiceBrokerServicer_to_server(self.broker, self.server)
        pb2_grpc.add_ServiceHeartbeatServicer_to_server(self.heartbeat, self.server)

    def start(self) -> None:
        self.server.add_insecure_port(f"[::]:{self.port}")
        self.server.start()
        logger.info("gRPC server listening on port %d", self.port)

    def stop(self, grace: float = 0) -> None:
        self.server.stop(grace)

    def serve(self) -> None:
        """Start and block until terminated."""
        self.start()
        self.server.wait_for_termination()


def main() -> None:
    import argparse
    parser = argparse.ArgumentParser(description="Aimran gRPC server")
    parser.add_argument("--port", type=int, default=50051, help="Listen port (default: 50051)")
    parser.add_argument("--workers", type=int, default=10, help="Thread pool size (default: 10)")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO)
    server = GrpcServer(port=args.port, max_workers=args.workers)
    server.serve()


if __name__ == "__main__":
    main()