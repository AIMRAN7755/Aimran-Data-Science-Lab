using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.Infrastructure
{
    /// <summary>
    /// Base class for the Service Registry in the AIMRAN ecosystem.
    /// The Registry maintains service discovery, registration, and health status information.
    /// </summary>
    public abstract class RegistryServiceBase : ServiceBase
    {
        protected RegistryServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the registry capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Service Registration",
                "Service Discovery",
                "Health Status Tracking",
                "Service Metadata Management",
                "Dependency Mapping",
                "Service Deregistration",
                "Lease Management"
            };

        /// <summary>
        /// Gets the standard dependencies for registry services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "MonitoringService",
                "EventBus",
                "PersistentStorage"
            };

        /// <summary>
        /// Gets registry-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "infrastructure" },
                { "tier", "registry" },
                { "criticality", "high" },
                { "stateful", "true" }
            };

        /// <summary>
        /// Gets registry-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 5000,
                RequestTimeout = TimeSpan.FromSeconds(10),
                HealthCheckInterval = TimeSpan.FromSeconds(5),
                Features = new()
                {
                    { "PersistentStorage", "true" },
                    { "Replication", "true" },
                    { "HealthTracking", "true" },
                    { "LeaseManagement", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Service Registry - maintains service discovery, registration, and health status information";

        /// <summary>
        /// Override this method to implement service registration.
        /// </summary>
        protected virtual Task<string> RegisterServiceAsync(
            string serviceName,
            Dictionary<string, string> serviceMetadata,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RegisterServiceAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement service discovery.
        /// </summary>
        protected virtual Task<List<Dictionary<string, string>>> DiscoverServiceAsync(
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("DiscoverServiceAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to update service health status.
        /// </summary>
        protected virtual Task UpdateHealthStatusAsync(
            string serviceId,
            Dictionary<string, object?> healthStatus,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("UpdateHealthStatusAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to deregister a service.
        /// </summary>
        protected virtual Task DeregisterServiceAsync(
            string serviceId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("DeregisterServiceAsync must be implemented in derived class");
        }
    }
}
