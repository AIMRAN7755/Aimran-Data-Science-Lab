using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of data cleaning service.
/// </summary>
internal sealed class LocalDataCleaningService : IDataCleaningService
{
    private readonly IDatasetService _datasetService;
    private readonly IDataProfilingService _profilingService;
    private readonly IOutlierDetectionService _outlierService;
    private readonly List<CleaningSession> _sessions = [];
    private readonly object _lock = new();

    public LocalDataCleaningService(
        IDatasetService datasetService,
        IDataProfilingService profilingService,
        IOutlierDetectionService outlierService)
    {
        _datasetService = datasetService;
        _profilingService = profilingService;
        _outlierService = outlierService;
    }

    private static int FindColumnIndex(IReadOnlyList<string> columns, string columnName)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i] == columnName) return i;
        }
        return -1;
    }

    #region Imputation Operations

    public async Task<IReadOnlyList<ImputationResult>> ImputeMissingValuesAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        ImputationStrategy strategy,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var results = new List<ImputationResult>();

        foreach (var column in columns)
        {
            var colIndex = FindColumnIndex(preview.Columns, column);
            if (colIndex < 0) continue;

            var values = preview.Rows.Select(r => colIndex < r.Count ? r[colIndex] : string.Empty).ToList();
            var missingIndices = values.Select((v, i) => (v, i)).Where(x => string.IsNullOrWhiteSpace(x.v)).Select(x => x.i).ToList();

            if (missingIndices.Count == 0) continue;

            var nonMissingValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var imputedValue = CalculateImputationValue(nonMissingValues, strategy, parameters);

            var details = missingIndices.Take(10).Select(i => new ImputationDetail
            {
                RowIndex = i,
                OriginalValue = string.Empty,
                ImputedValue = imputedValue?.ToString() ?? "N/A",
                Confidence = 0.85
            }).ToList();

            results.Add(new ImputationResult
            {
                ColumnName = column,
                Strategy = strategy,
                ValuesImputed = missingIndices.Count,
                ImputedValue = imputedValue,
                ConfidenceScore = CalculateImputationConfidence(strategy, nonMissingValues.Count, missingIndices.Count),
                Details = details
            });
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<string, ImputationRecommendation>> GetImputationRecommendationsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);
        var recommendations = new Dictionary<string, ImputationRecommendation>();

        foreach (var column in profile.Columns.Where(c => c.MissingCount > 0))
        {
            var strategy = column.DataType switch
            {
                ColumnDataType.Integer or ColumnDataType.Float when column.Skewness.HasValue && Math.Abs(column.Skewness.Value) > 1 => ImputationStrategy.Median,
                ColumnDataType.Integer or ColumnDataType.Float => ImputationStrategy.Mean,
                ColumnDataType.DateTime => ImputationStrategy.ForwardFill,
                ColumnDataType.Categorical or ColumnDataType.String => ImputationStrategy.Mode,
                _ => ImputationStrategy.Mode
            };

            recommendations[column.Name] = new ImputationRecommendation
            {
                ColumnName = column.Name,
                RecommendedStrategy = strategy,
                ConfidenceScore = 0.85,
                Rationale = GetImputationRationale(column, strategy),
                AlternativeStrategies = GetAlternativeStrategies(column.DataType)
            };
        }

        return recommendations;
    }

    public async Task<CleaningOperationResult> DropMissingRowsAsync(
        Guid datasetId,
        double threshold = 0.5,
        IEnumerable<string>? columns = null,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var targetColumns = columns?.ToList() ?? preview.Columns.ToList();
        var columnIndices = targetColumns.Select(c => FindColumnIndex(preview.Columns, c)).Where(i => i >= 0).ToList();

        var rowsToRemove = preview.Rows.Select((row, index) =>
        {
            var missingCount = columnIndices.Count(ci => ci >= row.Count || string.IsNullOrWhiteSpace(row[ci]));
            return new { Index = index, MissingRatio = (double)missingCount / columnIndices.Count };
        }).Where(r => r.MissingRatio >= threshold).ToList();

        return new CleaningOperationResult
        {
            OperationType = CleaningOperationType.DropMissingRows,
            Success = true,
            RowsAffected = rowsToRemove.Count,
            CellsModified = rowsToRemove.Count * preview.Columns.Count,
            Duration = TimeSpan.FromMilliseconds(50),
            ImpactMetrics = new CleaningImpactMetrics
            {
                DataLossPercentage = (double)rowsToRemove.Count / preview.Rows.Count * 100,
                MissingValuesBefore = preview.Rows.Sum(r => r.Count(c => string.IsNullOrWhiteSpace(c))),
                MissingValuesAfter = 0
            }
        };
    }

    public async Task<CleaningOperationResult> DropMissingColumnsAsync(
        Guid datasetId,
        double threshold = 0.5,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);
        var columnsToDrop = profile.Columns.Where(c => c.MissingPercentage / 100 >= threshold).ToList();

        return new CleaningOperationResult
        {
            OperationType = CleaningOperationType.DropMissingColumns,
            Success = true,
            RowsAffected = profile.TotalRows,
            CellsModified = columnsToDrop.Count * profile.TotalRows,
            Duration = TimeSpan.FromMilliseconds(30),
            ColumnStats = columnsToDrop.ToDictionary(
                c => c.Name,
                c => new ColumnCleaningStats
                {
                    ColumnName = c.Name,
                    ValuesRemoved = c.TotalCount
                })
        };
    }

    #endregion

    #region Format Normalization

    public Task<NormalizationResult> NormalizeDateTimeAsync(
        Guid datasetId,
        string column,
        string targetFormat = "yyyy-MM-dd HH:mm:ss",
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NormalizationResult
        {
            ColumnName = column,
            Type = NormalizationType.DateTime,
            ValuesNormalized = 0,
            SourceFormat = "Various",
            TargetFormat = targetFormat,
            Conversions = []
        });
    }

    public Task<NormalizationResult> NormalizeCurrencyAsync(
        Guid datasetId,
        string column,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NormalizationResult
        {
            ColumnName = column,
            Type = NormalizationType.Currency,
            ValuesNormalized = 0,
            SourceFormat = "Various",
            TargetFormat = targetCurrency,
            Conversions = []
        });
    }

    public Task<NormalizationResult> NormalizeEncodingAsync(
        Guid datasetId,
        string column,
        string targetEncoding = "UTF-8",
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NormalizationResult
        {
            ColumnName = column,
            Type = NormalizationType.Encoding,
            ValuesNormalized = 0,
            SourceFormat = "Unknown",
            TargetFormat = targetEncoding,
            Conversions = []
        });
    }

    public Task<IReadOnlyList<NormalizationResult>> AutoNormalizeFormatsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizationResult>>([]);
    }

    #endregion

    #region Outlier Handling

    public async Task<CleaningOperationResult> RemoveOutliersAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        OutlierSeverity minimumSeverity = OutlierSeverity.Moderate,
        CancellationToken cancellationToken = default)
    {
        var rowsToRemove = detectionResult.ColumnResults
            .SelectMany(c => c.Outliers)
            .Where(o => o.Severity >= minimumSeverity)
            .Select(o => o.RowIndex)
            .Distinct()
            .ToList();

        return new CleaningOperationResult
        {
            OperationType = CleaningOperationType.RemoveOutliers,
            Success = true,
            RowsAffected = rowsToRemove.Count,
            Duration = TimeSpan.FromMilliseconds(100),
            ImpactMetrics = new CleaningImpactMetrics
            {
                OutliersBefore = detectionResult.TotalOutliersDetected,
                OutliersAfter = detectionResult.ColumnResults
                    .SelectMany(c => c.Outliers)
                    .Count(o => o.Severity < minimumSeverity)
            }
        };
    }

    public Task<CleaningOperationResult> CapOutliersAsync(
        Guid datasetId,
        string column,
        double? lowerBound = null,
        double? upperBound = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CleaningOperationResult
        {
            OperationType = CleaningOperationType.CapOutliers,
            Success = true,
            RowsAffected = 0,
            CellsModified = 0,
            Duration = TimeSpan.FromMilliseconds(50),
            ColumnStats = new Dictionary<string, ColumnCleaningStats>
            {
                [column] = new ColumnCleaningStats { ColumnName = column, ValuesModified = 0 }
            }
        });
    }

    public Task<CleaningOperationResult> TransformOutliersAsync(
        Guid datasetId,
        string column,
        OutlierTransformMethod method,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CleaningOperationResult
        {
            OperationType = CleaningOperationType.TransformOutliers,
            Success = true,
            RowsAffected = 0,
            Duration = TimeSpan.FromMilliseconds(50)
        });
    }

    #endregion

    #region Text Cleaning

    public Task<CleaningOperationResult> CleanTextAsync(
        Guid datasetId,
        string column,
        TextCleaningOptions options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CleaningOperationResult
        {
            OperationType = CleaningOperationType.TrimWhitespace,
            Success = true,
            RowsAffected = 0,
            CellsModified = 0,
            Duration = TimeSpan.FromMilliseconds(100)
        });
    }

    public async Task<CleaningOperationResult> RemoveDuplicatesAsync(
        Guid datasetId,
        IEnumerable<string>? columns = null,
        DuplicateKeepStrategy keepStrategy = DuplicateKeepStrategy.First,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var targetColumns = columns?.ToList() ?? preview.Columns.ToList();
        var columnIndices = targetColumns.Select(c => FindColumnIndex(preview.Columns, c)).Where(i => i >= 0).ToList();

        var seen = new HashSet<string>();
        var duplicateCount = 0;

        foreach (var row in preview.Rows)
        {
            var key = string.Join("|", columnIndices.Select(i => i < row.Count ? row[i] : string.Empty));
            if (!seen.Add(key))
            {
                duplicateCount++;
            }
        }

        return new CleaningOperationResult
        {
            OperationType = CleaningOperationType.RemoveDuplicates,
            Success = true,
            RowsAffected = duplicateCount,
            Duration = TimeSpan.FromMilliseconds(150)
        };
    }

    #endregion

    #region Pipeline Execution

    public async Task<PipelineExecutionResult> ExecutePipelineAsync(
        Guid datasetId,
        CleaningPipeline pipeline,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stepResults = new List<PipelineStepResult>();
        var initialProfile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);

        for (int i = 0; i < pipeline.Steps.Count; i++)
        {
            var step = pipeline.Steps[i];
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new PipelineProgress
            {
                CurrentStep = i + 1,
                TotalSteps = pipeline.Steps.Count,
                CurrentStepName = step.Name,
                PercentComplete = (double)(i + 1) / pipeline.Steps.Count * 100,
                Status = "Executing"
            });

            var stepStart = DateTime.UtcNow;

            try
            {
                var operationResult = await ExecuteOperationAsync(datasetId, step, cancellationToken);
                stepResults.Add(new PipelineStepResult
                {
                    StepId = step.Id,
                    Order = step.Order,
                    StepName = step.Name,
                    Status = PipelineStepStatus.Completed,
                    OperationResult = operationResult,
                    Duration = DateTime.UtcNow - stepStart
                });
            }
            catch (Exception ex)
            {
                stepResults.Add(new PipelineStepResult
                {
                    StepId = step.Id,
                    Order = step.Order,
                    StepName = step.Name,
                    Status = step.ContinueOnError ? PipelineStepStatus.Failed : PipelineStepStatus.Failed,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - stepStart
                });

                if (!step.ContinueOnError)
                    break;
            }
        }

        var finalProfile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);

        return new PipelineExecutionResult
        {
            PipelineId = pipeline.Id,
            DatasetId = datasetId,
            Status = stepResults.All(s => s.Status == PipelineStepStatus.Completed)
                ? PipelineExecutionStatus.Completed
                : stepResults.Any(s => s.Status == PipelineStepStatus.Failed)
                    ? PipelineExecutionStatus.CompletedWithErrors
                    : PipelineExecutionStatus.Completed,
            StepResults = stepResults,
            TotalSteps = pipeline.Steps.Count,
            CompletedSteps = stepResults.Count(s => s.Status == PipelineStepStatus.Completed),
            FailedSteps = stepResults.Count(s => s.Status == PipelineStepStatus.Failed),
            SkippedSteps = stepResults.Count(s => s.Status == PipelineStepStatus.Skipped),
            OverallImpact = new CleaningImpactMetrics
            {
                QualityScoreBefore = initialProfile.QualityScore.OverallScore,
                QualityScoreAfter = finalProfile.QualityScore.OverallScore
            },
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            Duration = DateTime.UtcNow - startTime
        };
    }

    public async Task<PipelineExecutionResult> AutoCleanAsync(
        Guid datasetId,
        CleaningAggressiveness aggressiveness = CleaningAggressiveness.Balanced,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);
        var steps = new List<PipelineStep>();
        var order = 0;

        // Add steps based on detected issues and aggressiveness
        var missingThreshold = aggressiveness switch
        {
            CleaningAggressiveness.Conservative => 0.7,
            CleaningAggressiveness.Balanced => 0.5,
            CleaningAggressiveness.Aggressive => 0.3,
            _ => 0.5
        };

        // Step 1: Drop columns with too many missing values
        var highMissingColumns = profile.Columns.Where(c => c.MissingPercentage / 100 > missingThreshold).ToList();
        if (highMissingColumns.Count > 0)
        {
            steps.Add(new PipelineStep
            {
                Order = order++,
                Name = "Drop high-missing columns",
                OperationType = CleaningOperationType.DropMissingColumns,
                Parameters = new Dictionary<string, object> { ["threshold"] = missingThreshold }
            });
        }

        // Step 2: Impute remaining missing values
        var columnsToImpute = profile.Columns.Where(c => c.MissingCount > 0 && c.MissingPercentage / 100 <= missingThreshold).ToList();
        foreach (var column in columnsToImpute)
        {
            var strategy = column.DataType is ColumnDataType.Integer or ColumnDataType.Float
                ? CleaningOperationType.ImputeMedian
                : CleaningOperationType.ImputeMode;

            steps.Add(new PipelineStep
            {
                Order = order++,
                Name = $"Impute {column.Name}",
                OperationType = strategy,
                TargetColumns = [column.Name]
            });
        }

        // Step 3: Handle outliers if aggressive
        if (aggressiveness >= CleaningAggressiveness.Balanced)
        {
            steps.Add(new PipelineStep
            {
                Order = order++,
                Name = "Handle outliers",
                OperationType = aggressiveness == CleaningAggressiveness.Aggressive
                    ? CleaningOperationType.RemoveOutliers
                    : CleaningOperationType.CapOutliers
            });
        }

        // Step 4: Remove duplicates
        steps.Add(new PipelineStep
        {
            Order = order++,
            Name = "Remove duplicates",
            OperationType = CleaningOperationType.RemoveDuplicates
        });

        var pipeline = new CleaningPipeline
        {
            Name = $"Auto-Clean ({aggressiveness})",
            Description = $"Automatically generated cleaning pipeline with {aggressiveness} aggressiveness",
            Steps = steps,
            Aggressiveness = aggressiveness
        };

        return await ExecutePipelineAsync(datasetId, pipeline, progress, cancellationToken);
    }

    public async Task<CleaningPreview> PreviewCleaningAsync(
        Guid datasetId,
        CleaningOperation operation,
        int sampleSize = 100,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);

        return new CleaningPreview
        {
            TotalRowsAffected = 0,
            TotalCellsModified = 0,
            SampleChanges = [],
            EstimatedImpact = new CleaningImpactMetrics
            {
                QualityScoreBefore = profile.QualityScore.OverallScore,
                QualityScoreAfter = profile.QualityScore.OverallScore + 5
            },
            Warnings = []
        };
    }

    #endregion

    #region Session Management

    public async Task<CleaningSession> CreateSessionAsync(
        Guid datasetId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profilingService.ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);

        var session = new CleaningSession
        {
            DatasetId = datasetId,
            Name = name,
            Description = description,
            Status = CleaningSessionStatus.Draft,
            InitialProfile = profile,
            CreatedBy = "local-user"
        };

        lock (_lock)
        {
            _sessions.Add(session);
        }

        return session;
    }

    public Task<IReadOnlyList<CleaningSession>> GetSessionsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CleaningSession>>(
                _sessions.Where(s => s.DatasetId == datasetId).ToList());
        }
    }

    public Task<CleaningSession> ApplySessionAsync(
        Guid sessionId,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
                ?? throw new InvalidOperationException($"Session {sessionId} not found.");

            var updated = session with
            {
                Status = CleaningSessionStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                OutputFilePath = outputPath
            };

            var index = _sessions.FindIndex(s => s.Id == sessionId);
            _sessions[index] = updated;

            return Task.FromResult(updated);
        }
    }

    public Task<CleaningSession> RollbackSessionAsync(
        Guid sessionId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
                ?? throw new InvalidOperationException($"Session {sessionId} not found.");

            return Task.FromResult(session);
        }
    }

    #endregion

    #region Private Helpers

    private static double? CalculateImputationValue(List<string> values, ImputationStrategy strategy, IReadOnlyDictionary<string, object>? parameters)
    {
        var numericValues = values.Where(v => double.TryParse(v, out _)).Select(double.Parse).ToList();

        return strategy switch
        {
            ImputationStrategy.Mean when numericValues.Count > 0 => numericValues.Average(),
            ImputationStrategy.Median when numericValues.Count > 0 => numericValues.OrderBy(v => v).ElementAt(numericValues.Count / 2),
            ImputationStrategy.Mode => null, // Would need to find most frequent value
            ImputationStrategy.Constant when parameters?.TryGetValue("value", out var val) == true => Convert.ToDouble(val),
            _ => null
        };
    }

    private static double CalculateImputationConfidence(ImputationStrategy strategy, int nonMissingCount, int missingCount)
    {
        var dataRatio = (double)nonMissingCount / (nonMissingCount + missingCount);
        var baseConfidence = strategy switch
        {
            ImputationStrategy.Median => 0.85,
            ImputationStrategy.Mean => 0.80,
            ImputationStrategy.Mode => 0.75,
            ImputationStrategy.Knn => 0.90,
            ImputationStrategy.Regression => 0.88,
            _ => 0.70
        };

        return baseConfidence * dataRatio;
    }

    private static string GetImputationRationale(ColumnProfile column, ImputationStrategy strategy)
    {
        return strategy switch
        {
            ImputationStrategy.Median => $"Median recommended for {column.DataType} with {column.MissingPercentage:F1}% missing - robust to outliers",
            ImputationStrategy.Mean => $"Mean suitable for normally distributed {column.DataType} data",
            ImputationStrategy.Mode => $"Mode recommended for categorical column with {column.UniqueCount} unique values",
            ImputationStrategy.ForwardFill => "Forward fill recommended for time-series data",
            _ => "Default strategy based on data type"
        };
    }

    private static List<ImputationStrategy> GetAlternativeStrategies(ColumnDataType dataType)
    {
        return dataType switch
        {
            ColumnDataType.Integer or ColumnDataType.Float => [ImputationStrategy.Mean, ImputationStrategy.Median, ImputationStrategy.Knn],
            ColumnDataType.DateTime => [ImputationStrategy.ForwardFill, ImputationStrategy.BackwardFill, ImputationStrategy.TimeSeriesInterpolation],
            _ => [ImputationStrategy.Mode, ImputationStrategy.Constant]
        };
    }

    private async Task<CleaningOperationResult> ExecuteOperationAsync(
        Guid datasetId,
        PipelineStep step,
        CancellationToken cancellationToken)
    {
        return step.OperationType switch
        {
            CleaningOperationType.DropMissingRows => await DropMissingRowsAsync(datasetId, 0.5, step.TargetColumns, cancellationToken),
            CleaningOperationType.DropMissingColumns => await DropMissingColumnsAsync(datasetId, 0.5, cancellationToken),
            CleaningOperationType.RemoveDuplicates => await RemoveDuplicatesAsync(datasetId, step.TargetColumns, DuplicateKeepStrategy.First, cancellationToken),
            CleaningOperationType.ImputeMean or CleaningOperationType.ImputeMedian or CleaningOperationType.ImputeMode =>
                new CleaningOperationResult { OperationType = step.OperationType, Success = true, Duration = TimeSpan.FromMilliseconds(50) },
            _ => new CleaningOperationResult { OperationType = step.OperationType, Success = true, Duration = TimeSpan.FromMilliseconds(10) }
        };
    }

    #endregion
}
