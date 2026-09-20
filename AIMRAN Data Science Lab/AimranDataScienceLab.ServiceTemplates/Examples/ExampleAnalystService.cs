using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AimranDataScienceLab.ServiceTemplates.Base;

namespace AimranDataScienceLab.ServiceTemplates.Examples
{
    /// <summary>
    /// Example service implementation showing how to use the service templates.
    /// This is a simple Analyst Service that can be deployed to Docker, Kubernetes, or Windows Services.
    /// </summary>
    public class ExampleAnalystService : ServiceBase
    {
        private readonly ILogger<ExampleAnalystService> _logger;
        private DateTime _startTime;

        public ExampleAnalystService(ILogger<ExampleAnalystService> logger)
            : base(logger)
        {
            _logger = logger;
        }

        public override string ServiceId => "analyst-example-1";
        public override string ServiceName => "ExampleAnalystService";
        public override Version ServiceVersion => new(1, 0, 0);

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Example Analyst Service initializing");
            _startTime = DateTime.UtcNow;
            await Task.Delay(100); // Simulate initialization work
        }

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Example Analyst Service starting");
            await Task.Delay(50); // Simulate startup work
        }

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Example Analyst Service stopping");
            await Task.Delay(50); // Simulate cleanup work
        }

        protected override async Task<ServiceHealthStatus> OnCheckHealthAsync(
            CancellationToken cancellationToken)
        {
            var uptime = DateTime.UtcNow - _startTime;

            return new ServiceHealthStatus
            {
                IsHealthy = uptime.TotalSeconds > 0,
                Status = uptime.TotalSeconds > 0 ? "Healthy" : "Starting",
                Message = $"Running for {uptime.TotalSeconds:F0} seconds",
                LastChecked = DateTime.UtcNow,
                Details = new Dictionary<string, string>
                {
                    { "uptime_seconds", uptime.TotalSeconds.ToString("F0") },
                    { "version", ServiceVersion.ToString() }
                }
            };
        }

        protected override string GetServiceDescription() =>
            "Example Analyst Service demonstrating service templates";

        protected override string[] GetServiceDependencies() =>
            new[] { "ServiceRegistry", "EventBus", "Database" };

        protected override string[] GetServiceCapabilities() =>
            new[] { "SQL Execution", "Dashboard Management", "Report Generation", "EDA Analysis" };

        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "analyst" },
                { "version", ServiceVersion.ToString() },
                { "environment", "example" }
            };

        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 100,
                RequestTimeout = TimeSpan.FromSeconds(30),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Environment = new()
                {
                    { "ASPNETCORE_ENVIRONMENT", "Development" },
                    { "SERVICE_REGISTRY_URL", "http://localhost:5001" }
                },
                Features = new()
                {
                    { "EnableCaching", "true" },
                    { "EnableMetrics", "true" },
                    { "EnableTracing", "false" }
                }
            };

        protected override Dictionary<string, string> GetDependencyStatus() =>
            new()
            {
                { "ServiceRegistry", "Healthy" },
                { "EventBus", "Healthy" },
                { "Database", "Healthy" }
            };

        protected override Dictionary<string, object> GetCustomMetrics() =>
            new()
            {
                { "queries_executed", 1234 },
                { "reports_generated", 42 },
                { "dashboards_created", 15 }
            };
    }
}
