using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local filesystem-based storage provider for dataset versions.
/// </summary>
public sealed class LocalDatasetStorageProvider : IDatasetStorageProvider
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();

    public LocalDatasetStorageProvider()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIMRAN-DataScience",
            "datasets",
            "versions");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Directory.CreateDirectory(_basePath);
    }

    public StorageProvider ProviderType => StorageProvider.Local;

    #region File Operations

    public async Task<string> SaveDatasetAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        var destPath = GetVersionPath(datasetId, versionId);
        var destDir = Path.GetDirectoryName(destPath)!;

        Directory.CreateDirectory(destDir);

        await using var sourceStream = File.OpenRead(sourcePath);
        await using var destStream = File.Create(destPath);
        await sourceStream.CopyToAsync(destStream, cancellationToken);

        return destPath;
    }

    public async Task<string> SaveDeltaAsync(
        DatasetId datasetId,
        DatasetDelta delta,
        byte[] deltaData,
        CancellationToken cancellationToken = default)
    {
        var deltaPath = GetDeltaPath(datasetId, delta.Id);
        var deltaDir = Path.GetDirectoryName(deltaPath)!;

        Directory.CreateDirectory(deltaDir);

        await File.WriteAllBytesAsync(deltaPath, deltaData, cancellationToken);

        return deltaPath;
    }

    public async Task<byte[]> LoadDatasetAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return await File.ReadAllBytesAsync(storagePath, cancellationToken);
    }

    public async Task<byte[]> LoadDeltaAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return await File.ReadAllBytesAsync(storagePath, cancellationToken);
    }

    public async Task<string> ExportToPathAsync(
        string storagePath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await using var sourceStream = File.OpenRead(storagePath);
        await using var destStream = File.Create(outputPath);
        await sourceStream.CopyToAsync(destStream, cancellationToken);

        return outputPath;
    }

    public Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(storagePath));
    }

    #endregion

    #region Metadata Operations

    public async Task SaveVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersion version,
        CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataPath(datasetId);
        var metadataDir = Path.GetDirectoryName(metadataPath)!;

        Directory.CreateDirectory(metadataDir);

        var allVersions = await LoadAllVersionMetadataAsync(datasetId, cancellationToken);
        var versionsList = allVersions.ToList();

        var existingIndex = versionsList.FindIndex(v => v.Id == version.Id);
        if (existingIndex >= 0)
        {
            versionsList[existingIndex] = version;
        }
        else
        {
            versionsList.Add(version);
        }

        var json = JsonSerializer.Serialize(versionsList, _jsonOptions);
        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }

    public async Task<DatasetVersion?> LoadVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        var allVersions = await LoadAllVersionMetadataAsync(datasetId, cancellationToken);
        return allVersions.FirstOrDefault(v => v.Id == versionId);
    }

    public async Task<IReadOnlyList<DatasetVersion>> LoadAllVersionMetadataAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataPath(datasetId);

        if (!File.Exists(metadataPath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var versions = JsonSerializer.Deserialize<List<DatasetVersion>>(json, _jsonOptions);
            return versions ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task UpdateVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersion version,
        CancellationToken cancellationToken = default)
    {
        await SaveVersionMetadataAsync(datasetId, version, cancellationToken);
    }

    public async Task DeleteVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default)
    {
        var allVersions = await LoadAllVersionMetadataAsync(datasetId, cancellationToken);
        var versionsList = allVersions.Where(v => v.Id != versionId).ToList();

        var metadataPath = GetMetadataPath(datasetId);
        var json = JsonSerializer.Serialize(versionsList, _jsonOptions);
        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }

    #endregion

    #region Hash Operations

    public async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string ComputeHash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<HashVerificationResult> VerifyHashAsync(
        string storagePath,
        string expectedHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualHash = await ComputeHashAsync(storagePath, cancellationToken);
            return new HashVerificationResult
            {
                IsValid = string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase),
                ExpectedHash = expectedHash,
                ActualHash = actualHash
            };
        }
        catch (Exception ex)
        {
            return new HashVerificationResult
            {
                IsValid = false,
                ExpectedHash = expectedHash,
                ActualHash = string.Empty,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Storage Management

    public Task<long> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath))
        {
            return Task.FromResult(0L);
        }

        var fileInfo = new FileInfo(storagePath);
        return Task.FromResult(fileInfo.Length);
    }

    public async Task<long> GetTotalStorageAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var datasetPath = GetDatasetVersionsPath(datasetId);

        if (!Directory.Exists(datasetPath))
        {
            return 0;
        }

        var files = Directory.GetFiles(datasetPath, "*", SearchOption.AllDirectories);
        return files.Sum(f => new FileInfo(f).Length);
    }

    public Task<IReadOnlyList<StorageFileInfo>> ListVersionFilesAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default)
    {
        var datasetPath = GetDatasetVersionsPath(datasetId);

        if (!Directory.Exists(datasetPath))
        {
            return Task.FromResult<IReadOnlyList<StorageFileInfo>>([]);
        }

        var files = Directory.GetFiles(datasetPath, "*", SearchOption.AllDirectories)
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new StorageFileInfo
                {
                    Path = f,
                    FileName = fi.Name,
                    SizeBytes = fi.Length,
                    CreatedAt = fi.CreationTimeUtc,
                    ModifiedAt = fi.LastWriteTimeUtc,
                    FileType = GetFileType(fi.Name)
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StorageFileInfo>>(files);
    }

    public async Task<StorageCleanupResult> CleanupOrphanedFilesAsync(
        DatasetId datasetId,
        IReadOnlyList<DatasetVersionId> validVersionIds,
        CancellationToken cancellationToken = default)
    {
        var files = await ListVersionFilesAsync(datasetId, cancellationToken);
        var validIds = validVersionIds.Select(v => v.Value.ToString()).ToHashSet();
        var bytesReclaimed = 0L;
        var filesRemoved = 0;

        foreach (var file in files)
        {
            if (file.FileType == StorageFileType.FullSnapshot || file.FileType == StorageFileType.Delta)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                if (!validIds.Any(id => fileName.Contains(id)))
                {
                    bytesReclaimed += file.SizeBytes;
                    filesRemoved++;
                    File.Delete(file.Path);
                }
            }
        }

        return new StorageCleanupResult
        {
            VersionsRemoved = filesRemoved,
            BytesReclaimed = bytesReclaimed,
            Duration = TimeSpan.Zero
        };
    }

    #endregion

    #region Path Management

    public string GetDatasetVersionsPath(DatasetId datasetId)
    {
        return Path.Combine(_basePath, datasetId.Value.ToString());
    }

    public string GetVersionPath(DatasetId datasetId, DatasetVersionId versionId)
    {
        return Path.Combine(
            GetDatasetVersionsPath(datasetId),
            "snapshots",
            $"{versionId.Value}.dat");
    }

    public string GetDeltaPath(DatasetId datasetId, Guid deltaId)
    {
        return Path.Combine(
            GetDatasetVersionsPath(datasetId),
            "deltas",
            $"{deltaId}.delta");
    }

    public string GetMetadataPath(DatasetId datasetId)
    {
        return Path.Combine(
            GetDatasetVersionsPath(datasetId),
            "metadata.json");
    }

    #endregion

    #region Private Helpers

    private static StorageFileType GetFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".dat" => StorageFileType.FullSnapshot,
            ".delta" => StorageFileType.Delta,
            ".json" => StorageFileType.Metadata,
            ".tmp" => StorageFileType.Temp,
            _ => StorageFileType.Unknown
        };
    }

    #endregion
}
