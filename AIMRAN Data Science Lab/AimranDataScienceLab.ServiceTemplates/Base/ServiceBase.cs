using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base
{
    /// <summary>
    /// Abstract base class for all services in the AIMRAN ecosystem.
    /// Provides common lifecycle management, health checking, and diagnostics.
    /// </summary>
    public abstract class ServiceBase : IService
    {
        private readonly ILogger<ServiceBase> _logger;
        private readonly Stopwatch _uptime = Stopwatch.StartNew();
        private long _totalRequests;
        private long _processedRequests;
        private long _failedRequests;
        private double _totalResponseTimeMs;
        private readonly Queue<string> _recentErrors = new(capacity: 20);
        private ServiceHealthStatus _cachedHealthStatus;
        private DateTime _lastHealthCheck;
        private readonly object _diagnosticsLock = new();

        public abstract string ServiceId { get; }
        public abstract string ServiceName { get; }
        public abstract Version ServiceVersion { get; }

        public ServiceHealthStatus HealthStatus
        {
            get
            {
                lock (_diagnosticsLock)
                {
                    if (_cachedHealthStatus == null || DateTime.UtcNow - _lastHealthCheck > TimeSpan.FromSeconds(5))
                    {
                        _cachedHealthStatus = UpdateHealthStatus();
                        _lastHealthCheck = DateTime.UtcNow;
                    }
                    return _cachedHealthStatus;
                }
            }
        }

        protected ServiceBase(ILogger<ServiceBase> logger)
        {
            _logger = logger;
            _logger.LogInformation("Service {ServiceName} ({ServiceId}) initializing", ServiceName, ServiceId);
        }

        /// <summary>
        /// Derived classes override this to implement service-specific initialization.
        /// </summary>
        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Derived classes override this to implement service-specific startup logic.
        /// </summary>
        protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Derived classes override this to implement service-specific shutdown logic.
        /// </summary>
        protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Derived classes override this to implement service-specific health checks.
        /// </summary>
        protected virtual Task<ServiceHealthStatus> OnCheckHealthAsync(CancellationToken cancellationToken) => 
            Task.FromResult(new ServiceHealthStatus 
            { 
                IsHealthy = true, 
                Status = "Healthy", 
                Message = "Service is operational",
                LastChecked = DateTime.UtcNow
            });

        public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initializing service {ServiceName} v{Version}", ServiceName, ServiceVersion);
                await OnInitializeAsync(cancellationToken);
                _logger.LogInformation("Service {ServiceName} initialized successfully", ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing service {ServiceName}", ServiceName);
                RecordError($"Initialization failed: {ex.Message}");
                throw;
            }
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting service {ServiceName}", ServiceName);
                await OnStartAsync(cancellationToken);
                _logger.LogInformation("Service {ServiceName} started successfully", ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service {ServiceName}", ServiceName);
                RecordError($"Start failed: {ex.Message}");
                throw;
            }
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Stopping service {ServiceName}", ServiceName);
                await OnStopAsync(cancellationToken);
                _logger.LogInformation("Service {ServiceName} stopped successfully", ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service {ServiceName}", ServiceName);
                RecordError($"Stop failed: {ex.Message}");
                throw;
            }
        }

        public virtual async Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var health = await OnCheckHealthAsync(cancellationToken);
                lock (_diagnosticsLock)
                {
                    _cachedHealthStatus = health;
                    _lastHealthCheck = DateTime.UtcNow;
                }
                return health;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for {ServiceName}", ServiceName);
                RecordError($"Health check failed: {ex.Message}");
                return new ServiceHealthStatus 
                { 
                    IsHealthy = false, 
                    Status = "Unhealthy", 
                    Message = $"Health check failed: {ex.Message}",
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        public virtual ServiceMetadata GetMetadata()
        {
            return new ServiceMetadata
            {
                ServiceId = ServiceId,
                ServiceName = ServiceName,
                ServiceVersion = ServiceVersion,
                Description = GetServiceDescription(),
                Dependencies = GetServiceDependencies(),
                Capabilities = GetServiceCapabilities(),
                Tags = GetServiceTags(),
                Configuration = GetServiceConfiguration()
            };
        }

        public virtual async Task<ServiceDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var health = await CheckHealthAsync(cancellationToken);

            lock (_diagnosticsLock)
            {
                return new ServiceDiagnostics
                {
                    Timestamp = DateTime.UtcNow,
                    Health = health,
                    UptimeSeconds = (long)_uptime.Elapsed.TotalSeconds,
                    ActiveRequests = 0, // Can be overridden by derived classes
                    TotalRequests = _totalRequests,
                    ProcessedRequests = _processedRequests,
                    FailedRequests = _failedRequests,
                    AverageResponseTimeMs = _totalRequests > 0 ? _totalResponseTimeMs / _totalRequests : 0,
                    MemoryUsageMb = GC.GetTotalMemory(false) / (1024 * 1024),
                    CpuUsagePercent = GetCpuUsage(),
                    DependencyStatus = GetDependencyStatus(),
                    RecentErrors = _recentErrors.ToArray(),
                    CustomMetrics = GetCustomMetrics()
                };
            }
        }

        /// <summary>
        /// Records a request completion for diagnostics tracking.
        /// </summary>
        protected void RecordRequestMetrics(long responseTimeMs, bool success = true)
        {
            lock (_diagnosticsLock)
            {
                _totalRequests++;
                if (success)
                    _processedRequests++;
                else
                    _failedRequests++;

                _totalResponseTimeMs += responseTimeMs;
            }
        }

        /// <summary>
        /// Records an error for diagnostics.
        /// </summary>
        protected void RecordError(string errorMessage)
        {
            lock (_diagnosticsLock)
            {
                _recentErrors.Enqueue($"[{DateTime.UtcNow:O}] {errorMessage}");
                if (_recentErrors.Count > 20)
                    _recentErrors.Dequeue();
            }
        }

        /// <summary>
        /// Returns the service description. Override in derived classes.
        /// </summary>
        protected virtual string GetServiceDescription() => $"{ServiceName} service";

        /// <summary>
        /// Returns the service dependencies. Override in derived classes.
        /// </summary>
        protected virtual string[] GetServiceDependencies() => Array.Empty<string>();

        /// <summary>
        /// Returns the service capabilities. Override in derived classes.
        /// </summary>
        protected virtual string[] GetServiceCapabilities() => Array.Empty<string>();

        /// <summary>
        /// Returns service tags for categorization. Override in derived classes.
        /// </summary>
        protected virtual Dictionary<string, string> GetServiceTags() => new();

        /// <summary>
        /// Returns service configuration. Override in derived classes.
        /// </summary>
        protected virtual ServiceConfiguration GetServiceConfiguration() => new();

        /// <summary>
        /// Returns dependency health status. Override in derived classes.
        /// </summary>
        protected virtual Dictionary<string, string> GetDependencyStatus() => new();

        /// <summary>
        /// Returns custom metrics for derived services. Override in derived classes.
        /// </summary>
        protected virtual Dictionary<string, object> GetCustomMetrics() => new();

        /// <summary>
        /// Updates the cached health status. Override in derived classes for custom logic.
        /// </summary>
        protected virtual ServiceHealthStatus UpdateHealthStatus()
        {
            return new ServiceHealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                Message = "Service is operational",
                LastChecked = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets CPU usage as a percentage. Can be overridden by derived classes.
        /// </summary>
        protected virtual long GetCpuUsage()
        {
            try
            {
                using (var cpuCounter = new PerformanceCounter(
                    "Processor", "% Processor Time", "_Total", true))
                {
                    cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    return (long)cpuCounter.NextValue();
                }
            }
            catch
            {
                return 0; // Unable to determine CPU usage
            }
        }
    }
}
