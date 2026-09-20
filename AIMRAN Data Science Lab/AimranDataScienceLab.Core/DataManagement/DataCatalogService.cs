using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.DataManagement
{
    /// <summary>
    /// Data asset metadata for cataloging and discovery.
    /// </summary>
    public class DataAssetMetadata
    {
        public string AssetId { get; set; } = "";
        public string AssetName { get; set; } = "";
        public string AssetType { get; set; } = ""; // Table, File, Stream, API, etc.
        public string Description { get; set; } = "";
        public string Owner { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> TechnicalMetadata { get; set; } = new();
        public List<string> AccessibleBy { get; set; } = new();
        public DataSensitivity Sensitivity { get; set; } = DataSensitivity.Public;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int AccessCount { get; set; }
    }

    public enum DataSensitivity
    {
        Public = 0,
        Internal = 1,
        Confidential = 2,
        Restricted = 3
    }

    public class DataLineageNode
    {
        public string NodeId { get; set; } = "";
        public string AssetId { get; set; } = "";
        public List<string> DependsOn { get; set; } = new();
        public List<string> UsedBy { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Data Catalog service for asset discovery, metadata management, and lineage tracking.
    /// </summary>
    public interface IDataCatalogService
    {
        /// <summary>
        /// Registers a data asset in the catalog.
        /// </summary>
        Task<string> RegisterAssetAsync(DataAssetMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers assets by search criteria.
        /// </summary>
        Task<List<DataAssetMetadata>> DiscoverAssetsAsync(Dictionary<string, string> criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed lineage for an asset.
        /// </summary>
        Task<DataLineageNode> GetLineageAsync(string assetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks data lineage relationships.
        /// </summary>
        Task<bool> TrackLineageAsync(string sourceAssetId, string targetAssetId, Dictionary<string, object> transformationDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates asset metadata.
        /// </summary>
        Task<bool> UpdateAssetMetadataAsync(string assetId, DataAssetMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets usage statistics for an asset.
        /// </summary>
        Task<Dictionary<string, object?>> GetAssetUsageStatsAsync(string assetId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Data Catalog service.
    /// </summary>
    public class DataCatalogService : IDataCatalogService
    {
        private readonly ILogger<DataCatalogService> _logger;
        private readonly Dictionary<string, DataAssetMetadata> _assets = new();
        private readonly Dictionary<string, DataLineageNode> _lineage = new();
        private readonly Dictionary<string, Dictionary<string, object>> _usageStats = new();

        public DataCatalogService(ILogger<DataCatalogService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RegisterAssetAsync(DataAssetMetadata metadata, CancellationToken cancellationToken = default)
        {
            try
            {
                var assetId = metadata.AssetId ?? Guid.NewGuid().ToString();
                metadata.AssetId = assetId;
                metadata.CreatedAt = DateTime.UtcNow;

                _assets[assetId] = metadata;

                var lineageNode = new DataLineageNode
                {
                    NodeId = Guid.NewGuid().ToString(),
                    AssetId = assetId,
                    CreatedAt = DateTime.UtcNow
                };
                _lineage[assetId] = lineageNode;

                _usageStats[assetId] = new Dictionary<string, object>
                {
                    { "accessCount", 0 },
                    { "lastAccessed", null! }
                };

                _logger.LogInformation("Registered asset in catalog: {AssetName} ({AssetId})", metadata.AssetName, assetId);
                return assetId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering asset");
                throw;
            }
        }

        public async Task<List<DataAssetMetadata>> DiscoverAssetsAsync(Dictionary<string, string> criteria, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = new List<DataAssetMetadata>();

                foreach (var asset in _assets.Values)
                {
                    bool matches = true;

                    foreach (var criterion in criteria)
                    {
                        if (criterion.Key == "AssetType" && asset.AssetType != criterion.Value)
                            matches = false;
                        else if (criterion.Key == "Owner" && asset.Owner != criterion.Value)
                            matches = false;
                        else if (criterion.Key == "Sensitivity" && (!Enum.TryParse<DataSensitivity>(criterion.Value, out var sens) || asset.Sensitivity != sens))
                            matches = false;
                        else if (criterion.Key == "Tags")
                        {
                            var searchTags = criterion.Value.Split(',');
                            if (!searchTags.Any(t => asset.Tags.Contains(t.Trim())))
                                matches = false;
                        }
                    }

                    if (matches)
                        results.Add(asset);
                }

                _logger.LogInformation("Discovered {Count} assets matching criteria", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering assets");
                throw;
            }
        }

        public async Task<DataLineageNode> GetLineageAsync(string assetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_lineage.TryGetValue(assetId, out var node))
                {
                    _logger.LogInformation("Retrieved lineage for asset {AssetId}", assetId);
                    return node;
                }

                return new DataLineageNode { AssetId = assetId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lineage");
                throw;
            }
        }

        public async Task<bool> TrackLineageAsync(string sourceAssetId, string targetAssetId, Dictionary<string, object> transformationDetails, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_lineage.ContainsKey(sourceAssetId) && _lineage.ContainsKey(targetAssetId))
                {
                    var sourceNode = _lineage[sourceAssetId];
                    var targetNode = _lineage[targetAssetId];

                    sourceNode.UsedBy.Add(targetAssetId);
                    targetNode.DependsOn.Add(sourceAssetId);

                    _logger.LogInformation("Tracked lineage from {Source} to {Target}", sourceAssetId, targetAssetId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking lineage");
                throw;
            }
        }

        public async Task<bool> UpdateAssetMetadataAsync(string assetId, DataAssetMetadata metadata, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_assets.ContainsKey(assetId))
                {
                    metadata.AssetId = assetId;
                    metadata.LastModified = DateTime.UtcNow;
                    _assets[assetId] = metadata;

                    _logger.LogInformation("Updated metadata for asset {AssetId}", assetId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset metadata");
                throw;
            }
        }

        public async Task<Dictionary<string, object?>> GetAssetUsageStatsAsync(string assetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_usageStats.TryGetValue(assetId, out var stats))
                {
                    _logger.LogInformation("Retrieved usage stats for asset {AssetId}", assetId);
                    return stats.ToDictionary(x => x.Key, x => (object?)x.Value);
                }

                return new Dictionary<string, object?> { { "recorded", false } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage stats");
                throw;
            }
        }
    }
}
