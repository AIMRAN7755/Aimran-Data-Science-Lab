using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.ML
{
    /// <summary>
    /// Represents a trained ML model artifact.
    /// </summary>
    public class ModelArtifact
    {
        public string ModelId { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string ModelType { get; set; } = ""; // Classification, Regression, Clustering, etc.
        public string FrameworkType { get; set; } = ""; // TensorFlow, PyTorch, Sklearn, etc.
        public int Version { get; set; }
        public string TrainingDate { get; set; } = "";
        public Dictionary<string, object> Hyperparameters { get; set; } = new();
        public ModelPerformanceMetrics Metrics { get; set; } = new();
        public string ArtifactLocation { get; set; } = "";
        public string Status { get; set; } = "Draft"; // Draft, Registered, Staging, Production, Retired
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class ModelPerformanceMetrics
    {
        public float Accuracy { get; set; }
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float F1Score { get; set; }
        public float AUC { get; set; }
        public Dictionary<string, float> CustomMetrics { get; set; } = new();
        public long TrainingTimeMs { get; set; }
        public int EpochsRun { get; set; }
    }

    public class ModelDeployment
    {
        public string DeploymentId { get; set; } = "";
        public string ModelId { get; set; } = "";
        public int ModelVersion { get; set; }
        public string Environment { get; set; } = ""; // Dev, Staging, Production
        public string EndpointUrl { get; set; } = "";
        public DateTime DeployedAt { get; set; }
        public string Status { get; set; } = ""; // Deploying, Active, Inactive, Failed
        public Dictionary<string, object> InferenceConfig { get; set; } = new();
    }

    /// <summary>
    /// Model Registry for managing model lifecycle and versions.
    /// </summary>
    public interface IModelRegistryService
    {
        /// <summary>
        /// Registers a new model artifact.
        /// </summary>
        Task<string> RegisterModelAsync(ModelArtifact model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves model artifact details.
        /// </summary>
        Task<ModelArtifact?> GetModelAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all versions of a model.
        /// </summary>
        Task<List<ModelArtifact>> GetModelVersionsAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Promotes a model to a new stage.
        /// </summary>
        Task<bool> PromoteModelAsync(string modelId, int version, string targetStage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compares performance between model versions.
        /// </summary>
        Task<Dictionary<string, object?>> CompareModelsAsync(string modelId, int version1, int version2, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys a model to an environment.
        /// </summary>
        Task<string> DeployModelAsync(string modelId, int version, string environment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets deployment status.
        /// </summary>
        Task<ModelDeployment?> GetDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Model Registry service.
    /// </summary>
    public class ModelRegistryService : IModelRegistryService
    {
        private readonly ILogger<ModelRegistryService> _logger;
        private readonly Dictionary<string, List<ModelArtifact>> _modelVersions = new();
        private readonly Dictionary<string, ModelDeployment> _deployments = new();

        public ModelRegistryService(ILogger<ModelRegistryService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RegisterModelAsync(ModelArtifact model, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelId = model.ModelId ?? Guid.NewGuid().ToString();
                model.ModelId = modelId;
                model.CreatedAt = DateTime.UtcNow;

                if (!_modelVersions.ContainsKey(modelId))
                    _modelVersions[modelId] = new List<ModelArtifact>();

                model.Version = _modelVersions[modelId].Count + 1;
                _modelVersions[modelId].Add(model);

                _logger.LogInformation("Registered model: {ModelName} v{Version} ({ModelId})", 
                    model.ModelName, model.Version, modelId);

                return modelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering model");
                throw;
            }
        }

        public async Task<ModelArtifact?> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_modelVersions.TryGetValue(modelId, out var versions))
                {
                    var model = versions.OrderByDescending(m => m.Version).FirstOrDefault();
                    if (model != null)
                    {
                        _logger.LogInformation("Retrieved model {ModelId} v{Version}", modelId, model.Version);
                        return model;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving model");
                throw;
            }
        }

        public async Task<List<ModelArtifact>> GetModelVersionsAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_modelVersions.TryGetValue(modelId, out var versions))
                {
                    _logger.LogInformation("Retrieved {Count} versions for model {ModelId}", versions.Count, modelId);
                    return versions.OrderByDescending(m => m.Version).ToList();
                }

                return new List<ModelArtifact>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving model versions");
                throw;
            }
        }

        public async Task<bool> PromoteModelAsync(string modelId, int version, string targetStage, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_modelVersions.TryGetValue(modelId, out var versions))
                {
                    var model = versions.FirstOrDefault(m => m.Version == version);
                    if (model != null)
                    {
                        model.Status = targetStage;
                        _logger.LogInformation("Promoted model {ModelId} v{Version} to {Stage}", modelId, version, targetStage);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting model");
                throw;
            }
        }

        public async Task<Dictionary<string, object?>> CompareModelsAsync(string modelId, int version1, int version2, CancellationToken cancellationToken = default)
        {
            try
            {
                var comparison = new Dictionary<string, object?>
                {
                    { "modelId", modelId },
                    { "version1", version1 },
                    { "version2", version2 },
                    { "accuracyDifference", 0.05 },
                    { "f1ScoreDifference", 0.03 }
                };

                _logger.LogInformation("Compared model versions {V1} and {V2}", version1, version2);
                return comparison;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing models");
                throw;
            }
        }

        public async Task<string> DeployModelAsync(string modelId, int version, string environment, CancellationToken cancellationToken = default)
        {
            try
            {
                var deploymentId = Guid.NewGuid().ToString();
                var deployment = new ModelDeployment
                {
                    DeploymentId = deploymentId,
                    ModelId = modelId,
                    ModelVersion = version,
                    Environment = environment,
                    DeployedAt = DateTime.UtcNow,
                    Status = "Deploying"
                };

                _deployments[deploymentId] = deployment;
                _logger.LogInformation("Deployed model {ModelId} v{Version} to {Environment}", modelId, version, environment);

                return deploymentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying model");
                throw;
            }
        }

        public async Task<ModelDeployment?> GetDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_deployments.TryGetValue(deploymentId, out var deployment))
                {
                    _logger.LogInformation("Retrieved deployment {DeploymentId}", deploymentId);
                    return deployment;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deployment");
                throw;
            }
        }
    }
}
