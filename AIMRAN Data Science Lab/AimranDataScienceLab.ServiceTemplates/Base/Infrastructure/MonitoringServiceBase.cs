using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.Infrastructure
{
    /// <summary>
    /// Base class for the Monitoring service in the AIMRAN ecosystem.
    /// The Monitoring service collects metrics, logs, traces, and provides observability.
    /// </summary>
    public abstract class MonitoringServiceBase : ServiceBase
    {
        protected MonitoringServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the monitoring capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Metrics Collection",
                "Log Aggregation",
                "Distributed Tracing",
                "Alert Management",
                "Anomaly Detection",
                "Performance Analysis",
                "Health Dashboards",
                "Custom Metrics"
            };

        /// <summary>
        /// Gets the standard dependencies for monitoring services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "PersistentStorage",
                "TimeSeries Database",
                "EventBus"
            };

        /// <summary>
        /// Gets monitoring-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "infrastructure" },
                { "tier", "observability" },
                { "criticality", "high" },
                { "stateful", "true" }
            };

        /// <summary>
        /// Gets monitoring-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 5000,
                RequestTimeout = TimeSpan.FromSeconds(10),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Features = new()
                {
                    { "MetricsCollection", "true" },
                    { "LogAggregation", "true" },
                    { "DistributedTracing", "true" },
                    { "AnomalyDetection", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Monitoring Service - collects metrics, logs, and traces for observability and health monitoring";

        /// <summary>
        /// Override this method to record metrics.
        /// </summary>
        protected virtual Task RecordMetricAsync(
            string metricName,
            double value,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RecordMetricAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to publish structured logs.
        /// </summary>
        protected virtual Task PublishLogAsync(
            string serviceName,
            string logLevel,
            string message,
            Dictionary<string, object?> metadata,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("PublishLogAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to record distributed traces.
        /// </summary>
        protected virtual Task RecordTraceAsync(
            string traceId,
            string spanId,
            string operationName,
            Dictionary<string, object?> traceData,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RecordTraceAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to query metrics.
        /// </summary>
        protected virtual Task<List<Dictionary<string, object?>>> QueryMetricsAsync(
            string metricName,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("QueryMetricsAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to detect anomalies.
        /// </summary>
        protected virtual Task<List<Dictionary<string, object?>>> DetectAnomaliesAsync(
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("DetectAnomaliesAsync must be implemented in derived class");
        }
    }
}
