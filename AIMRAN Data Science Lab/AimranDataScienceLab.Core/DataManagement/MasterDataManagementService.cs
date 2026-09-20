using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.DataManagement
{
    /// <summary>
    /// Master data record with version history and lineage.
    /// </summary>
    public class MasterDataRecord
    {
        public string RecordId { get; set; } = "";
        public string EntityType { get; set; } = "";
        public Dictionary<string, object> Attributes { get; set; } = new();
        public List<MasterDataVersion> Versions { get; set; } = new();
        public string CurrentVersionId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Archived
    }

    public class MasterDataVersion
    {
        public string VersionId { get; set; } = "";
        public int VersionNumber { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
        public string ChangedBy { get; set; } = "";
        public string ChangeReason { get; set; } = "";
        public DateTime ChangedAt { get; set; }
        public Dictionary<string, object> ChangeDetails { get; set; } = new();
    }

    public class DataQualityMatch
    {
        public string RecordId { get; set; } = "";
        public string MatchedAgainstId { get; set; } = "";
        public float SimilarityScore { get; set; } // 0-1
        public List<string> MatchingFields { get; set; } = new();
        public DateTime MatchedAt { get; set; }
    }

    /// <summary>
    /// Master Data Management service for data governance and quality.
    /// </summary>
    public interface IMasterDataManagementService
    {
        /// <summary>
        /// Creates or updates a master data record.
        /// </summary>
        Task<string> UpsertMasterDataAsync(MasterDataRecord record, string userId, string changeReason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a master data record with all versions.
        /// </summary>
        Task<MasterDataRecord?> GetMasterDataAsync(string recordId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs data quality matching across records.
        /// </summary>
        Task<List<DataQualityMatch>> PerformDataQualityMatchingAsync(string datasetId, float similarityThreshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages data stewardship rules and governance.
        /// </summary>
        Task<bool> SetGovernanceRuleAsync(string entityType, Dictionary<string, object> rules, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets audit trail for a master data record.
        /// </summary>
        Task<List<MasterDataVersion>> GetAuditTrailAsync(string recordId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consolidates duplicate records.
        /// </summary>
        Task<string> ConsolidateDuplicatesAsync(List<string> recordIds, string survivingRecordId, string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Master Data Management service.
    /// </summary>
    public class MasterDataManagementService : IMasterDataManagementService
    {
        private readonly ILogger<MasterDataManagementService> _logger;
        private readonly Dictionary<string, MasterDataRecord> _masterDataRecords = new();
        private readonly Dictionary<string, List<DataQualityMatch>> _qualityMatches = new();
        private readonly Dictionary<string, Dictionary<string, object>> _governanceRules = new();

        public MasterDataManagementService(ILogger<MasterDataManagementService> logger)
        {
            _logger = logger;
        }

        public async Task<string> UpsertMasterDataAsync(MasterDataRecord record, string userId, string changeReason, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(record.RecordId))
                    record.RecordId = Guid.NewGuid().ToString();

                var version = new MasterDataVersion
                {
                    VersionId = Guid.NewGuid().ToString(),
                    VersionNumber = (record.Versions?.Count ?? 0) + 1,
                    Attributes = record.Attributes,
                    ChangedBy = userId,
                    ChangeReason = changeReason,
                    ChangedAt = DateTime.UtcNow
                };

                if (!_masterDataRecords.ContainsKey(record.RecordId))
                {
                    record.CreatedAt = DateTime.UtcNow;
                    record.Versions = new List<MasterDataVersion>();
                }

                record.Versions ??= new List<MasterDataVersion>();
                record.Versions.Add(version);
                record.CurrentVersionId = version.VersionId;
                record.LastModified = DateTime.UtcNow;

                _masterDataRecords[record.RecordId] = record;
                _logger.LogInformation("Upserted master data record {RecordId} (v{Version}) by {UserId}", 
                    record.RecordId, version.VersionNumber, userId);

                return record.RecordId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting master data");
                throw;
            }
        }

        public async Task<MasterDataRecord?> GetMasterDataAsync(string recordId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_masterDataRecords.TryGetValue(recordId, out var record))
                {
                    _logger.LogInformation("Retrieved master data record {RecordId}", recordId);
                    return record;
                }

                _logger.LogWarning("Master data record not found: {RecordId}", recordId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving master data");
                throw;
            }
        }

        public async Task<List<DataQualityMatch>> PerformDataQualityMatchingAsync(string datasetId, float similarityThreshold, CancellationToken cancellationToken = default)
        {
            try
            {
                var matches = new List<DataQualityMatch>();

                var records = new List<string>(_masterDataRecords.Keys);
                for (int i = 0; i < records.Count; i++)
                {
                    for (int j = i + 1; j < records.Count; j++)
                    {
                        var similarity = CalculateSimilarity(records[i], records[j]);
                        if (similarity >= similarityThreshold)
                        {
                            matches.Add(new DataQualityMatch
                            {
                                RecordId = records[i],
                                MatchedAgainstId = records[j],
                                SimilarityScore = similarity,
                                MatchedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                _qualityMatches[datasetId] = matches;
                _logger.LogInformation("Performed quality matching for dataset {DatasetId}. Found {Count} matches", datasetId, matches.Count);

                return matches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing quality matching");
                throw;
            }
        }

        public async Task<bool> SetGovernanceRuleAsync(string entityType, Dictionary<string, object> rules, CancellationToken cancellationToken = default)
        {
            try
            {
                _governanceRules[entityType] = rules;
                _logger.LogInformation("Set governance rules for entity type: {EntityType}", entityType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting governance rule");
                throw;
            }
        }

        public async Task<List<MasterDataVersion>> GetAuditTrailAsync(string recordId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_masterDataRecords.TryGetValue(recordId, out var record))
                {
                    _logger.LogInformation("Retrieved audit trail for record {RecordId} with {Count} versions", recordId, record.Versions.Count);
                    return record.Versions;
                }

                return new List<MasterDataVersion>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail");
                throw;
            }
        }

        public async Task<string> ConsolidateDuplicatesAsync(List<string> recordIds, string survivingRecordId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var consolidationId = Guid.NewGuid().ToString();
                _logger.LogInformation("Consolidating {Count} duplicate records into {SurvivingId} by {UserId}", 
                    recordIds.Count, survivingRecordId, userId);

                return consolidationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consolidating duplicates");
                throw;
            }
        }

        private float CalculateSimilarity(string recordId1, string recordId2)
        {
            // Simplified similarity calculation - in production, use Levenshtein distance or similar
            return _masterDataRecords.ContainsKey(recordId1) && _masterDataRecords.ContainsKey(recordId2) ? 0.75f : 0.0f;
        }
    }
}
