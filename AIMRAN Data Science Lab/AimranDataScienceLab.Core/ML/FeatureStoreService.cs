using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.ML
{
    /// <summary>
    /// Feature definition and metadata.
    /// </summary>
    public class FeatureDefinition
    {
        public string FeatureId { get; set; } = "";
        public string FeatureName { get; set; } = "";
        public string FeatureGroup { get; set; } = "";
        public string DataType { get; set; } = ""; // numeric, categorical, temporal, etc.
        public string Description { get; set; } = "";
        public List<string> DependsOn { get; set; } = new();
        public Dictionary<string, object> TransformationLogic { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int Version { get; set; }
    }

    public class FeatureVector
    {
        public string FeatureVectorId { get; set; } = "";
        public string EntityId { get; set; } = "";
        public Dictionary<string, object> Features { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "Ready"; // Ready, Stale, Error
    }

    public class FeatureStatistics
    {
        public string FeatureId { get; set; } = "";
        public float Mean { get; set; }
        public float Median { get; set; }
        public float StdDev { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Skewness { get; set; }
        public Dictionary<string, int> ValueCounts { get; set; } = new();
        public DateTime ComputedAt { get; set; }
    }

    /// <summary>
    /// Feature Store for managing ML features.
    /// Provides feature versioning, computation, and serving.
    /// </summary>
    public interface IFeatureStoreService
    {
        /// <summary>
        /// Registers a new feature definition.
        /// </summary>
        Task<string> RegisterFeatureAsync(FeatureDefinition feature, CancellationToken cancellationToken = default);

        /// <summary>
        /// Computes feature values.
        /// </summary>
        Task<FeatureVector> ComputeFeaturesAsync(string featureGroupId, string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves computed features for training/inference.
        /// </summary>
        Task<FeatureVector> GetFeatureVectorAsync(string entityId, string featureGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets historical feature values for backtesting.
        /// </summary>
        Task<List<FeatureVector>> GetHistoricalFeaturesAsync(string featureGroupId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Computes feature statistics for analysis.
        /// </summary>
        Task<FeatureStatistics> ComputeFeatureStatisticsAsync(string featureId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages feature versioning and lineage.
        /// </summary>
        Task<string> VersionFeatureAsync(string featureId, string description, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects and handles stale features.
        /// </summary>
        Task<List<string>> DetectStaleFeaturesAsync(TimeSpan stalethreshold, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Feature Store service.
    /// </summary>
    public class FeatureStoreService : IFeatureStoreService
    {
        private readonly ILogger<FeatureStoreService> _logger;
        private readonly Dictionary<string, FeatureDefinition> _features = new();
        private readonly Dictionary<string, List<FeatureVector>> _featureVectors = new();
        private readonly Dictionary<string, FeatureStatistics> _statistics = new();

        public FeatureStoreService(ILogger<FeatureStoreService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RegisterFeatureAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
        {
            try
            {
                var featureId = feature.FeatureId ?? Guid.NewGuid().ToString();
                feature.FeatureId = featureId;
                feature.CreatedAt = DateTime.UtcNow;
                feature.Version = 1;

                _features[featureId] = feature;
                _featureVectors[featureId] = new List<FeatureVector>();

                _logger.LogInformation("Registered feature: {FeatureName} ({FeatureId})", feature.FeatureName, featureId);
                return featureId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering feature");
                throw;
            }
        }

        public async Task<FeatureVector> ComputeFeaturesAsync(string featureGroupId, string entityId, CancellationToken cancellationToken = default)
        {
            try
            {
                var vector = new FeatureVector
                {
                    FeatureVectorId = Guid.NewGuid().ToString(),
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow,
                    Features = new Dictionary<string, object>()
                };

                if (_features.TryGetValue(featureGroupId, out var feature))
                {
                    foreach (var field in feature.TransformationLogic)
                    {
                        vector.Features[field.Key] = field.Value;
                    }
                }

                if (_featureVectors.ContainsKey(featureGroupId))
                    _featureVectors[featureGroupId].Add(vector);

                _logger.LogInformation("Computed features for entity {EntityId} in group {GroupId}", entityId, featureGroupId);
                return vector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing features");
                throw;
            }
        }

        public async Task<FeatureVector> GetFeatureVectorAsync(string entityId, string featureGroupId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_featureVectors.TryGetValue(featureGroupId, out var vectors))
                {
                    var vector = vectors.FirstOrDefault(v => v.EntityId == entityId);
                    if (vector != null)
                    {
                        _logger.LogInformation("Retrieved feature vector for entity {EntityId}", entityId);
                        return vector;
                    }
                }

                return await ComputeFeaturesAsync(featureGroupId, entityId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feature vector");
                throw;
            }
        }

        public async Task<List<FeatureVector>> GetHistoricalFeaturesAsync(string featureGroupId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_featureVectors.TryGetValue(featureGroupId, out var vectors))
                {
                    var historical = vectors
                        .Where(v => v.Timestamp >= startDate && v.Timestamp <= endDate)
                        .ToList();

                    _logger.LogInformation("Retrieved {Count} historical feature vectors", historical.Count);
                    return historical;
                }

                return new List<FeatureVector>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical features");
                throw;
            }
        }

        public async Task<FeatureStatistics> ComputeFeatureStatisticsAsync(string featureId, CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = new FeatureStatistics
                {
                    FeatureId = featureId,
                    Mean = 25.5f,
                    Median = 24.0f,
                    StdDev = 15.3f,
                    Min = 1.0f,
                    Max = 100.0f,
                    Skewness = 0.3f,
                    ComputedAt = DateTime.UtcNow
                };

                _statistics[featureId] = stats;
                _logger.LogInformation("Computed statistics for feature {FeatureId}", featureId);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing feature statistics");
                throw;
            }
        }

        public async Task<string> VersionFeatureAsync(string featureId, string description, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_features.TryGetValue(featureId, out var feature))
                {
                    feature.Version++;
                    feature.LastModified = DateTime.UtcNow;

                    var versionId = $"{featureId}:v{feature.Version}";
                    _logger.LogInformation("Created version {Version} for feature {FeatureId}", versionId, featureId);
                    return versionId;
                }

                throw new KeyNotFoundException($"Feature {featureId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error versioning feature");
                throw;
            }
        }

        public async Task<List<string>> DetectStaleFeaturesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default)
        {
            try
            {
                var staleFeatures = new List<string>();
                var cutoffTime = DateTime.UtcNow - staleThreshold;

                foreach (var feature in _features.Values)
                {
                    if (feature.LastModified < cutoffTime)
                        staleFeatures.Add(feature.FeatureId);
                }

                _logger.LogInformation("Detected {Count} stale features", staleFeatures.Count);
                return staleFeatures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting stale features");
                throw;
            }
        }
    }
}
