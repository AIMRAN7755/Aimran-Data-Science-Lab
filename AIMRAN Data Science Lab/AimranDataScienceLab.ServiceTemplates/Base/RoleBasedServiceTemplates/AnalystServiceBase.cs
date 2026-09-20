using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates
{
    /// <summary>
    /// Base class for Analyst role services in the AIMRAN ecosystem.
    /// Analysts perform SQL execution, dashboard management, report generation, and EDA analysis.
    /// </summary>
    public abstract class AnalystServiceBase : ServiceBase
    {
        protected AnalystServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the primary analyst capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "SQL Query Execution",
                "Dashboard Management",
                "Report Generation",
                "Exploratory Data Analysis",
                "Data Visualization",
                "Query Optimization"
            };

        /// <summary>
        /// Gets the standard dependencies for analyst services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "DatabaseService",
                "EventBus",
                "AuthenticationService",
                "VizualizationEngine"
            };

        /// <summary>
        /// Gets analyst-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "analyst" },
                { "tier", "application" },
                { "data-access", "read-write" }
            };

        /// <summary>
        /// Gets analyst-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 100,
                RequestTimeout = TimeSpan.FromSeconds(60),
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                Features = new()
                {
                    { "QueryCaching", "true" },
                    { "QueryOptimization", "true" },
                    { "VisualizationSupport", "true" },
                    { "ReportGeneration", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Analyst Service - provides SQL execution, dashboard management, report generation, and data analysis capabilities";

        /// <summary>
        /// Override this method to implement analyst-specific query execution logic.
        /// </summary>
        protected virtual Task<string> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ExecuteQueryAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement dashboard data aggregation.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> GetDashboardDataAsync(
            string dashboardId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetDashboardDataAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement report generation.
        /// </summary>
        protected virtual Task<byte[]> GenerateReportAsync(
            string reportId,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GenerateReportAsync must be implemented in derived class");
        }
    }
}
