using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AimranDataScienceLab.ServiceTemplates.Base
{
    /// <summary>
    /// Core interface that all services must implement.
    /// Provides lifecycle management and health monitoring across all deployment scenarios.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Gets the unique identifier for this service.
        /// </summary>
        string ServiceId { get; }

        /// <summary>
        /// Gets the service name (e.g., "DataAnalysisService", "ModelRegistryService").
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Gets the current version of the service.
        /// </summary>
        Version ServiceVersion { get; }

        /// <summary>
        /// Gets the current service health status.
        /// </summary>
        ServiceHealthStatus HealthStatus { get; }

        /// <summary>
        /// Initializes the service with the given configuration.
        /// Called during startup before the service begins processing requests.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the service. Called after initialization.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the service gracefully.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks the health status of the service.
        /// Used by orchestrators (Kubernetes, Docker Swarm) for liveness/readiness probes.
        /// </summary>
        Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets service metadata including configuration, dependencies, and capabilities.
        /// </summary>
        ServiceMetadata GetMetadata();

        /// <summary>
        /// Gets detailed diagnostics information for troubleshooting.
        /// </summary>
        Task<ServiceDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service health status information.
    /// </summary>
    public class ServiceHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } // "Healthy", "Degraded", "Unhealthy"
        public string Message { get; set; }
        public DateTime LastChecked { get; set; }
        public Dictionary<string, string> Details { get; set; } = new();
    }

    /// <summary>
    /// Service metadata for discovery and documentation.
    /// </summary>
    public class ServiceMetadata
    {
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public Version ServiceVersion { get; set; }
        public string Description { get; set; }
        public string[] Dependencies { get; set; }
        public string[] Capabilities { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public ServiceConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Service configuration metadata.
    /// </summary>
    public class ServiceConfiguration
    {
        public int MaxConcurrentRequests { get; set; } = 100;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);
        public Dictionary<string, string> Environment { get; set; } = new();
        public Dictionary<string, string> Features { get; set; } = new();
    }

    /// <summary>
    /// Detailed diagnostics for service troubleshooting.
    /// </summary>
    public class ServiceDiagnostics
    {
        public DateTime Timestamp { get; set; }
        public ServiceHealthStatus Health { get; set; }
        public long UptimeSeconds { get; set; }
        public int ActiveRequests { get; set; }
        public long TotalRequests { get; set; }
        public long ProcessedRequests { get; set; }
        public long FailedRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public long MemoryUsageMb { get; set; }
        public long CpuUsagePercent { get; set; }
        public Dictionary<string, string> DependencyStatus { get; set; } = new();
        public string[] RecentErrors { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }
}
