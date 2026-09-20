"""
Rust Service Bridge — HTTP client for Rust services.

Talks to a running Rust service over HTTP (``GET /health``, ``POST /invoke``)
and launches the compiled binary best-effort when the service is not already
reachable.  Services started externally are supported too.
"""

import logging
import subprocess
import time
from dataclasses import dataclass
from typing import Any, Dict, Optional

logger = logging.getLogger(__name__)


@dataclass
class RustServiceConfig:
    """Rust Service Configuration"""
    service_id: str
    service_name: str
    version: str
    host: str
    port: int
    binary_path: str  # Path to compiled Rust binary
    invoke_timeout: float = 30.0

    def to_dict(self) -> Dict:
        return {
            'service_id': self.service_id,
            'name': self.service_name,
            'language': 'rust',
            'version': self.version,
            'host': self.host,
            'port': self.port,
            'metadata': {
                'binary_path': self.binary_path,
            }
        }


class RustServiceBridge:
    """Bridge for communicating with Rust services over HTTP"""

    def __init__(self, config: RustServiceConfig):
        self.config = config
        self.process: Optional[subprocess.Popen] = None
        self.is_running = False

    def _base_url(self) -> str:
        return f"http://{self.config.host}:{self.config.port}"

    def health(self) -> Optional[Dict]:
        """Probe the Rust service health endpoint.  None if unreachable."""
        import requests
        try:
            resp = requests.get(f"{self._base_url()}/health", timeout=3)
            resp.raise_for_status()
            return resp.json()
        except Exception as exc:
            logger.debug("Rust service health check failed: %s", exc)
            return None

    def start(self) -> bool:
        """Start Rust service — attach if already reachable, otherwise launch
        the binary best-effort."""
        try:
            if self.health() is not None:
                self.is_running = True
                logger.info("Rust service already running: %s on %s:%s",
                            self.config.service_name, self.config.host, self.config.port)
                return True

            if self.config.binary_path:
                self.process = subprocess.Popen(
                    [self.config.binary_path],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
                for _ in range(10):
                    time.sleep(0.5)
                    if self.health() is not None:
                        self.is_running = True
                        logger.info("Rust service started: %s on %s:%s",
                                    self.config.service_name, self.config.host, self.config.port)
                        return True
                logger.warning("Rust service did not become reachable after launch: %s",
                               self.config.service_name)
            else:
                logger.info("No binary_path configured for %s — assuming externally managed service.",
                            self.config.service_name)
                self.is_running = True
            return self.is_running
        except Exception as exc:
            logger.error("Failed to start Rust service: %s", exc)
            return False

    def stop(self) -> bool:
        """Stop Rust service process if we launched it."""
        try:
            if self.process is not None and self.process.poll() is None:
                self.process.terminate()
                try:
                    self.process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.process.kill()
            self.process = None
            self.is_running = False
            logger.info("Rust service stopped: %s", self.config.service_name)
            return True
        except Exception as exc:
            logger.error("Failed to stop Rust service: %s", exc)
            return False

    def call_method(self, method_name: str, parameters: Dict[str, Any] = None) -> Optional[Dict]:
        """Call a method on the Rust service via POST /invoke."""
        import requests
        payload = {"method": method_name, "parameters": parameters or {}}
        try:
            resp = requests.post(f"{self._base_url()}/invoke", json=payload,
                                 timeout=self.config.invoke_timeout)
            resp.raise_for_status()
            data = resp.json()
            logger.info("Called Rust method: %s", method_name)
            return {
                'success': True,
                'method': method_name,
                'parameters': parameters or {},
                'result': data.get('result', data),
            }
        except Exception as exc:
            logger.warning("Failed to call Rust method '%s': %s", method_name, exc)
            return {
                'success': False,
                'method': method_name,
                'parameters': parameters or {},
                'error': str(exc),
            }

    def get_status(self) -> Dict[str, Any]:
        """Get Rust service status with a live health probe."""
        healthy = self.health() is not None
        return {
            'service_id': self.config.service_id,
            'name': self.config.service_name,
            'language': 'rust',
            'status': 'running' if healthy or self.is_running else 'stopped',
            'host': self.config.host,
            'port': self.config.port,
            'reachable': healthy,
        }


# Rust Service Template Example
RUST_SERVICE_TEMPLATE = '''
// Aimran Lab Rust Service Template
// This service integrates with the multi-language gateway

use std::env;
use tokio::net::TcpListener;
use tokio::task;

#[tokio::main]
async fn main() {
    let service_id = "rust-service";
    let port = 50052;

    println!("[Rust] Service {} starting on port {}", service_id, port);

    // Start gRPC server
    let addr = format!("127.0.0.1:{}", port).parse().unwrap();

    // TODO: Implement gRPC server using tonic
    // let (service, channel) = channel::create(addr);

    register_with_gateway(service_id, port).await;

    println!("[Rust] Service {} registered with gateway", service_id);
}

async fn register_with_gateway(service_id: &str, port: u16) {
    // TODO: Implement gRPC registration with ServiceRegistry
    println!("[Rust] Registered with gateway");
}

pub fn process_data(input: &str) -> String {
    println!("[Rust] Processing: {}", input);
    format!("Processed by Rust: {}", input)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_process_data() {
        let result = process_data("test");
        assert!(result.contains("test"));
    }
}
'''

logger.info("Rust Service Bridge initialized")