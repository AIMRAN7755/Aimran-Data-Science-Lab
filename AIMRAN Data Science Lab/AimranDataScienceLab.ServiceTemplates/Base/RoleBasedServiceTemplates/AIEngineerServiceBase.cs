using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates
{
    /// <summary>
    /// Base class for AI Engineer role services in the AIMRAN ecosystem.
    /// AI Engineers handle model deployment, inference pipelines, model serving, and optimization.
    /// </summary>
    public abstract class AIEngineerServiceBase : ServiceBase
    {
        protected AIEngineerServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the primary AI engineer capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Model Deployment",
                "Inference Serving",
                "Batch Prediction",
                "Real-Time Inference",
                "Model Monitoring",
                "Performance Optimization",
                "A/B Testing",
                "Model Versioning"
            };

        /// <summary>
        /// Gets the standard dependencies for AI engineer services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "ModelRegistry",
                "InferenceEngine",
                "MonitoringService",
                "LoggingService",
                "FeatureStore"
            };

        /// <summary>
        /// Gets AI engineer-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "ai-engineer" },
                { "tier", "serving" },
                { "process-type", "inference" }
            };

        /// <summary>
        /// Gets AI engineer-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 1000,
                RequestTimeout = TimeSpan.FromSeconds(30),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Features = new()
                {
                    { "ModelVersionControl", "true" },
                    { "InferenceOptimization", "true" },
                    { "ABTesting", "true" },
                    { "PerformanceMonitoring", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN AI Engineer Service - provides model deployment, inference pipelines, model serving, and optimization capabilities";

        /// <summary>
        /// Override this method to implement model deployment.
        /// </summary>
        protected virtual Task<string> DeployModelAsync(
            string modelId,
            string modelVersion,
            Dictionary<string, string> deploymentConfig,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("DeployModelAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement real-time inference.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> InferAsync(
            string modelId,
            Dictionary<string, object?> features,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("InferAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement batch prediction.
        /// </summary>
        protected virtual Task<string> BatchPredictAsync(
            string modelId,
            string datasetId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("BatchPredictAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to get inference metrics.
        /// </summary>
        protected virtual Task<Dictionary<string, object?>> GetInferenceMetricsAsync(
            string modelId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetInferenceMetricsAsync must be implemented in derived class");
        }
    }
}
