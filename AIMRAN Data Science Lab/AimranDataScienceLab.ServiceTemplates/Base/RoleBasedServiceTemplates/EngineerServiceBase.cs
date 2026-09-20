using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates
{
    /// <summary>
    /// Base class for Engineer role services in the AIMRAN ecosystem.
    /// Engineers handle infrastructure, data pipelines, ETL, and system integration.
    /// </summary>
    public abstract class EngineerServiceBase : ServiceBase
    {
        protected EngineerServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the primary engineer capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Data Pipeline Orchestration",
                "ETL Processing",
                "Data Validation",
                "Schema Management",
                "Data Quality Monitoring",
                "Integration Management",
                "Batch Processing",
                "Stream Processing"
            };

        /// <summary>
        /// Gets the standard dependencies for engineer services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "DataStore",
                "EventBus",
                "WorkflowEngine",
                "MetadataStore",
                "QualityMonitor"
            };

        /// <summary>
        /// Gets engineer-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "engineer" },
                { "tier", "middleware" },
                { "processor-type", "batch-stream" }
            };

        /// <summary>
        /// Gets engineer-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 200,
                RequestTimeout = TimeSpan.FromMinutes(5),
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                Features = new()
                {
                    { "PipelineOrchestation", "true" },
                    { "SchemaValidation", "true" },
                    { "DataQualityChecks", "true" },
                    { "FailoverRecovery", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Engineer Service - provides data pipeline orchestration, ETL processing, data validation, and integration capabilities";

        /// <summary>
        /// Override this method to implement pipeline execution.
        /// </summary>
        protected virtual Task<string> ExecutePipelineAsync(
            string pipelineId,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ExecutePipelineAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement ETL transformation.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> TransformDataAsync(
            string sourceId,
            string targetId,
            Dictionary<string, string> transformRules,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("TransformDataAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement data validation.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> ValidateDataAsync(
            string datasetId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ValidateDataAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to manage data schemas.
        /// </summary>
        protected virtual Task<string> ManageSchemaAsync(
            string schemaId,
            string operation,
            Dictionary<string, string> schemaDefition,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ManageSchemaAsync must be implemented in derived class");
        }
    }
}
