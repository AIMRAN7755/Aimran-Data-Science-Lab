using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.Infrastructure
{
    /// <summary>
    /// Base class for the Service Gateway in the AIMRAN ecosystem.
    /// The Gateway handles request routing, load balancing, authentication, and API management.
    /// </summary>
    public abstract class GatewayServiceBase : ServiceBase
    {
        protected GatewayServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the gateway capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Request Routing",
                "Load Balancing",
                "Authentication",
                "Authorization",
                "Rate Limiting",
                "Request Validation",
                "Response Caching",
                "API Documentation"
            };

        /// <summary>
        /// Gets the standard dependencies for gateway services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "AuthenticationService",
                "MonitoringService",
                "EventBus"
            };

        /// <summary>
        /// Gets gateway-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "infrastructure" },
                { "tier", "gateway" },
                { "criticality", "high" }
            };

        /// <summary>
        /// Gets gateway-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 10000,
                RequestTimeout = TimeSpan.FromSeconds(30),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Features = new()
                {
                    { "RateLimiting", "true" },
                    { "Caching", "true" },
                    { "LoadBalancing", "true" },
                    { "Authentication", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Service Gateway - provides request routing, load balancing, authentication, and API management";

        /// <summary>
        /// Override this method to implement route registration.
        /// </summary>
        protected virtual Task RegisterRouteAsync(
            string routePath,
            string targetService,
            Dictionary<string, string> routeConfig,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RegisterRouteAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement request routing.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> RouteRequestAsync(
            string path,
            string method,
            Dictionary<string, object?> requestBody,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RouteRequestAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement rate limiting.
        /// </summary>
        protected virtual Task<bool> CheckRateLimitAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("CheckRateLimitAsync must be implemented in derived class");
        }
    }
}
