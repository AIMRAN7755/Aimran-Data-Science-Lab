using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of the delta engine for computing and applying dataset changes.
/// </summary>
internal sealed class LocalDeltaEngine : IDeltaEngine
{
    private readonly JsonSerializerOptions _jsonOptions;
    private const char Delimiter = ',';

    public LocalDeltaEngine()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Delta Computation

    public async Task<DeltaComputeResult> ComputeDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        DeltaComputeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        options ??= new DeltaComputeOptions();

        try
        {
            var baseLines = await File.ReadAllLinesAsync(baseFilePath, cancellationToken);
            var targetLines = await File.ReadAllLinesAsync(targetFilePath, cancellationToken);

            if (baseLines.Length == 0 || targetLines.Length == 0)
            {
                return new DeltaComputeResult
                {
                    Success = false,
                    ErrorMessage = "Base or target file is empty"
                };
            }

            var baseColumns = ParseCsvLine(baseLines[0]);
            var targetColumns = ParseCsvLine(targetLines[0]);

            var baseRows = baseLines.Skip(1).Select(ParseCsvLine).ToList();
            var targetRows = targetLines.Skip(1).Select(ParseCsvLine).ToList();

            return await ComputeDeltaFromDataAsync(
                baseRows,
                targetRows,
                targetColumns,
                options,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return new DeltaComputeResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ComputeDuration = sw.Elapsed
            };
        }
    }

    public Task<DeltaComputeResult> ComputeDeltaFromDataAsync(
        IReadOnlyList<IReadOnlyList<string>> baseRows,
        IReadOnlyList<IReadOnlyList<string>> targetRows,
        IReadOnlyList<string> columns,
        DeltaComputeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        options ??= new DeltaComputeOptions();

        try
        {
            var rowOperations = new List<DeltaRowOperation>();
            var stats = new DeltaComputeStats
            {
                RowsCompared = Math.Max(baseRows.Count, targetRows.Count)
            };

            // Build index for base rows if primary key specified
            Dictionary<string, int>? baseIndex = null;
            Dictionary<string, int>? targetIndex = null;
            int keyColumnIndex = -1;

            if (!string.IsNullOrEmpty(options.PrimaryKeyColumn))
            {
                keyColumnIndex = columns.ToList().IndexOf(options.PrimaryKeyColumn);
                if (keyColumnIndex >= 0)
                {
                    baseIndex = BuildRowIndex(baseRows, keyColumnIndex);
                    targetIndex = BuildRowIndex(targetRows, keyColumnIndex);
                }
            }

            int rowsMatched = 0;
            int rowsAdded = 0;
            int rowsRemoved = 0;
            int rowsModified = 0;

            if (keyColumnIndex >= 0 && baseIndex != null && targetIndex != null)
            {
                // Key-based comparison
                foreach (var (key, targetIdx) in targetIndex)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (baseIndex.TryGetValue(key, out var baseIdx))
                    {
                        // Row exists in both - check for modifications
                        var changes = CompareRows(baseRows[baseIdx], targetRows[targetIdx], columns);
                        if (changes.Count > 0)
                        {
                            rowsModified++;
                            rowOperations.Add(new DeltaRowOperation
                            {
                                Type = DeltaOperationType.Update,
                                RowIndex = targetIdx,
                                RowKey = key,
                                NewValues = targetRows[targetIdx]
                                    .Select((v, i) => (v, i))
                                    .ToDictionary(x => columns[x.i], x => (string?)x.v),
                                OldValues = baseRows[baseIdx]
                                    .Select((v, i) => (v, i))
                                    .ToDictionary(x => columns[x.i], x => (string?)x.v),
                                ModifiedColumns = changes
                            });
                        }
                        else
                        {
                            rowsMatched++;
                        }
                    }
                    else
                    {
                        // New row
                        rowsAdded++;
                        rowOperations.Add(new DeltaRowOperation
                        {
                            Type = DeltaOperationType.Insert,
                            RowIndex = targetIdx,
                            RowKey = key,
                            NewValues = targetRows[targetIdx]
                                .Select((v, i) => (v, i))
                                .ToDictionary(x => columns[x.i], x => (string?)x.v)
                        });
                    }
                }

                // Find deleted rows
                foreach (var (key, baseIdx) in baseIndex)
                {
                    if (!targetIndex.ContainsKey(key))
                    {
                        rowsRemoved++;
                        rowOperations.Add(new DeltaRowOperation
                        {
                            Type = DeltaOperationType.Delete,
                            RowIndex = baseIdx,
                            RowKey = key,
                            OldValues = baseRows[baseIdx]
                                .Select((v, i) => (v, i))
                                .ToDictionary(x => columns[x.i], x => (string?)x.v)
                        });
                    }
                }
            }
            else
            {
                // Index-based comparison
                var maxRows = Math.Max(baseRows.Count, targetRows.Count);

                for (int i = 0; i < maxRows; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (i >= baseRows.Count)
                    {
                        // Added row
                        rowsAdded++;
                        rowOperations.Add(new DeltaRowOperation
                        {
                            Type = DeltaOperationType.Insert,
                            RowIndex = i,
                            NewValues = targetRows[i]
                                .Select((v, idx) => (v, idx))
                                .Where(x => x.idx < columns.Count)
                                .ToDictionary(x => columns[x.idx], x => (string?)x.v)
                        });
                    }
                    else if (i >= targetRows.Count)
                    {
                        // Deleted row
                        rowsRemoved++;
                        rowOperations.Add(new DeltaRowOperation
                        {
                            Type = DeltaOperationType.Delete,
                            RowIndex = i,
                            OldValues = baseRows[i]
                                .Select((v, idx) => (v, idx))
                                .Where(x => x.idx < columns.Count)
                                .ToDictionary(x => columns[x.idx], x => (string?)x.v)
                        });
                    }
                    else
                    {
                        // Compare rows
                        var changes = CompareRows(baseRows[i], targetRows[i], columns);
                        if (changes.Count > 0)
                        {
                            rowsModified++;
                            rowOperations.Add(new DeltaRowOperation
                            {
                                Type = DeltaOperationType.Update,
                                RowIndex = i,
                                NewValues = targetRows[i]
                                    .Select((v, idx) => (v, idx))
                                    .Where(x => x.idx < columns.Count)
                                    .ToDictionary(x => columns[x.idx], x => (string?)x.v),
                                OldValues = baseRows[i]
                                    .Select((v, idx) => (v, idx))
                                    .Where(x => x.idx < columns.Count)
                                    .ToDictionary(x => columns[x.idx], x => (string?)x.v),
                                ModifiedColumns = changes
                            });
                        }
                        else
                        {
                            rowsMatched++;
                        }
                    }
                }
            }

            var operations = new DeltaOperations
            {
                RowOperations = rowOperations,
                SchemaOperations = []
            };

            var delta = new DatasetDelta
            {
                Type = DeltaType.RowLevel,
                Operations = operations
            };

            var deltaBytes = SerializeDelta(delta);
            if (options.Compress)
            {
                deltaBytes = CompressDelta(deltaBytes);
            }

            sw.Stop();

            return Task.FromResult(new DeltaComputeResult
            {
                Success = true,
                Delta = delta with
                {
                    DeltaSizeBytes = deltaBytes.Length,
                    OriginalSizeBytes = targetRows.Count * columns.Count * 10 // Estimate
                },
                DeltaData = deltaBytes,
                OriginalSize = targetRows.Count * columns.Count * 10,
                DeltaSize = deltaBytes.Length,
                ComputeDuration = sw.Elapsed,
                Stats = new DeltaComputeStats
                {
                    RowsCompared = Math.Max(baseRows.Count, targetRows.Count),
                    RowsMatched = rowsMatched,
                    RowsAdded = rowsAdded,
                    RowsRemoved = rowsRemoved,
                    RowsModified = rowsModified,
                    OperationsGenerated = rowOperations.Count
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new DeltaComputeResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ComputeDuration = sw.Elapsed
            });
        }
    }

    public async Task<DeltaRecommendation> ShouldUseDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        VersionStorageConfig config,
        CancellationToken cancellationToken = default)
    {
        var baseInfo = new FileInfo(baseFilePath);
        var targetInfo = new FileInfo(targetFilePath);

        if (!baseInfo.Exists || !targetInfo.Exists)
        {
            return new DeltaRecommendation
            {
                UseDelta = false,
                Reason = "File not found",
                EstimatedFullSize = targetInfo.Exists ? targetInfo.Length : 0
            };
        }

        // Compute delta to determine size
        var deltaResult = await ComputeDeltaAsync(
            baseFilePath,
            targetFilePath,
            new DeltaComputeOptions { Compress = config.CompressDeltas },
            cancellationToken);

        if (!deltaResult.Success || deltaResult.DeltaData == null)
        {
            return new DeltaRecommendation
            {
                UseDelta = false,
                Reason = "Failed to compute delta",
                EstimatedFullSize = targetInfo.Length
            };
        }

        var deltaSize = deltaResult.DeltaSize;
        var fullSize = targetInfo.Length;
        var ratio = (double)deltaSize / fullSize;
        var changePercent = deltaResult.Stats.OperationsGenerated > 0
            ? (double)(deltaResult.Stats.RowsAdded + deltaResult.Stats.RowsRemoved + deltaResult.Stats.RowsModified)
              / deltaResult.Stats.RowsCompared * 100
            : 0;

        var useDelta = deltaSize < config.DeltaThresholdBytes ||
                       ratio < config.DeltaThresholdRatio;

        return new DeltaRecommendation
        {
            UseDelta = useDelta,
            Reason = useDelta
                ? $"Delta is {ratio:P1} of full size"
                : $"Delta ({ratio:P1}) exceeds threshold ({config.DeltaThresholdRatio:P0})",
            EstimatedDeltaSize = deltaSize,
            EstimatedFullSize = fullSize,
            EstimatedSavings = fullSize - deltaSize,
            ChangePercentage = changePercent
        };
    }

    #endregion

    #region Delta Application

    public async Task<DeltaApplyResult> ApplyDeltaAsync(
        string baseFilePath,
        DatasetDelta delta,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var baseLines = await File.ReadAllLinesAsync(baseFilePath, cancellationToken);
            if (baseLines.Length == 0)
            {
                return new DeltaApplyResult
                {
                    Success = false,
                    ErrorMessage = "Base file is empty"
                };
            }

            var columns = ParseCsvLine(baseLines[0]);
            var rows = baseLines.Skip(1).Select(ParseCsvLine).ToList();

            // Deserialize operations from delta data if needed
            var workingDelta = delta.Operations.TotalOperations > 0
                ? delta
                : DeserializeDelta(DecompressDelta(deltaData));

            int opsApplied = 0;

            // Apply deletions first (in reverse order to maintain indices)
            var deletions = workingDelta.Operations.RowOperations
                .Where(op => op.Type == DeltaOperationType.Delete)
                .OrderByDescending(op => op.RowIndex)
                .ToList();

            foreach (var deletion in deletions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (deletion.RowIndex < rows.Count)
                {
                    rows.RemoveAt(deletion.RowIndex);
                    opsApplied++;
                }
            }

            // Apply updates
            foreach (var update in workingDelta.Operations.RowOperations.Where(op => op.Type == DeltaOperationType.Update))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.RowIndex < rows.Count && update.NewValues != null)
                {
                    var newRow = new List<string>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (update.NewValues.TryGetValue(columns[i], out var newValue))
                        {
                            newRow.Add(newValue ?? string.Empty);
                        }
                        else if (i < rows[update.RowIndex].Count)
                        {
                            newRow.Add(rows[update.RowIndex][i]);
                        }
                        else
                        {
                            newRow.Add(string.Empty);
                        }
                    }
                    rows[update.RowIndex] = newRow;
                    opsApplied++;
                }
            }

            // Apply insertions (sorted by index)
            var insertions = workingDelta.Operations.RowOperations
                .Where(op => op.Type == DeltaOperationType.Insert)
                .OrderBy(op => op.RowIndex)
                .ToList();

            foreach (var insertion in insertions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (insertion.NewValues != null)
                {
                    var newRow = columns.Select(c =>
                        insertion.NewValues.TryGetValue(c, out var v) ? v ?? string.Empty : string.Empty
                    ).ToList();

                    if (insertion.RowIndex <= rows.Count)
                    {
                        rows.Insert(insertion.RowIndex, newRow);
                    }
                    else
                    {
                        rows.Add(newRow);
                    }
                    opsApplied++;
                }
            }

            // Write output
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var outputLines = new List<string> { string.Join(Delimiter, columns) };
            outputLines.AddRange(rows.Select(r => string.Join(Delimiter, r.Select(EscapeCsvField))));

            await File.WriteAllLinesAsync(outputPath, outputLines, cancellationToken);

            var hash = await ComputeFileHashAsync(outputPath, cancellationToken);

            sw.Stop();

            return new DeltaApplyResult
            {
                Success = true,
                OutputPath = outputPath,
                Hash = hash,
                OperationsApplied = opsApplied,
                RowsInResult = rows.Count,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new DeltaApplyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    public async Task<DeltaApplyResult> ApplyDeltaChainAsync(
        string baseFilePath,
        DeltaChain chain,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (chain.Deltas.Count == 0)
        {
            // No deltas, just copy base
            File.Copy(baseFilePath, outputPath, true);
            return new DeltaApplyResult
            {
                Success = true,
                OutputPath = outputPath,
                Duration = sw.Elapsed
            };
        }

        var tempPath = Path.GetTempFileName();
        var currentPath = baseFilePath;
        var totalOps = 0;

        try
        {
            for (int i = 0; i < chain.Deltas.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var delta = chain.Deltas[i];
                var deltaData = await deltaLoader(delta);

                var isLast = i == chain.Deltas.Count - 1;
                var targetPath = isLast ? outputPath : Path.GetTempFileName();

                var result = await ApplyDeltaAsync(currentPath, delta, deltaData, targetPath, cancellationToken);

                if (!result.Success)
                {
                    return result;
                }

                totalOps += result.OperationsApplied;

                // Clean up intermediate files
                if (currentPath != baseFilePath && File.Exists(currentPath))
                {
                    File.Delete(currentPath);
                }

                currentPath = targetPath;
            }

            var hash = await ComputeFileHashAsync(outputPath, cancellationToken);
            sw.Stop();

            return new DeltaApplyResult
            {
                Success = true,
                OutputPath = outputPath,
                Hash = hash,
                OperationsApplied = totalOps,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new DeltaApplyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
        finally
        {
            // Clean up temp files
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task<DeltaApplyResult> ApplyReverseDeltaAsync(
        string currentFilePath,
        DatasetDelta delta,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var currentLines = await File.ReadAllLinesAsync(currentFilePath, cancellationToken);
            if (currentLines.Length == 0)
            {
                return new DeltaApplyResult
                {
                    Success = false,
                    ErrorMessage = "Current file is empty"
                };
            }

            var columns = ParseCsvLine(currentLines[0]);
            var rows = currentLines.Skip(1).Select(ParseCsvLine).ToList();

            var workingDelta = delta.Operations.TotalOperations > 0
                ? delta
                : DeserializeDelta(DecompressDelta(deltaData));

            int opsApplied = 0;

            // Reverse insertions become deletions
            var insertions = workingDelta.Operations.RowOperations
                .Where(op => op.Type == DeltaOperationType.Insert)
                .OrderByDescending(op => op.RowIndex)
                .ToList();

            foreach (var insertion in insertions)
            {
                if (insertion.RowIndex < rows.Count)
                {
                    rows.RemoveAt(insertion.RowIndex);
                    opsApplied++;
                }
            }

            // Reverse updates - restore old values
            foreach (var update in workingDelta.Operations.RowOperations.Where(op => op.Type == DeltaOperationType.Update))
            {
                if (update.RowIndex < rows.Count && update.OldValues != null)
                {
                    var oldRow = new List<string>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (update.OldValues.TryGetValue(columns[i], out var oldValue))
                        {
                            oldRow.Add(oldValue ?? string.Empty);
                        }
                        else if (i < rows[update.RowIndex].Count)
                        {
                            oldRow.Add(rows[update.RowIndex][i]);
                        }
                        else
                        {
                            oldRow.Add(string.Empty);
                        }
                    }
                    rows[update.RowIndex] = oldRow;
                    opsApplied++;
                }
            }

            // Reverse deletions become insertions
            var deletions = workingDelta.Operations.RowOperations
                .Where(op => op.Type == DeltaOperationType.Delete)
                .OrderBy(op => op.RowIndex)
                .ToList();

            foreach (var deletion in deletions)
            {
                if (deletion.OldValues != null)
                {
                    var oldRow = columns.Select(c =>
                        deletion.OldValues.TryGetValue(c, out var v) ? v ?? string.Empty : string.Empty
                    ).ToList();

                    if (deletion.RowIndex <= rows.Count)
                    {
                        rows.Insert(deletion.RowIndex, oldRow);
                    }
                    else
                    {
                        rows.Add(oldRow);
                    }
                    opsApplied++;
                }
            }

            // Write output
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var outputLines = new List<string> { string.Join(Delimiter, columns) };
            outputLines.AddRange(rows.Select(r => string.Join(Delimiter, r.Select(EscapeCsvField))));

            await File.WriteAllLinesAsync(outputPath, outputLines, cancellationToken);

            var hash = await ComputeFileHashAsync(outputPath, cancellationToken);
            sw.Stop();

            return new DeltaApplyResult
            {
                Success = true,
                OutputPath = outputPath,
                Hash = hash,
                OperationsApplied = opsApplied,
                RowsInResult = rows.Count,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new DeltaApplyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    #endregion

    #region Delta Serialization

    public byte[] SerializeDelta(DatasetDelta delta)
    {
        var json = JsonSerializer.Serialize(delta, _jsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    public DatasetDelta DeserializeDelta(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<DatasetDelta>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize delta");
    }

    public byte[] CompressDelta(byte[] deltaData)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzipStream.Write(deltaData, 0, deltaData.Length);
        }
        return outputStream.ToArray();
    }

    public byte[] DecompressDelta(byte[] compressedData)
    {
        try
        {
            using var inputStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
        catch (InvalidDataException)
        {
            // Data might not be compressed
            return compressedData;
        }
    }

    #endregion

    #region Delta Chain Management

    public async Task<DeltaChain> BuildDeltaChainAsync(
        DatasetVersionId baseSnapshotId,
        DatasetVersionId targetVersionId,
        Func<DatasetVersionId, Task<DatasetVersion>> versionLoader,
        CancellationToken cancellationToken = default)
    {
        var deltas = new List<DatasetDelta>();
        var currentId = targetVersionId;

        while (currentId != baseSnapshotId)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var version = await versionLoader(currentId);
            if (version?.ParentVersionId == null)
            {
                break;
            }

            // Create placeholder delta for chain
            deltas.Insert(0, new DatasetDelta
            {
                BaseVersionId = version.ParentVersionId.Value,
                TargetVersionId = currentId,
                StoragePath = version.StoragePointer
            });

            currentId = version.ParentVersionId.Value;

            // Safety limit
            if (deltas.Count > 100)
            {
                break;
            }
        }

        return new DeltaChain
        {
            BaseSnapshotId = baseSnapshotId,
            Deltas = deltas
        };
    }

    public async Task<DeltaConsolidateResult> ConsolidateDeltaChainAsync(
        DeltaChain chain,
        string baseFilePath,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        CancellationToken cancellationToken = default)
    {
        if (chain.Deltas.Count <= 1)
        {
            return new DeltaConsolidateResult
            {
                Success = true,
                DeltasConsolidated = 0
            };
        }

        var tempOutput = Path.GetTempFileName();
        try
        {
            // Apply entire chain to get final state
            var applyResult = await ApplyDeltaChainAsync(
                baseFilePath, chain, deltaLoader, tempOutput, cancellationToken);

            if (!applyResult.Success)
            {
                return new DeltaConsolidateResult
                {
                    Success = false,
                    ErrorMessage = applyResult.ErrorMessage
                };
            }

            // Compute single delta from base to final
            var deltaResult = await ComputeDeltaAsync(
                baseFilePath, tempOutput, new DeltaComputeOptions { Compress = true }, cancellationToken);

            if (!deltaResult.Success)
            {
                return new DeltaConsolidateResult
                {
                    Success = false,
                    ErrorMessage = deltaResult.ErrorMessage
                };
            }

            return new DeltaConsolidateResult
            {
                Success = true,
                ConsolidatedDelta = deltaResult.Delta,
                DeltaData = deltaResult.DeltaData,
                DeltasConsolidated = chain.Deltas.Count,
                OriginalTotalSize = chain.TotalDeltaSize,
                ConsolidatedSize = deltaResult.DeltaSize
            };
        }
        finally
        {
            if (File.Exists(tempOutput))
            {
                File.Delete(tempOutput);
            }
        }
    }

    public async Task<DeltaOptimizeResult> OptimizeDeltaChainAsync(
        DeltaChain chain,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        DeltaOptimizeOptions options,
        CancellationToken cancellationToken = default)
    {
        if (chain.ChainLength <= options.MaxChainLength)
        {
            return new DeltaOptimizeResult
            {
                Success = true,
                OriginalChainLength = chain.ChainLength,
                OptimizedChainLength = chain.ChainLength,
                OptimizedDeltas = chain.Deltas,
                StorageSaved = 0
            };
        }

        // Find small consecutive deltas to merge
        var optimizedDeltas = new List<DatasetDelta>(chain.Deltas);
        long storageSaved = 0;

        // Simple optimization: keep every N-th delta
        var keepInterval = Math.Max(1, chain.ChainLength / options.MaxChainLength);
        var keptDeltas = new List<DatasetDelta>();

        for (int i = 0; i < chain.Deltas.Count; i++)
        {
            if (i == 0 || i == chain.Deltas.Count - 1 || i % keepInterval == 0)
            {
                keptDeltas.Add(chain.Deltas[i]);
            }
            else
            {
                storageSaved += chain.Deltas[i].DeltaSizeBytes;
            }
        }

        return new DeltaOptimizeResult
        {
            Success = true,
            OriginalChainLength = chain.ChainLength,
            OptimizedChainLength = keptDeltas.Count,
            StorageSaved = storageSaved,
            OptimizedDeltas = keptDeltas
        };
    }

    #endregion

    #region Diff Operations

    public async Task<DatasetDiffResult> DiffDatasetsAsync(
        string fromFilePath,
        string toFilePath,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        options ??= new DiffOptions();

        var fromLines = await File.ReadAllLinesAsync(fromFilePath, cancellationToken);
        var toLines = await File.ReadAllLinesAsync(toFilePath, cancellationToken);

        if (fromLines.Length == 0 && toLines.Length == 0)
        {
            return new DatasetDiffResult { Summary = "Both datasets are empty" };
        }

        var fromColumns = fromLines.Length > 0 ? ParseCsvLine(fromLines[0]) : [];
        var toColumns = toLines.Length > 0 ? ParseCsvLine(toLines[0]) : [];

        var fromRows = fromLines.Skip(1).Select(ParseCsvLine).ToList();
        var toRows = toLines.Skip(1).Select(ParseCsvLine).ToList();

        // Schema diff
        var schemaDiff = ComputeSchemaDiff(fromColumns, toColumns, options);

        // Row diff
        var deltaResult = await ComputeDeltaFromDataAsync(
            fromRows, toRows, toColumns,
            new DeltaComputeOptions { PrimaryKeyColumn = options.PrimaryKeyColumn },
            cancellationToken);

        var stats = deltaResult.Stats;

        // Sample changes
        var sampleAdded = new List<RowChange>();
        var sampleRemoved = new List<RowChange>();
        var sampleModified = new List<RowModification>();

        if (options.IncludeSampleChanges && deltaResult.Delta != null)
        {
            foreach (var op in deltaResult.Delta.Operations.RowOperations.Take(options.MaxSampleRows))
            {
                switch (op.Type)
                {
                    case DeltaOperationType.Insert when op.NewValues != null:
                        sampleAdded.Add(new RowChange
                        {
                            RowIndex = op.RowIndex,
                            RowKey = op.RowKey ?? op.RowIndex.ToString(),
                            Values = op.NewValues
                        });
                        break;

                    case DeltaOperationType.Delete when op.OldValues != null:
                        sampleRemoved.Add(new RowChange
                        {
                            RowIndex = op.RowIndex,
                            RowKey = op.RowKey ?? op.RowIndex.ToString(),
                            Values = op.OldValues
                        });
                        break;

                    case DeltaOperationType.Update when op.ModifiedColumns != null:
                        sampleModified.Add(new RowModification
                        {
                            RowIndex = op.RowIndex,
                            RowKey = op.RowKey ?? op.RowIndex.ToString(),
                            ColumnsAffected = op.ModifiedColumns.Count,
                            Changes = op.ModifiedColumns.Select(col => new Models.DatasetVersioning.ValueChange
                            {
                                RowIndex = op.RowIndex,
                                ColumnName = col,
                                OldValue = op.OldValues?.GetValueOrDefault(col),
                                NewValue = op.NewValues?.GetValueOrDefault(col),
                                ChangeType = ValueChangeType.Modified
                            }).ToList()
                        });
                        break;
                }
            }
        }

        var totalFromCells = fromRows.Count * fromColumns.Count;
        var changePercentage = totalFromCells > 0
            ? (double)(stats.RowsAdded + stats.RowsRemoved + stats.RowsModified) / Math.Max(fromRows.Count, 1) * 100
            : 0;

        sw.Stop();

        return new DatasetDiffResult
        {
            FromVersionNumber = 0,
            ToVersionNumber = 0,
            ComparedAt = DateTime.UtcNow,
            ComputeDuration = sw.Elapsed,
            RowsAdded = stats.RowsAdded,
            RowsRemoved = stats.RowsRemoved,
            RowsModified = stats.RowsModified,
            RowsUnchanged = stats.RowsMatched,
            TotalRowsBefore = fromRows.Count,
            TotalRowsAfter = toRows.Count,
            CellsModified = stats.RowsModified * toColumns.Count,
            ChangePercentage = changePercentage,
            SchemaDiff = schemaDiff,
            SampleAddedRows = sampleAdded,
            SampleRemovedRows = sampleRemoved,
            SampleModifiedRows = sampleModified,
            Statistics = new DiffStatistics
            {
                DataSimilarity = 100 - changePercentage,
                SchemaSimilarity = schemaDiff.HasSchemaChanges ? 80 : 100,
                OverallSimilarity = (100 - changePercentage) * (schemaDiff.HasSchemaChanges ? 0.8 : 1.0)
            },
            Summary = BuildDiffSummary(stats, schemaDiff),
            Severity = DetermineSeverity(changePercentage, schemaDiff)
        };
    }

    public DatasetDiffResult DeltaToDiff(
        DatasetDelta delta,
        int fromRowCount,
        int toRowCount)
    {
        var ops = delta.Operations.RowOperations;

        var added = ops.Count(o => o.Type == DeltaOperationType.Insert);
        var removed = ops.Count(o => o.Type == DeltaOperationType.Delete);
        var modified = ops.Count(o => o.Type == DeltaOperationType.Update);

        var changePercent = fromRowCount > 0
            ? (double)(added + removed + modified) / fromRowCount * 100
            : 0;

        return new DatasetDiffResult
        {
            ComparedAt = DateTime.UtcNow,
            RowsAdded = added,
            RowsRemoved = removed,
            RowsModified = modified,
            RowsUnchanged = fromRowCount - removed - modified,
            TotalRowsBefore = fromRowCount,
            TotalRowsAfter = toRowCount,
            ChangePercentage = changePercent,
            Summary = $"Added: {added}, Removed: {removed}, Modified: {modified}",
            Severity = DetermineSeverity(changePercent, new SchemaDiff())
        };
    }

    #endregion

    #region Private Helpers

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
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

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static Dictionary<string, int> BuildRowIndex(IReadOnlyList<IReadOnlyList<string>> rows, int keyColumnIndex)
    {
        var index = new Dictionary<string, int>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (keyColumnIndex < rows[i].Count)
            {
                var key = rows[i][keyColumnIndex];
                if (!index.ContainsKey(key))
                {
                    index[key] = i;
                }
            }
        }
        return index;
    }

    private static IReadOnlyList<string> CompareRows(
        IReadOnlyList<string> baseRow,
        IReadOnlyList<string> targetRow,
        IReadOnlyList<string> columns)
    {
        var changes = new List<string>();
        var maxCols = Math.Min(Math.Min(baseRow.Count, targetRow.Count), columns.Count);

        for (int i = 0; i < maxCols; i++)
        {
            if (!string.Equals(baseRow[i], targetRow[i], StringComparison.Ordinal))
            {
                changes.Add(columns[i]);
            }
        }

        return changes;
    }

    private static SchemaDiff ComputeSchemaDiff(
        IReadOnlyList<string> fromColumns,
        IReadOnlyList<string> toColumns,
        DiffOptions options)
    {
        var fromSet = fromColumns.ToHashSet();
        var toSet = toColumns.ToHashSet();

        var added = toColumns.Where(c => !fromSet.Contains(c))
            .Select((c, i) => new ColumnSchema { Name = c, Index = toColumns.ToList().IndexOf(c) })
            .ToList();

        var removed = fromColumns.Where(c => !toSet.Contains(c))
            .Select((c, i) => new ColumnSchema { Name = c, Index = fromColumns.ToList().IndexOf(c) })
            .ToList();

        // Detect renames if enabled
        var renames = new List<ColumnRename>();
        if (options.DetectRenames && removed.Count > 0 && added.Count > 0)
        {
            // Simple heuristic: same position suggests rename
            foreach (var rem in removed.ToList())
            {
                var possibleRename = added.FirstOrDefault(a => a.Index == rem.Index);
                if (possibleRename != null)
                {
                    renames.Add(new ColumnRename
                    {
                        OldName = rem.Name,
                        NewName = possibleRename.Name,
                        Confidence = 0.7
                    });
                    removed.Remove(rem);
                    added.Remove(possibleRename);
                }
            }
        }

        return new SchemaDiff
        {
            ColumnsAdded = added,
            ColumnsRemoved = removed,
            ColumnsRenamed = renames
        };
    }

    private static string BuildDiffSummary(DeltaComputeStats stats, SchemaDiff schemaDiff)
    {
        var parts = new List<string>();

        if (stats.RowsAdded > 0) parts.Add($"{stats.RowsAdded} rows added");
        if (stats.RowsRemoved > 0) parts.Add($"{stats.RowsRemoved} rows removed");
        if (stats.RowsModified > 0) parts.Add($"{stats.RowsModified} rows modified");
        if (schemaDiff.HasSchemaChanges) parts.Add("schema changed");

        return parts.Count > 0 ? string.Join(", ", parts) : "No changes detected";
    }

    private static DiffSeverity DetermineSeverity(double changePercent, SchemaDiff schemaDiff)
    {
        if (schemaDiff.ColumnsRemoved.Count > 0 || changePercent > 50)
            return DiffSeverity.Breaking;
        if (schemaDiff.HasSchemaChanges || changePercent > 20)
            return DiffSeverity.Major;
        if (changePercent > 5)
            return DiffSeverity.Moderate;
        if (changePercent > 0)
            return DiffSeverity.Minor;
        return DiffSeverity.None;
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion
}
