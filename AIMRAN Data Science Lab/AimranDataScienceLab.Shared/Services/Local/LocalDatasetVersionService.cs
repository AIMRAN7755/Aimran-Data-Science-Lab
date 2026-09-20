using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of the dataset versioning service.
/// Provides Git-like versioning capabilities for datasets.
/// </summary>
public sealed class LocalDatasetVersionService : IDatasetVersionService
{
    private readonly IDatasetStorageProvider _storageProvider;
    private readonly IDeltaEngine _deltaEngine;
    private readonly Dictionary<DatasetVersionId, DatasetVersion> _versionCache = new();
    private readonly object _lock = new();

    public LocalDatasetVersionService(
        IDatasetStorageProvider storageProvider,
        IDeltaEngine deltaEngine)
    {
        _storageProvider = storageProvider;
        _deltaEngine = deltaEngine;
    }

    #region Snapshot Operations

    public async Task<SnapshotResult> CreateSnapshotAsync(
        DatasetSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Get current version to determine strategy
            var currentVersion = await GetCurrentVersionAsync(request.DatasetId, cancellationToken);
            var strategy = request.Strategy;

            if (strategy == SnapshotStrategy.Auto && currentVersion != null)
            {
                // Check if delta would be beneficial
                var currentPath = await GetVersionDataPathAsync(currentVersion.Id, cancellationToken);
                if (File.Exists(currentPath))
                {
                    var config = new VersionStorageConfig
                    {
                        DeltaThresholdRatio = 0.3,
                        CompressDeltas = true
                    };

                    var recommendation = await _deltaEngine.ShouldUseDeltaAsync(
                        currentPath, request.DatasetPath, config, cancellationToken);

                    strategy = recommendation.UseDelta ? SnapshotStrategy.DeltaOnly : SnapshotStrategy.FullSnapshot;
                }
                else
                {
                    strategy = SnapshotStrategy.FullSnapshot;
                }
            }
            else if (strategy == SnapshotStrategy.Auto)
            {
                strategy = SnapshotStrategy.FullSnapshot;
            }

            return strategy == SnapshotStrategy.DeltaOnly && currentVersion != null
                ? await CreateDeltaSnapshotAsync(request, currentVersion, cancellationToken)
                : await CreateFullSnapshotAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return new SnapshotResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    public async Task<SnapshotResult> CreateFullSnapshotAsync(
        DatasetSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var fileInfo = new FileInfo(request.DatasetPath);
            if (!fileInfo.Exists)
            {
                return new SnapshotResult
                {
                    Success = false,
                    ErrorMessage = "Source file not found",
                    Duration = sw.Elapsed
                };
            }

            // Compute hash
            var hash = await _storageProvider.ComputeHashAsync(request.DatasetPath, cancellationToken);

            // Get version number
            var existingVersions = await GetAllVersionsAsync(request.DatasetId, cancellationToken);
            var versionNumber = existingVersions.Count > 0
                ? existingVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var currentVersion = existingVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

            // Read metadata from file
            var metadata = await ExtractMetadataAsync(request.DatasetPath, cancellationToken);

            var version = new DatasetVersion
            {
                DatasetId = request.DatasetId,
                Name = request.Name ?? $"Version {versionNumber}",
                Description = request.Description,
                VersionNumber = versionNumber,
                ParentVersionId = currentVersion?.Id,
                CreatedBy = request.CreatedBy,
                Hash = hash,
                SizeBytes = fileInfo.Length,
                VersionType = DatasetVersionType.FullSnapshot,
                Status = DatasetVersionStatus.Active,
                Metadata = metadata,
                Tags = request.Tags
            };

            // Save to storage
            var storagePath = await _storageProvider.SaveDatasetAsync(
                request.DatasetId, version.Id, request.DatasetPath, cancellationToken);

            version = version with { StoragePointer = storagePath };

            // Save metadata
            await _storageProvider.SaveVersionMetadataAsync(request.DatasetId, version, cancellationToken);

            // Update cache
            lock (_lock)
            {
                _versionCache[version.Id] = version;
            }

            sw.Stop();

            return new SnapshotResult
            {
                Success = true,
                Version = version,
                UsedStrategy = SnapshotStrategy.FullSnapshot,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new SnapshotResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<SnapshotResult> CreateDeltaSnapshotAsync(
        DatasetSnapshotRequest request,
        DatasetVersion parentVersion,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var parentPath = await GetVersionDataPathAsync(parentVersion.Id, cancellationToken);

            // Compute delta
            var deltaResult = await _deltaEngine.ComputeDeltaAsync(
                parentPath, request.DatasetPath,
                new DeltaComputeOptions { Compress = true },
                cancellationToken);

            if (!deltaResult.Success || deltaResult.Delta == null || deltaResult.DeltaData == null)
            {
                // Fall back to full snapshot
                return await CreateFullSnapshotAsync(request, cancellationToken);
            }

            var fileInfo = new FileInfo(request.DatasetPath);
            var hash = await _storageProvider.ComputeHashAsync(request.DatasetPath, cancellationToken);

            var existingVersions = await GetAllVersionsAsync(request.DatasetId, cancellationToken);
            var versionNumber = existingVersions.Max(v => v.VersionNumber) + 1;

            var metadata = await ExtractMetadataAsync(request.DatasetPath, cancellationToken);
            metadata = metadata with
            {
                ChangeStats = new VersionChangeStats
                {
                    RowsAdded = deltaResult.Stats.RowsAdded,
                    RowsRemoved = deltaResult.Stats.RowsRemoved,
                    RowsModified = deltaResult.Stats.RowsModified
                }
            };

            var version = new DatasetVersion
            {
                DatasetId = request.DatasetId,
                Name = request.Name ?? $"Version {versionNumber}",
                Description = request.Description,
                VersionNumber = versionNumber,
                ParentVersionId = parentVersion.Id,
                CreatedBy = request.CreatedBy,
                Hash = hash,
                SizeBytes = deltaResult.DeltaSize,
                VersionType = DatasetVersionType.Delta,
                Status = DatasetVersionStatus.Active,
                Metadata = metadata,
                Tags = request.Tags
            };

            // Save delta and full snapshot
            var delta = deltaResult.Delta with
            {
                BaseVersionId = parentVersion.Id,
                TargetVersionId = version.Id
            };

            var deltaPath = await _storageProvider.SaveDeltaAsync(
                request.DatasetId, delta, deltaResult.DeltaData, cancellationToken);

            // Also save full snapshot for direct access
            var snapshotPath = await _storageProvider.SaveDatasetAsync(
                request.DatasetId, version.Id, request.DatasetPath, cancellationToken);

            version = version with { StoragePointer = snapshotPath };

            await _storageProvider.SaveVersionMetadataAsync(request.DatasetId, version, cancellationToken);

            lock (_lock)
            {
                _versionCache[version.Id] = version;
            }

            sw.Stop();

            return new SnapshotResult
            {
                Success = true,
                Version = version,
                UsedStrategy = SnapshotStrategy.DeltaOnly,
                Duration = sw.Elapsed,
                StorageSaved = fileInfo.Length - deltaResult.DeltaSize
            };
        }
        catch (Exception ex)
        {
            return new SnapshotResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    public async Task<bool> HasChangesAsync(
        DatasetId datasetId,
        string currentPath,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = await GetCurrentVersionAsync(datasetId, cancellationToken);
        if (currentVersion == null)
        {
            return true; // No previous version, so any file is a change
        }

        var currentHash = await _storageProvider.ComputeHashAsync(currentPath, cancellationToken);
        return !string.Equals(currentHash, currentVersion.Hash, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Diff Operations

    public async Task<DatasetDiffResult> DiffAsync(
        DatasetVersionId fromVersion,
        DatasetVersionId toVersion,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fromPath = await GetVersionDataPathAsync(fromVersion, cancellationToken);
        var toPath = await GetVersionDataPathAsync(toVersion, cancellationToken);

        var result = await _deltaEngine.DiffDatasetsAsync(fromPath, toPath, options, cancellationToken);

        var from = await GetVersionAsync(fromVersion, cancellationToken);
        var to = await GetVersionAsync(toVersion, cancellationToken);

        return result with
        {
            FromVersionId = fromVersion,
            ToVersionId = toVersion,
            FromVersionNumber = from?.VersionNumber ?? 0,
            ToVersionNumber = to?.VersionNumber ?? 0
        };
    }

    public async Task<DatasetDiffResult> DiffWithCurrentAsync(
        DatasetId datasetId,
        string currentPath,
        DatasetVersionId compareToVersion,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var versionPath = await GetVersionDataPathAsync(compareToVersion, cancellationToken);
        return await _deltaEngine.DiffDatasetsAsync(versionPath, currentPath, options, cancellationToken);
    }

    public async Task<DiffSummary> GetDiffSummaryAsync(
        DatasetVersionId fromVersion,
        DatasetVersionId toVersion,
        CancellationToken cancellationToken = default)
    {
        var diff = await DiffAsync(fromVersion, toVersion, new DiffOptions
        {
            IncludeSampleChanges = false,
            ComputeColumnStats = false
        }, cancellationToken);

        return new DiffSummary
        {
            FromVersionId = fromVersion,
            ToVersionId = toVersion,
            RowsAdded = diff.RowsAdded,
            RowsRemoved = diff.RowsRemoved,
            RowsModified = diff.RowsModified,
            HasSchemaChanges = diff.SchemaDiff.HasSchemaChanges,
            ChangePercentage = diff.ChangePercentage,
            Severity = diff.Severity
        };
    }

    #endregion

    #region Rollback Operations

    public async Task<RollbackResult> RollbackAsync(
        RollbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var validation = await ValidateRollbackAsync(request.DatasetId, request.TargetVersionId, cancellationToken);
            if (!validation.IsValid)
            {
                return new RollbackResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Issues),
                    Duration = sw.Elapsed
                };
            }

            var currentVersion = await GetCurrentVersionAsync(request.DatasetId, cancellationToken);
            DatasetVersion? backupVersion = null;

            // Create backup if requested
            if (request.CreateBackup && currentVersion != null)
            {
                var currentPath = await GetVersionDataPathAsync(currentVersion.Id, cancellationToken);
                var backupResult = await CreateFullSnapshotAsync(new DatasetSnapshotRequest
                {
                    DatasetId = request.DatasetId,
                    DatasetPath = currentPath,
                    Name = $"Backup before rollback to v{(await GetVersionAsync(request.TargetVersionId, cancellationToken))?.VersionNumber}",
                    Description = $"Automatic backup created before rollback. Reason: {request.Reason}",
                    CreatedBy = request.RequestedBy
                }, cancellationToken);

                backupVersion = backupResult.Version;
            }

            // Get target version data
            var targetVersion = await GetVersionAsync(request.TargetVersionId, cancellationToken);
            if (targetVersion == null)
            {
                return new RollbackResult
                {
                    Success = false,
                    ErrorMessage = "Target version not found",
                    Duration = sw.Elapsed
                };
            }

            var targetPath = await GetVersionDataPathAsync(request.TargetVersionId, cancellationToken);

            // Create new version representing the rollback
            var existingVersions = await GetAllVersionsAsync(request.DatasetId, cancellationToken);
            var versionNumber = existingVersions.Max(v => v.VersionNumber) + 1;

            var metadata = await ExtractMetadataAsync(targetPath, cancellationToken);

            var rollbackVersion = new DatasetVersion
            {
                DatasetId = request.DatasetId,
                Name = $"Rollback to v{targetVersion.VersionNumber}",
                Description = request.Reason ?? $"Rolled back to version {targetVersion.VersionNumber}",
                VersionNumber = versionNumber,
                ParentVersionId = currentVersion?.Id,
                CreatedBy = request.RequestedBy,
                Hash = targetVersion.Hash,
                SizeBytes = targetVersion.SizeBytes,
                VersionType = DatasetVersionType.Rollback,
                Status = DatasetVersionStatus.Active,
                Metadata = metadata,
                Tags = new Dictionary<string, string>
                {
                    ["rollback_source"] = targetVersion.Id.ToString(),
                    ["rollback_reason"] = request.Reason ?? "Manual rollback"
                }
            };

            // Save rollback version (copy from target)
            var storagePath = await _storageProvider.SaveDatasetAsync(
                request.DatasetId, rollbackVersion.Id, targetPath, cancellationToken);

            rollbackVersion = rollbackVersion with { StoragePointer = storagePath };

            await _storageProvider.SaveVersionMetadataAsync(request.DatasetId, rollbackVersion, cancellationToken);

            lock (_lock)
            {
                _versionCache[rollbackVersion.Id] = rollbackVersion;
            }

            sw.Stop();

            return new RollbackResult
            {
                Success = true,
                NewVersion = rollbackVersion,
                BackupVersion = backupVersion,
                Duration = sw.Elapsed,
                MethodUsed = RollbackMethod.DirectRestore
            };
        }
        catch (Exception ex)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    public async Task<RollbackPreview> PreviewRollbackAsync(
        DatasetId datasetId,
        DatasetVersionId targetVersion,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = await GetCurrentVersionAsync(datasetId, cancellationToken);
        if (currentVersion == null)
        {
            return new RollbackPreview
            {
                TargetVersionId = targetVersion,
                Warnings = ["No current version found"]
            };
        }

        var diff = await DiffAsync(currentVersion.Id, targetVersion, null, cancellationToken);
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);
        var targetVer = await GetVersionAsync(targetVersion, cancellationToken);
        var versionsToRevert = versions.Count(v => v.VersionNumber > (targetVer?.VersionNumber ?? 0));

        return new RollbackPreview
        {
            CurrentVersionId = currentVersion.Id,
            TargetVersionId = targetVersion,
            VersionsToRevert = versionsToRevert,
            PredictedChanges = diff,
            RecommendedMethod = RollbackMethod.DirectRestore,
            EstimatedDuration = TimeSpan.FromSeconds(versionsToRevert * 2)
        };
    }

    public async Task<RollbackValidation> ValidateRollbackAsync(
        DatasetId datasetId,
        DatasetVersionId targetVersion,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        var version = await GetVersionAsync(targetVersion, cancellationToken);
        var versionExists = version != null;

        if (!versionExists)
        {
            issues.Add("Target version does not exist");
        }

        var dataAvailable = false;
        var integrityVerified = false;

        if (versionExists && version != null)
        {
            dataAvailable = await _storageProvider.ExistsAsync(version.StoragePointer, cancellationToken);

            if (!dataAvailable)
            {
                issues.Add("Version data is not available in storage");
            }
            else
            {
                var hashResult = await _storageProvider.VerifyHashAsync(
                    version.StoragePointer, version.Hash, cancellationToken);
                integrityVerified = hashResult.IsValid;

                if (!integrityVerified)
                {
                    warnings.Add("Data integrity check failed - file may be corrupted");
                }
            }

            if (version.Status == DatasetVersionStatus.Archived)
            {
                warnings.Add("Target version is archived");
            }
        }

        return new RollbackValidation
        {
            IsValid = issues.Count == 0,
            VersionExists = versionExists,
            DataAvailable = dataAvailable,
            IntegrityVerified = integrityVerified,
            Issues = issues,
            Warnings = warnings
        };
    }

    #endregion

    #region Version History

    public async Task<VersionHistory> GetVersionHistoryAsync(
        DatasetId datasetId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);

        if (limit.HasValue)
        {
            versions = versions.OrderByDescending(v => v.VersionNumber).Take(limit.Value).ToList();
        }

        var currentVersion = versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var totalStorage = await _storageProvider.GetTotalStorageAsync(datasetId, cancellationToken);

        var fullSnapshotSize = versions
            .Where(v => v.VersionType == DatasetVersionType.FullSnapshot)
            .Sum(v => v.SizeBytes);

        var lineageNodes = versions.Select(v => new VersionLineageNode
        {
            VersionId = v.Id,
            VersionNumber = v.VersionNumber,
            ParentId = v.ParentVersionId,
            Type = v.VersionType,
            CreatedAt = v.CreatedAt,
            Label = v.Name
        }).ToList();

        return new VersionHistory
        {
            DatasetId = datasetId,
            TotalVersions = versions.Count,
            CurrentVersion = currentVersion,
            Versions = versions.OrderByDescending(v => v.VersionNumber).ToList(),
            LineageGraph = lineageNodes,
            TotalStorageUsed = totalStorage,
            StorageSavedByDeltas = fullSnapshotSize - totalStorage
        };
    }

    public async Task<DatasetVersion?> GetVersionAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_versionCache.TryGetValue(versionId, out var cached))
            {
                return cached;
            }
        }

        // Search across all datasets - this is inefficient but works for local storage
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIMRAN-DataScience",
            "datasets",
            "versions");

        if (!Directory.Exists(basePath))
        {
            return null;
        }

        foreach (var datasetDir in Directory.GetDirectories(basePath))
        {
            if (Guid.TryParse(Path.GetFileName(datasetDir), out var datasetGuid))
            {
                var datasetId = new DatasetId(datasetGuid);
                var versions = await _storageProvider.LoadAllVersionMetadataAsync(datasetId, cancellationToken);
                var version = versions.FirstOrDefault(v => v.Id == versionId);

                if (version != null)
                {
                    lock (_lock)
                    {
                        _versionCache[versionId] = version;
                    }
                    return version;
                }
            }
        }

        return null;
    }

    public async Task<DatasetVersion?> GetCurrentVersionAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);
        return versions
            .Where(v => v.Status == DatasetVersionStatus.Active)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<DatasetVersion>> GetAllVersionsAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var versions = await _storageProvider.LoadAllVersionMetadataAsync(datasetId, cancellationToken);

        lock (_lock)
        {
            foreach (var version in versions)
            {
                _versionCache[version.Id] = version;
            }
        }

        return versions;
    }

    public async Task<IReadOnlyList<DatasetVersion>> SearchVersionsAsync(
        DatasetId datasetId,
        VersionSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);

        var query = versions.AsEnumerable();

        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(v => v.CreatedAt >= criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            query = query.Where(v => v.CreatedAt <= criteria.CreatedBefore.Value);
        }

        if (!string.IsNullOrEmpty(criteria.CreatedBy))
        {
            query = query.Where(v => v.CreatedBy.Equals(criteria.CreatedBy, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.VersionType.HasValue)
        {
            query = query.Where(v => v.VersionType == criteria.VersionType.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(v => v.Status == criteria.Status.Value);
        }

        if (criteria.Tags != null && criteria.Tags.Count > 0)
        {
            query = query.Where(v => criteria.Tags.All(t =>
                v.Tags.TryGetValue(t.Key, out var val) && val == t.Value));
        }

        return query.ToList();
    }

    #endregion

    #region Version Management

    public async Task<DatasetVersion> UpdateVersionMetadataAsync(
        DatasetVersionId versionId,
        VersionMetadataUpdate update,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        var updatedVersion = version with
        {
            Name = update.Name ?? version.Name,
            Description = update.Description ?? version.Description,
            Tags = update.Tags ?? version.Tags
        };

        await _storageProvider.UpdateVersionMetadataAsync(version.DatasetId, updatedVersion, cancellationToken);

        lock (_lock)
        {
            _versionCache[versionId] = updatedVersion;
        }

        return updatedVersion;
    }

    public async Task<DatasetVersion> AddTagsAsync(
        DatasetVersionId versionId,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        var newTags = new Dictionary<string, string>(version.Tags);
        foreach (var tag in tags)
        {
            newTags[tag.Key] = tag.Value;
        }

        var updatedVersion = version with { Tags = newTags };

        await _storageProvider.UpdateVersionMetadataAsync(version.DatasetId, updatedVersion, cancellationToken);

        lock (_lock)
        {
            _versionCache[versionId] = updatedVersion;
        }

        return updatedVersion;
    }

    public async Task ArchiveVersionAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        var archivedVersion = version with { Status = DatasetVersionStatus.Archived };

        await _storageProvider.UpdateVersionMetadataAsync(version.DatasetId, archivedVersion, cancellationToken);

        lock (_lock)
        {
            _versionCache[versionId] = archivedVersion;
        }
    }

    public async Task DeleteVersionAsync(
        DatasetVersionId versionId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        if (!force)
        {
            // Check if this version is a parent of other versions
            var allVersions = await GetAllVersionsAsync(version.DatasetId, cancellationToken);
            var hasChildren = allVersions.Any(v => v.ParentVersionId == versionId);

            if (hasChildren)
            {
                throw new InvalidOperationException(
                    "Cannot delete version with dependent versions. Use force=true to override.");
            }
        }

        // Delete data file
        if (!string.IsNullOrEmpty(version.StoragePointer))
        {
            await _storageProvider.DeleteAsync(version.StoragePointer, cancellationToken);
        }

        // Delete metadata
        await _storageProvider.DeleteVersionMetadataAsync(version.DatasetId, versionId, cancellationToken);

        lock (_lock)
        {
            _versionCache.Remove(versionId);
        }
    }

    #endregion

    #region Data Access

    public async Task<string> ExportVersionAsync(
        DatasetVersionId versionId,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        return await _storageProvider.ExportToPathAsync(version.StoragePointer, outputPath, cancellationToken);
    }

    public async Task<string> GetVersionDataPathAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        return version.StoragePointer;
    }

    public async Task<HashVerificationResult> VerifyVersionIntegrityAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(versionId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found");

        return await _storageProvider.VerifyHashAsync(version.StoragePointer, version.Hash, cancellationToken);
    }

    #endregion

    #region Storage Management

    public async Task<VersionStorageStats> GetStorageStatsAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);
        var totalStorage = await _storageProvider.GetTotalStorageAsync(datasetId, cancellationToken);

        var fullSnapshots = versions.Where(v => v.VersionType == DatasetVersionType.FullSnapshot).ToList();
        var deltas = versions.Where(v => v.VersionType == DatasetVersionType.Delta).ToList();

        var fullSnapshotBytes = fullSnapshots.Sum(v => v.SizeBytes);
        var deltaBytes = deltas.Sum(v => v.SizeBytes);

        // Calculate max chain length
        var maxChainLength = 0;
        foreach (var version in versions.Where(v => v.VersionType != DatasetVersionType.FullSnapshot))
        {
            var chainLength = 0;
            var current = version;
            while (current?.ParentVersionId != null)
            {
                chainLength++;
                current = versions.FirstOrDefault(v => v.Id == current.ParentVersionId);
                if (current?.VersionType == DatasetVersionType.FullSnapshot)
                    break;
            }
            maxChainLength = Math.Max(maxChainLength, chainLength);
        }

        return new VersionStorageStats
        {
            DatasetId = datasetId,
            TotalVersions = versions.Count,
            FullSnapshots = fullSnapshots.Count,
            Deltas = deltas.Count,
            TotalStorageBytes = totalStorage,
            FullSnapshotBytes = fullSnapshotBytes,
            DeltaBytes = deltaBytes,
            StorageSaved = (fullSnapshots.Count * (fullSnapshotBytes / Math.Max(fullSnapshots.Count, 1))) - totalStorage,
            CompressionRatio = fullSnapshotBytes > 0 ? (double)totalStorage / fullSnapshotBytes : 1.0,
            MaxDeltaChainLength = maxChainLength,
            OldestVersion = versions.Min(v => v.CreatedAt),
            NewestVersion = versions.Max(v => v.CreatedAt)
        };
    }

    public async Task<StorageCleanupResult> ConsolidateDeltasAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);

        // Find delta chains that need consolidation
        var deltasConsolidated = 0;
        var bytesReclaimed = 0L;

        // This is a simplified implementation
        // A full implementation would rebuild delta chains

        sw.Stop();

        return new StorageCleanupResult
        {
            DeltasConsolidated = deltasConsolidated,
            BytesReclaimed = bytesReclaimed,
            Duration = sw.Elapsed
        };
    }

    public async Task<StorageCleanupResult> CleanupStorageAsync(
        DatasetId datasetId,
        StorageCleanupOptions options,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var versions = await GetAllVersionsAsync(datasetId, cancellationToken);
        var versionsRemoved = 0;
        var bytesReclaimed = 0L;

        var toDelete = new List<DatasetVersion>();

        if (options.DeleteArchivedVersions)
        {
            toDelete.AddRange(versions.Where(v => v.Status == DatasetVersionStatus.Archived));
        }

        if (options.KeepLastNVersions.HasValue)
        {
            var toKeep = versions
                .OrderByDescending(v => v.VersionNumber)
                .Take(options.KeepLastNVersions.Value)
                .Select(v => v.Id)
                .ToHashSet();

            toDelete.AddRange(versions.Where(v => !toKeep.Contains(v.Id) && !toDelete.Contains(v)));
        }

        if (options.DeleteOlderThan.HasValue)
        {
            toDelete.AddRange(versions.Where(v =>
                v.CreatedAt < options.DeleteOlderThan.Value && !toDelete.Contains(v)));
        }

        if (!options.DryRun)
        {
            foreach (var version in toDelete)
            {
                try
                {
                    bytesReclaimed += version.SizeBytes;
                    await DeleteVersionAsync(version.Id, force: true, cancellationToken);
                    versionsRemoved++;
                }
                catch
                {
                    // Continue with other deletions
                }
            }

            if (options.ConsolidateDeltas)
            {
                var consolidateResult = await ConsolidateDeltasAsync(datasetId, cancellationToken);
                bytesReclaimed += consolidateResult.BytesReclaimed;
            }
        }
        else
        {
            versionsRemoved = toDelete.Count;
            bytesReclaimed = toDelete.Sum(v => v.SizeBytes);
        }

        sw.Stop();

        return new StorageCleanupResult
        {
            VersionsRemoved = versionsRemoved,
            BytesReclaimed = bytesReclaimed,
            Duration = sw.Elapsed
        };
    }

    #endregion

    #region Private Helpers

    private static async Task<VersionMetadata> ExtractMetadataAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            if (lines.Length == 0)
            {
                return new VersionMetadata();
            }

            var columns = ParseCsvLine(lines[0]);
            var rowCount = lines.Length - 1;

            return new VersionMetadata
            {
                RowCount = rowCount,
                ColumnCount = columns.Count,
                ColumnNames = columns
            };
        }
        catch
        {
            return new VersionMetadata();
        }
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    #endregion
}
