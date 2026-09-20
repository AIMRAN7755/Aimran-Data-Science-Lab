using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates
{
    /// <summary>
    /// Base class for Scientist role services in the AIMRAN ecosystem.
    /// Scientists perform model development, training, experimentation, and validation.
    /// </summary>
    public abstract class ScientistServiceBase : ServiceBase
    {
        protected ScientistServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the primary scientist capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Model Development",
                "Model Training",
                "Experiment Management",
                "Hyperparameter Tuning",
                "Model Validation",
                "Performance Metrics",
                "Feature Engineering"
            };

        /// <summary>
        /// Gets the standard dependencies for scientist services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "ModelRegistry",
                "DataService",
                "ComputeCluster",
                "ExperimentTracker",
                "EventBus"
            };

        /// <summary>
        /// Gets scientist-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "scientist" },
                { "tier", "compute" },
                { "resource-intensive", "true" }
            };

        /// <summary>
        /// Gets scientist-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 50,
                RequestTimeout = TimeSpan.FromMinutes(10),
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                Features = new()
                {
                    { "ModelVersioning", "true" },
                    { "ExperimentTracking", "true" },
                    { "HyperparameterSearch", "true" },
                    { "DistributedTraining", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN Scientist Service - provides model development, training, experimentation, and validation capabilities";

        /// <summary>
        /// Override this method to implement model training logic.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> TrainModelAsync(
            string modelId,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("TrainModelAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement experiment execution.
        /// </summary>
        protected virtual Task<string> RunExperimentAsync(
            string experimentId,
            Dictionary<string, string> config,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("RunExperimentAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement model validation.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> ValidateModelAsync(
            string modelId,
            string datasetId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ValidateModelAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to get model metrics.
        /// </summary>
        protected virtual Task<Dictionary<string, double>> GetMetricsAsync(
            string modelId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetMetricsAsync must be implemented in derived class");
        }
    }
}
