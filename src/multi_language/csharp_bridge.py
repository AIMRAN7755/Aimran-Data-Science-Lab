"""
C# Service Bridge — HTTP client for C# services.

Talks to a running C# service over HTTP (``GET /health``, ``POST /invoke``)
and launches the compiled DLL best-effort when the service is not already
reachable.  Services started externally are supported too.
"""

import logging
import shutil
import subprocess
import time
from dataclasses import dataclass
from typing import Any, Dict, Optional

logger = logging.getLogger(__name__)


@dataclass
class CSharpServiceConfig:
    """C# Service Configuration"""
    service_id: str
    service_name: str
    version: str
    host: str
    port: int
    dll_path: str  # Path to compiled C# DLL
    assembly_name: str
    class_name: str = "Service"
    invoke_timeout: float = 30.0

    def to_dict(self) -> Dict:
        return {
            'service_id': self.service_id,
            'name': self.service_name,
            'language': 'csharp',
            'version': self.version,
            'host': self.host,
            'port': self.port,
            'metadata': {
                'dll_path': self.dll_path,
                'assembly_name': self.assembly_name,
                'class_name': self.class_name,
            }
        }


class CSharpServiceBridge:
    """Bridge for communicating with C# services over HTTP"""

    def __init__(self, config: CSharpServiceConfig):
        self.config = config
        self.process: Optional[subprocess.Popen] = None
        self.is_running = False

    def _base_url(self) -> str:
        return f"http://{self.config.host}:{self.config.port}"

    def health(self) -> Optional[Dict]:
        """Probe the C# service health endpoint.  None if unreachable."""
        import requests
        try:
            resp = requests.get(f"{self._base_url()}/health", timeout=3)
            resp.raise_for_status()
            return resp.json()
        except Exception as exc:
            logger.debug("C# service health check failed: %s", exc)
            return None

    def start(self) -> bool:
        """Start C# service — attach if already reachable, otherwise launch
        the DLL via `dotnet` best-effort."""
        try:
            if self.health() is not None:
                self.is_running = True
                logger.info("C# service already running: %s on %s:%s",
                            self.config.service_name, self.config.host, self.config.port)
                return True

            if self.config.dll_path:
                dotnet = shutil.which("dotnet")
                if dotnet:
                    self.process = subprocess.Popen(
                        [dotnet, self.config.dll_path],
                        stdout=subprocess.DEVNULL,
                        stderr=subprocess.DEVNULL,
                    )
                    for _ in range(10):
                        time.sleep(0.5)
                        if self.health() is not None:
                            self.is_running = True
                            logger.info("C# service started: %s on %s:%s",
                                        self.config.service_name, self.config.host, self.config.port)
                            return True
                    logger.warning("C# service did not become reachable after launch: %s",
                                   self.config.service_name)
                else:
                    logger.warning("dotnet CLI not found; cannot launch C# service %s",
                                   self.config.service_name)
            else:
                logger.info("No dll_path configured for %s — assuming externally managed service.",
                            self.config.service_name)
                self.is_running = True
            return self.is_running
        except Exception as exc:
            logger.error("Failed to start C# service: %s", exc)
            return False

    def stop(self) -> bool:
        """Stop C# service process if we launched it."""
        try:
            if self.process is not None and self.process.poll() is None:
                self.process.terminate()
                try:
                    self.process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.process.kill()
            self.process = None
            self.is_running = False
            logger.info("C# service stopped: %s", self.config.service_name)
            return True
        except Exception as exc:
            logger.error("Failed to stop C# service: %s", exc)
            return False

    def call_method(self, method_name: str, parameters: Dict[str, Any] = None) -> Optional[Dict]:
        """Call a method on the C# service via POST /invoke."""
        import requests
        payload = {"method": method_name, "parameters": parameters or {}}
        try:
            resp = requests.post(f"{self._base_url()}/invoke", json=payload,
                                 timeout=self.config.invoke_timeout)
            resp.raise_for_status()
            data = resp.json()
            logger.info("Called C# method: %s", method_name)
            return {
                'success': True,
                'method': method_name,
                'parameters': parameters or {},
                'result': data.get('result', data),
            }
        except Exception as exc:
            logger.warning("Failed to call C# method '%s': %s", method_name, exc)
            return {
                'success': False,
                'method': method_name,
                'parameters': parameters or {},
                'error': str(exc),
            }

    def get_status(self) -> Dict[str, Any]:
        """Get C# service status with a live health probe."""
        healthy = self.health() is not None
        return {
            'service_id': self.config.service_id,
            'name': self.config.service_name,
            'language': 'csharp',
            'status': 'running' if healthy or self.is_running else 'stopped',
            'host': self.config.host,
            'port': self.config.port,
            'reachable': healthy,
        }


# C# Service Template Example
CSHARP_SERVICE_TEMPLATE = '''
// Aimran Lab C# Service Template
// This service integrates with the multi-language gateway

using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcService;

namespace AimranLabService
{
    public class Service
    {
        private readonly string _serviceId;
        private readonly int _port;

        public Service(string serviceId, int port)
        {
            _serviceId = serviceId;
            _port = port;
        }

        public void Start()
        {
            Console.WriteLine($"[C#] Service {_serviceId} starting on port {_port}");
            // Start gRPC server
            // Register with service registry
            RegisterWithGateway();
        }

        public void Stop()
        {
            Console.WriteLine($"[C#] Service {_serviceId} stopping");
        }

        private void RegisterWithGateway()
        {
            // TODO: Implement gRPC registration with ServiceRegistry
            Console.WriteLine("[C#] Registered with gateway");
        }

        public string ProcessData(string input)
        {
            Console.WriteLine($"[C#] Processing: {input}");
            return $"Processed by C#: {input}";
        }

        public Task<string> ProcessDataAsync(string input)
        {
            return Task.FromResult(ProcessData(input));
        }

        public static void Main(string[] args)
        {
            var service = new Service("csharp-service", 50051);
            service.Start();
        }
    }
}
'''

logger.info("C# Service Bridge initialized")