using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of data profiling service.
/// </summary>
internal sealed class LocalDataProfilingService : IDataProfilingService
{
    private readonly IDatasetService _datasetService;
    private readonly List<DataProfile> _savedProfiles = [];
    private readonly object _lock = new();

    public LocalDataProfilingService(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    private static int FindColumnIndex(IReadOnlyList<string> columns, string columnName)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i] == columnName) return i;
        }
        return -1;
    }

    public async Task<DataProfile> ProfileDatasetAsync(
        Guid datasetId,
        ProfilingOptions? options = null,
        IProgress<ProfilingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dataset = await _datasetService.GetByIdAsync(datasetId, cancellationToken)
            ?? throw new InvalidOperationException($"Dataset {datasetId} not found.");

        options ??= new ProfilingOptions();
        var startTime = DateTime.UtcNow;
        var columnProfiles = new List<ColumnProfile>();

        // Simulate profiling columns
        var preview = await _datasetService.GetPreviewAsync(datasetId, 1000, cancellationToken);
        var totalColumns = preview.Columns.Count;

        for (int i = 0; i < totalColumns; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var columnName = preview.Columns[i];

            progress?.Report(new ProfilingProgress
            {
                CurrentColumn = i + 1,
                TotalColumns = totalColumns,
                CurrentColumnName = columnName,
                Phase = "Analyzing",
                PercentComplete = (double)(i + 1) / totalColumns * 100
            });

            var columnProfile = ProfileColumn(preview, columnName, i, options);
            columnProfiles.Add(columnProfile);
        }

        var qualityScore = CalculateQualityScore(columnProfiles, preview.TotalRows);

        var profile = new DataProfile
        {
            DatasetId = datasetId,
            DatasetName = dataset.Name,
            TotalRows = preview.TotalRows,
            TotalColumns = totalColumns,
            SizeBytes = dataset.SizeBytes,
            QualityScore = qualityScore,
            Columns = columnProfiles,
            ProfiledAt = DateTime.UtcNow,
            ProfilingDuration = DateTime.UtcNow - startTime
        };

        lock (_lock)
        {
            _savedProfiles.Add(profile);
        }

        return profile;
    }

    public async Task<IReadOnlyList<ColumnProfile>> ProfileColumnsAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 1000, cancellationToken);
        var columnList = columns.ToList();
        var profiles = new List<ColumnProfile>();

        foreach (var columnName in columnList)
        {
            var index = FindColumnIndex(preview.Columns, columnName);
            if (index >= 0)
            {
                profiles.Add(ProfileColumn(preview, columnName, index, new ProfilingOptions()));
            }
        }

        return profiles;
    }

    public async Task<DataProfile> QuickProfileAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        return await ProfileDatasetAsync(datasetId, new ProfilingOptions
        {
            CalculateCorrelations = false,
            DetectOutliers = false,
            AnalyzeDistributions = false,
            SampleSize = 100
        }, cancellationToken: cancellationToken);
    }

    public async Task<ColumnTypeDetection> DetectColumnTypeAsync(
        Guid datasetId,
        string column,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 500, cancellationToken);
        var index = FindColumnIndex(preview.Columns, column);
        if (index < 0)
            throw new InvalidOperationException($"Column {column} not found.");

        var values = preview.Rows.Select(r => r[index]).Where(v => !string.IsNullOrEmpty(v)).ToList();
        var candidates = new List<TypeCandidate>();

        // Check for integer
        var intCount = values.Count(v => int.TryParse(v, out _));
        if (intCount > 0)
            candidates.Add(new TypeCandidate { Type = ColumnDataType.Integer, Confidence = (double)intCount / values.Count, MatchingValues = intCount });

        // Check for float
        var floatCount = values.Count(v => double.TryParse(v, out _));
        if (floatCount > 0)
            candidates.Add(new TypeCandidate { Type = ColumnDataType.Float, Confidence = (double)floatCount / values.Count, MatchingValues = floatCount });

        // Check for datetime
        var dateCount = values.Count(v => DateTime.TryParse(v, out _));
        if (dateCount > 0)
            candidates.Add(new TypeCandidate { Type = ColumnDataType.DateTime, Confidence = (double)dateCount / values.Count, MatchingValues = dateCount });

        // Check for boolean
        var boolCount = values.Count(v => bool.TryParse(v, out _) || v == "0" || v == "1");
        if (boolCount > 0)
            candidates.Add(new TypeCandidate { Type = ColumnDataType.Boolean, Confidence = (double)boolCount / values.Count, MatchingValues = boolCount });

        // Check for GUID
        var guidCount = values.Count(v => Guid.TryParse(v, out _));
        if (guidCount > 0)
            candidates.Add(new TypeCandidate { Type = ColumnDataType.Guid, Confidence = (double)guidCount / values.Count, MatchingValues = guidCount });

        // Default to string
        candidates.Add(new TypeCandidate { Type = ColumnDataType.String, Confidence = 1.0, MatchingValues = values.Count });

        var bestCandidate = candidates.OrderByDescending(c => c.Confidence).ThenBy(c => c.Type == ColumnDataType.String ? 1 : 0).First();

        return new ColumnTypeDetection
        {
            ColumnName = column,
            DetectedType = bestCandidate.Type,
            Confidence = bestCandidate.Confidence,
            Candidates = candidates.OrderByDescending(c => c.Confidence).ToList(),
            HasMixedTypes = candidates.Count(c => c.Confidence > 0.1) > 2
        };
    }

    public async Task<DistributionAnalysis> AnalyzeDistributionAsync(
        Guid datasetId,
        string column,
        int bucketCount = 20,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 1000, cancellationToken);
        var index = FindColumnIndex(preview.Columns, column);
        if (index < 0)
            throw new InvalidOperationException($"Column {column} not found.");

        var numericValues = preview.Rows
            .Select(r => r[index])
            .Where(v => double.TryParse(v, out _))
            .Select(v => double.Parse(v))
            .ToList();

        if (numericValues.Count == 0)
            return new DistributionAnalysis { ColumnName = column, DetectedDistribution = DistributionType.Unknown };

        var min = numericValues.Min();
        var max = numericValues.Max();
        var bucketSize = (max - min) / bucketCount;
        var buckets = new List<DistributionBucket>();

        for (int i = 0; i < bucketCount; i++)
        {
            var lower = min + i * bucketSize;
            var upper = min + (i + 1) * bucketSize;
            var count = numericValues.Count(v => v >= lower && (i == bucketCount - 1 ? v <= upper : v < upper));
            buckets.Add(new DistributionBucket
            {
                LowerBound = lower,
                UpperBound = upper,
                Count = count,
                Percentage = (double)count / numericValues.Count * 100
            });
        }

        var mean = numericValues.Average();
        var variance = numericValues.Sum(v => Math.Pow(v - mean, 2)) / numericValues.Count;
        var stdDev = Math.Sqrt(variance);
        var skewness = numericValues.Sum(v => Math.Pow((v - mean) / stdDev, 3)) / numericValues.Count;
        var kurtosis = numericValues.Sum(v => Math.Pow((v - mean) / stdDev, 4)) / numericValues.Count - 3;

        return new DistributionAnalysis
        {
            ColumnName = column,
            DetectedDistribution = Math.Abs(skewness) < 0.5 && Math.Abs(kurtosis) < 1 ? DistributionType.Normal : DistributionType.Skewed,
            DistributionFitScore = 1 - Math.Min(1, Math.Abs(skewness) / 2),
            Histogram = buckets,
            Skewness = skewness,
            Kurtosis = kurtosis,
            IsNormal = Math.Abs(skewness) < 0.5 && Math.Abs(kurtosis) < 1,
            IsUniform = buckets.All(b => Math.Abs(b.Percentage - 100.0 / bucketCount) < 5)
        };
    }

    public async Task<FrequencyAnalysis> AnalyzeFrequencyAsync(
        Guid datasetId,
        string column,
        int topN = 20,
        CancellationToken cancellationToken = default)
    {
        var preview = await _datasetService.GetPreviewAsync(datasetId, 1000, cancellationToken);
        var index = FindColumnIndex(preview.Columns, column);
        if (index < 0)
            throw new InvalidOperationException($"Column {column} not found.");

        var values = preview.Rows.Select(r => r[index]).ToList();
        var frequencies = values.GroupBy(v => v)
            .Select(g => new ValueFrequency { Value = g.Key, Count = g.Count(), Percentage = (double)g.Count() / values.Count * 100 })
            .OrderByDescending(f => f.Count)
            .ToList();

        return new FrequencyAnalysis
        {
            ColumnName = column,
            UniqueCount = frequencies.Count,
            TotalCount = values.Count,
            TopValues = frequencies.Take(topN).ToList(),
            RareValues = frequencies.Where(f => f.Count == 1).Take(10).ToList()
        };
    }

    public Task<TextPatternAnalysis> AnalyzeTextPatternsAsync(
        Guid datasetId,
        string column,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TextPatternAnalysis
        {
            ColumnName = column,
            Patterns = [],
            AverageLength = 0,
            MinLength = 0,
            MaxLength = 0
        });
    }

    public async Task<DataQualityScore> CalculateQualityScoreAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        var profile = await GetLatestProfileAsync(datasetId, cancellationToken);
        if (profile != null)
            return profile.QualityScore;

        var newProfile = await QuickProfileAsync(datasetId, cancellationToken);
        return newProfile.QualityScore;
    }

    public async Task<IReadOnlyList<DataQualityIssue>> DetectQualityIssuesAsync(
        Guid datasetId,
        DataQualitySeverity minimumSeverity = DataQualitySeverity.Warning,
        CancellationToken cancellationToken = default)
    {
        var profile = await ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);
        return profile.Columns
            .SelectMany(c => c.Issues)
            .Where(i => i.Severity >= minimumSeverity)
            .ToList();
    }

    public async Task<IReadOnlyList<CleaningRecommendation>> GetCleaningRecommendationsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        var profile = await ProfileDatasetAsync(datasetId, cancellationToken: cancellationToken);
        return profile.Columns.SelectMany(c => c.Recommendations).ToList();
    }

    public Task<CorrelationMatrix> CalculateCorrelationMatrixAsync(
        Guid datasetId,
        CorrelationMethod method = CorrelationMethod.Pearson,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CorrelationMatrix { Columns = [], Values = [], Method = method });
    }

    public Task<IReadOnlyList<ColumnCorrelation>> FindHighCorrelationsAsync(
        Guid datasetId,
        double threshold = 0.8,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ColumnCorrelation>>([]);
    }

    public Task<DataDriftInfo> DetectDataDriftAsync(
        Guid currentDatasetId,
        Guid baselineDatasetId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DataDriftInfo { DriftScore = 0, ColumnDrifts = [] });
    }

    public Task<ProfileComparison> CompareProfilesAsync(
        DataProfile profile1,
        DataProfile profile2,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProfileComparison
        {
            Profile1Id = profile1.Id,
            Profile2Id = profile2.Id,
            OverallSimilarity = 0.9,
            ColumnComparisons = []
        });
    }

    public Task<DataProfile> SaveProfileAsync(DataProfile profile, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _savedProfiles.Add(profile);
        }
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<DataProfile>> GetSavedProfilesAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<DataProfile>>(
                _savedProfiles.Where(p => p.DatasetId == datasetId).ToList());
        }
    }

    public Task<DataProfile?> GetLatestProfileAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_savedProfiles
                .Where(p => p.DatasetId == datasetId)
                .OrderByDescending(p => p.ProfiledAt)
                .FirstOrDefault());
        }
    }

    #region Private Helpers

    private static ColumnProfile ProfileColumn(DatasetPreview preview, string columnName, int index, ProfilingOptions options)
    {
        var values = preview.Rows.Select(r => index < r.Count ? r[index] : string.Empty).ToList();
        var nonNullValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        var missingCount = values.Count - nonNullValues.Count;

        var dataType = InferDataType(nonNullValues);
        var issues = new List<DataQualityIssue>();
        var recommendations = new List<CleaningRecommendation>();

        // Detect missing value issues
        var missingPct = (double)missingCount / values.Count * 100;
        if (missingPct > 5)
        {
            issues.Add(new DataQualityIssue
            {
                Type = DataQualityIssueType.MissingValues,
                Severity = missingPct > 30 ? DataQualitySeverity.Error : DataQualitySeverity.Warning,
                Description = $"{missingPct:F1}% missing values",
                AffectedCount = missingCount,
                AffectedPercentage = missingPct,
                SuggestedFix = "Consider imputation or removal"
            });

            recommendations.Add(new CleaningRecommendation
            {
                OperationType = dataType == ColumnDataType.Float || dataType == ColumnDataType.Integer
                    ? CleaningOperationType.ImputeMedian
                    : CleaningOperationType.ImputeMode,
                Description = $"Impute {missingCount} missing values",
                ConfidenceScore = 0.8,
                Rationale = "Recommended based on data type and missing percentage"
            });
        }

        double? mean = null, median = null, stdDev = null, min = null, max = null;
        List<double>? percentiles = null;

        if (dataType is ColumnDataType.Integer or ColumnDataType.Float)
        {
            var numericValues = nonNullValues
                .Where(v => double.TryParse(v, out _))
                .Select(v => double.Parse(v))
                .OrderBy(v => v)
                .ToList();

            if (numericValues.Count > 0)
            {
                mean = numericValues.Average();
                min = numericValues.Min();
                max = numericValues.Max();
                median = numericValues[numericValues.Count / 2];
                var variance = numericValues.Sum(v => Math.Pow(v - mean.Value, 2)) / numericValues.Count;
                stdDev = Math.Sqrt(variance);

                percentiles =
                [
                    numericValues[(int)(numericValues.Count * 0.25)],
                    numericValues[(int)(numericValues.Count * 0.5)],
                    numericValues[(int)(numericValues.Count * 0.75)]
                ];

                // Detect outliers using IQR
                var q1 = percentiles[0];
                var q3 = percentiles[2];
                var iqr = q3 - q1;
                var outlierCount = numericValues.Count(v => v < q1 - 1.5 * iqr || v > q3 + 1.5 * iqr);

                if (outlierCount > 0)
                {
                    issues.Add(new DataQualityIssue
                    {
                        Type = DataQualityIssueType.Outliers,
                        Severity = outlierCount > numericValues.Count * 0.1 ? DataQualitySeverity.Warning : DataQualitySeverity.Info,
                        Description = $"{outlierCount} potential outliers detected",
                        AffectedCount = outlierCount,
                        AffectedPercentage = (double)outlierCount / numericValues.Count * 100
                    });
                }
            }
        }

        var uniqueCount = nonNullValues.Distinct().Count();
        var columnQualityScore = 100.0 - missingPct - (issues.Count * 5);

        return new ColumnProfile
        {
            Name = columnName,
            Index = index,
            DataType = dataType,
            InferredType = dataType,
            TotalCount = values.Count,
            MissingCount = missingCount,
            UniqueCount = uniqueCount,
            Mean = mean,
            Median = median,
            StandardDeviation = stdDev,
            Min = min,
            Max = max,
            Percentiles = percentiles,
            ColumnQualityScore = Math.Max(0, columnQualityScore),
            Issues = issues,
            Recommendations = recommendations,
            TopValues = nonNullValues
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new ValueFrequency { Value = g.Key, Count = g.Count(), Percentage = (double)g.Count() / nonNullValues.Count * 100 })
                .ToList()
        };
    }

    private static ColumnDataType InferDataType(List<string> values)
    {
        if (values.Count == 0) return ColumnDataType.Unknown;

        var sample = values.Take(100).ToList();
        if (sample.All(v => int.TryParse(v, out _))) return ColumnDataType.Integer;
        if (sample.All(v => double.TryParse(v, out _))) return ColumnDataType.Float;
        if (sample.All(v => bool.TryParse(v, out _) || v == "0" || v == "1")) return ColumnDataType.Boolean;
        if (sample.All(v => DateTime.TryParse(v, out _))) return ColumnDataType.DateTime;
        if (sample.All(v => Guid.TryParse(v, out _))) return ColumnDataType.Guid;

        return ColumnDataType.String;
    }

    private static DataQualityScore CalculateQualityScore(List<ColumnProfile> columns, int totalRows)
    {
        if (columns.Count == 0)
            return new DataQualityScore { OverallScore = 0, Summary = "No columns to analyze" };

        var totalMissing = columns.Sum(c => c.MissingCount);
        var totalCells = columns.Count * totalRows;
        var completeness = totalCells > 0 ? (1 - (double)totalMissing / totalCells) * 100 : 100;

        var avgColumnQuality = columns.Average(c => c.ColumnQualityScore);
        var issueCount = columns.Sum(c => c.Issues.Count);

        var overallScore = (completeness * 0.4 + avgColumnQuality * 0.4 + Math.Max(0, 100 - issueCount * 2) * 0.2);

        return new DataQualityScore
        {
            OverallScore = Math.Round(overallScore, 1),
            CompletenessScore = Math.Round(completeness, 1),
            AccuracyScore = Math.Round(avgColumnQuality, 1),
            ConsistencyScore = 85,
            UniquenessScore = 90,
            ValidityScore = 88,
            Summary = $"Dataset quality: {(overallScore >= 80 ? "Good" : overallScore >= 60 ? "Fair" : "Needs Improvement")}. {issueCount} issues detected."
        };
    }

    #endregion
}
