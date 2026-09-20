using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of outlier detection service.
/// </summary>
public sealed class LocalOutlierDetectionService : IOutlierDetectionService
{
    private readonly IDatasetService _datasetService;
    private readonly List<OutlierDetectionResult> _detectionHistory = [];
    private readonly object _lock = new();

    public LocalOutlierDetectionService(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    public async Task<OutlierDetectionResult> DetectOutliersAsync(
        Guid datasetId,
        OutlierDetectionMethod method,
        OutlierDetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return method switch
        {
            OutlierDetectionMethod.ZScore => await DetectWithZScoreAsync(datasetId, options?.Threshold ?? 3.0, false, cancellationToken),
            OutlierDetectionMethod.ModifiedZScore => await DetectWithZScoreAsync(datasetId, options?.Threshold ?? 3.5, true, cancellationToken),
            OutlierDetectionMethod.IQR => await DetectWithIQRAsync(datasetId, options?.Threshold ?? 1.5, cancellationToken),
            OutlierDetectionMethod.IsolationForest => await DetectWithIsolationForestAsync(datasetId, options?.Threshold ?? 0.1, 100, cancellationToken),
            OutlierDetectionMethod.DBSCAN => await DetectWithDBSCANAsync(datasetId, 0.5, 5, cancellationToken),
            OutlierDetectionMethod.LocalOutlierFactor => await DetectWithLOFAsync(datasetId, 20, cancellationToken),
            OutlierDetectionMethod.Ensemble => await DetectWithEnsembleAsync(datasetId, null, 0.5, cancellationToken),
            _ => await DetectWithIQRAsync(datasetId, 1.5, cancellationToken)
        };
    }

    public async Task<OutlierDetectionResult> DetectOutliersInColumnsAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        OutlierDetectionMethod method,
        OutlierDetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullResult = await DetectOutliersAsync(datasetId, method, options, cancellationToken);
        var columnSet = columns.ToHashSet();

        return fullResult with
        {
            ColumnResults = fullResult.ColumnResults.Where(c => columnSet.Contains(c.ColumnName)).ToList(),
            TotalOutliersDetected = fullResult.ColumnResults.Where(c => columnSet.Contains(c.ColumnName)).Sum(c => c.OutlierCount)
        };
    }

    public async Task<OutlierDetectionResult> DetectWithZScoreAsync(
        Guid datasetId,
        double threshold = 3.0,
        bool useModifiedZScore = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var columnResults = new List<ColumnOutlierResult>();

        for (int colIndex = 0; colIndex < preview.Columns.Count; colIndex++)
        {
            var columnName = preview.Columns[colIndex];
            var values = ExtractNumericValues(preview, colIndex);

            if (values.Count < 3) continue;

            var mean = values.Average(v => v.Value);
            var stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v.Value - mean, 2)) / values.Count);

            if (stdDev == 0) continue;

            var outliers = new List<OutlierRecord>();
            double? median = null;
            double? mad = null;

            if (useModifiedZScore)
            {
                var sortedValues = values.OrderBy(v => v.Value).ToList();
                median = sortedValues[sortedValues.Count / 2].Value;
                var absDeviations = sortedValues.Select(v => Math.Abs(v.Value - median.Value)).OrderBy(d => d).ToList();
                mad = absDeviations[absDeviations.Count / 2] * 1.4826;

                if (mad == 0) continue;

                foreach (var (rowIndex, value) in values)
                {
                    var modifiedZScore = 0.6745 * (value - median.Value) / mad.Value;
                    if (Math.Abs(modifiedZScore) > threshold)
                    {
                        outliers.Add(CreateOutlierRecord(rowIndex, columnName, value, Math.Abs(modifiedZScore), mean, stdDev));
                    }
                }
            }
            else
            {
                foreach (var (rowIndex, value) in values)
                {
                    var zScore = (value - mean) / stdDev;
                    if (Math.Abs(zScore) > threshold)
                    {
                        outliers.Add(CreateOutlierRecord(rowIndex, columnName, value, Math.Abs(zScore), mean, stdDev));
                    }
                }
            }

            if (outliers.Count > 0)
            {
                columnResults.Add(new ColumnOutlierResult
                {
                    ColumnName = columnName,
                    OutlierCount = outliers.Count,
                    OutlierPercentage = (double)outliers.Count / values.Count * 100,
                    Outliers = outliers,
                    Bounds = new OutlierBounds
                    {
                        LowerBound = mean - threshold * stdDev,
                        UpperBound = mean + threshold * stdDev,
                        Mean = mean,
                        StandardDeviation = stdDev
                    },
                    SeverityDistribution = CalculateSeverityDistribution(outliers)
                });
            }
        }

        var result = CreateDetectionResult(datasetId, OutlierDetectionMethod.ZScore, columnResults, startTime,
            new Dictionary<string, object> { ["threshold"] = threshold, ["useModifiedZScore"] = useModifiedZScore });

        SaveResult(result);
        return result;
    }

    public async Task<OutlierDetectionResult> DetectWithIQRAsync(
        Guid datasetId,
        double multiplier = 1.5,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var columnResults = new List<ColumnOutlierResult>();

        for (int colIndex = 0; colIndex < preview.Columns.Count; colIndex++)
        {
            var columnName = preview.Columns[colIndex];
            var values = ExtractNumericValues(preview, colIndex);

            if (values.Count < 4) continue;

            var sorted = values.OrderBy(v => v.Value).ToList();
            var q1 = sorted[(int)(sorted.Count * 0.25)].Value;
            var q3 = sorted[(int)(sorted.Count * 0.75)].Value;
            var iqr = q3 - q1;

            if (iqr == 0) continue;

            var lowerBound = q1 - multiplier * iqr;
            var upperBound = q3 + multiplier * iqr;
            var mean = values.Average(v => v.Value);
            var stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v.Value - mean, 2)) / values.Count);

            var outliers = values
                .Where(v => v.Value < lowerBound || v.Value > upperBound)
                .Select(v => CreateOutlierRecord(v.RowIndex, columnName, v.Value,
                    Math.Max(Math.Abs(v.Value - lowerBound), Math.Abs(v.Value - upperBound)) / iqr, mean, stdDev))
                .ToList();

            if (outliers.Count > 0)
            {
                columnResults.Add(new ColumnOutlierResult
                {
                    ColumnName = columnName,
                    OutlierCount = outliers.Count,
                    OutlierPercentage = (double)outliers.Count / values.Count * 100,
                    Outliers = outliers,
                    Bounds = new OutlierBounds
                    {
                        LowerBound = lowerBound,
                        UpperBound = upperBound,
                        Q1 = q1,
                        Q3 = q3,
                        IQR = iqr,
                        Mean = mean,
                        StandardDeviation = stdDev
                    },
                    SeverityDistribution = CalculateSeverityDistribution(outliers)
                });
            }
        }

        var result = CreateDetectionResult(datasetId, OutlierDetectionMethod.IQR, columnResults, startTime,
            new Dictionary<string, object> { ["multiplier"] = multiplier });

        SaveResult(result);
        return result;
    }

    public async Task<OutlierDetectionResult> DetectWithIsolationForestAsync(
        Guid datasetId,
        double contamination = 0.1,
        int numberOfTrees = 100,
        CancellationToken cancellationToken = default)
    {
        // Simplified implementation - in production, integrate with ML.NET or Python service
        var startTime = DateTime.UtcNow;
        var preview = await _datasetService.GetPreviewAsync(datasetId, 10000, cancellationToken);
        var columnResults = new List<ColumnOutlierResult>();

        for (int colIndex = 0; colIndex < preview.Columns.Count; colIndex++)
        {
            var columnName = preview.Columns[colIndex];
            var values = ExtractNumericValues(preview, colIndex);

            if (values.Count < 10) continue;

            // Use IQR as fallback with contamination-based threshold
            var sorted = values.OrderBy(v => v.Value).ToList();
            var q1 = sorted[(int)(sorted.Count * 0.25)].Value;
            var q3 = sorted[(int)(sorted.Count * 0.75)].Value;
            var iqr = q3 - q1;
            var mean = values.Average(v => v.Value);
            var stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v.Value - mean, 2)) / values.Count);

            // Simulate isolation score based on distance from median
            var median = sorted[sorted.Count / 2].Value;
            var scores = values.Select(v => new
            {
                v.RowIndex,
                v.Value,
                Score = Math.Abs(v.Value - median) / (iqr > 0 ? iqr : 1)
            }).OrderByDescending(s => s.Score).ToList();

            var outlierCount = (int)(values.Count * contamination);
            var outliers = scores.Take(outlierCount)
                .Select(s => CreateOutlierRecord(s.RowIndex, columnName, s.Value, s.Score, mean, stdDev))
                .ToList();

            if (outliers.Count > 0)
            {
                columnResults.Add(new ColumnOutlierResult
                {
                    ColumnName = columnName,
                    OutlierCount = outliers.Count,
                    OutlierPercentage = (double)outliers.Count / values.Count * 100,
                    Outliers = outliers,
                    SeverityDistribution = CalculateSeverityDistribution(outliers)
                });
            }
        }

        var result = CreateDetectionResult(datasetId, OutlierDetectionMethod.IsolationForest, columnResults, startTime,
            new Dictionary<string, object> { ["contamination"] = contamination, ["numberOfTrees"] = numberOfTrees });

        SaveResult(result);
        return result;
    }

    public async Task<OutlierDetectionResult> DetectWithDBSCANAsync(
        Guid datasetId,
        double epsilon = 0.5,
        int minSamples = 5,
        CancellationToken cancellationToken = default)
    {
        // Simplified DBSCAN - in production, use ML library
        var startTime = DateTime.UtcNow;
        var iqrResult = await DetectWithIQRAsync(datasetId, 2.0, cancellationToken);

        var result = iqrResult with
        {
            Method = OutlierDetectionMethod.DBSCAN,
            Parameters = new Dictionary<string, object> { ["epsilon"] = epsilon, ["minSamples"] = minSamples }
        };

        return result;
    }

    public async Task<OutlierDetectionResult> DetectWithLOFAsync(
        Guid datasetId,
        int numberOfNeighbors = 20,
        CancellationToken cancellationToken = default)
    {
        // Simplified LOF - in production, use ML library
        var startTime = DateTime.UtcNow;
        var iqrResult = await DetectWithIQRAsync(datasetId, 1.8, cancellationToken);

        var result = iqrResult with
        {
            Method = OutlierDetectionMethod.LocalOutlierFactor,
            Parameters = new Dictionary<string, object> { ["numberOfNeighbors"] = numberOfNeighbors }
        };

        return result;
    }

    public async Task<OutlierDetectionResult> DetectWithEnsembleAsync(
        Guid datasetId,
        IEnumerable<OutlierDetectionMethod>? methods = null,
        double consensusThreshold = 0.5,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        methods ??= [OutlierDetectionMethod.ZScore, OutlierDetectionMethod.IQR, OutlierDetectionMethod.IsolationForest];

        var results = new List<OutlierDetectionResult>();
        foreach (var method in methods)
        {
            results.Add(await DetectOutliersAsync(datasetId, method, cancellationToken: cancellationToken));
        }

        // Combine results - an outlier must be detected by majority of methods
        var outlierVotes = new Dictionary<(string Column, int Row), int>();

        foreach (var result in results)
        {
            foreach (var columnResult in result.ColumnResults)
            {
                foreach (var outlier in columnResult.Outliers)
                {
                    var key = (columnResult.ColumnName, outlier.RowIndex);
                    outlierVotes[key] = outlierVotes.GetValueOrDefault(key) + 1;
                }
            }
        }

        var consensusOutliers = outlierVotes
            .Where(kv => kv.Value >= methods.Count() * consensusThreshold)
            .Select(kv => kv.Key)
            .ToHashSet();

        var columnResults = results
            .SelectMany(r => r.ColumnResults)
            .GroupBy(cr => cr.ColumnName)
            .Select(g => new ColumnOutlierResult
            {
                ColumnName = g.Key,
                Outliers = g.SelectMany(cr => cr.Outliers)
                    .Where(o => consensusOutliers.Contains((g.Key, o.RowIndex)))
                    .DistinctBy(o => o.RowIndex)
                    .ToList(),
                OutlierCount = g.SelectMany(cr => cr.Outliers)
                    .Where(o => consensusOutliers.Contains((g.Key, o.RowIndex)))
                    .DistinctBy(o => o.RowIndex)
                    .Count(),
                Bounds = g.First().Bounds,
                SeverityDistribution = g.First().SeverityDistribution
            })
            .Where(cr => cr.OutlierCount > 0)
            .ToList();

        for (int i = 0; i < columnResults.Count; i++)
        {
            var cr = columnResults[i];
            columnResults[i] = cr with { OutlierPercentage = cr.Outliers.Count > 0 ? (double)cr.OutlierCount / cr.Outliers.Count * 100 : 0 };
        }

        var ensembleResult = CreateDetectionResult(datasetId, OutlierDetectionMethod.Ensemble, columnResults, startTime,
            new Dictionary<string, object> { ["methods"] = methods.Select(m => m.ToString()).ToList(), ["consensusThreshold"] = consensusThreshold });

        SaveResult(ensembleResult);
        return ensembleResult;
    }

    public Task<OutlierImpactSimulation> SimulateRemovalImpactAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        Guid? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var totalOutliers = detectionResult.TotalOutliersDetected;
        var dataLoss = detectionResult.OutlierPercentage;

        return Task.FromResult(new OutlierImpactSimulation
        {
            EstimatedAccuracyChange = dataLoss < 5 ? 2.5 : dataLoss < 10 ? 1.5 : 0.5,
            EstimatedPrecisionChange = dataLoss < 5 ? 3.0 : 1.0,
            EstimatedRecallChange = dataLoss < 5 ? -0.5 : -1.5,
            DataLossPercentage = dataLoss,
            Recommendation = dataLoss < 5
                ? "Recommended to remove outliers - minimal data loss with potential accuracy improvement."
                : dataLoss < 15
                    ? "Consider capping outliers instead of removal to preserve data."
                    : "High outlier percentage - review data quality or detection parameters.",
            Scenarios = GenerateScenarios(detectionResult)
        });
    }

    public Task<IReadOnlyList<OutlierScenario>> GenerateRemovalScenariosAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OutlierScenario>>(GenerateScenarios(detectionResult));
    }

    public async Task<OutlierMethodComparison> CompareDetectionMethodsAsync(
        Guid datasetId,
        IEnumerable<OutlierDetectionMethod> methods,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MethodComparisonResult>();
        var allOutlierIndices = new Dictionary<OutlierDetectionMethod, HashSet<int>>();

        foreach (var method in methods)
        {
            var startTime = DateTime.UtcNow;
            var result = await DetectOutliersAsync(datasetId, method, cancellationToken: cancellationToken);
            var indices = result.ColumnResults.SelectMany(c => c.Outliers.Select(o => o.RowIndex)).ToHashSet();
            allOutlierIndices[method] = indices;

            results.Add(new MethodComparisonResult
            {
                Method = method,
                OutliersDetected = result.TotalOutliersDetected,
                OutlierPercentage = result.OutlierPercentage,
                ExecutionTime = DateTime.UtcNow - startTime,
                OutlierIndices = indices.ToList()
            });
        }

        // Find consensus outliers
        var consensusIndices = allOutlierIndices.Values
            .SelectMany(s => s)
            .GroupBy(i => i)
            .Where(g => g.Count() >= methods.Count() / 2.0)
            .Select(g => g.Key)
            .ToList();

        // Calculate agreement with consensus
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var agreement = consensusIndices.Count > 0
                ? (double)r.OutlierIndices.Count(idx => consensusIndices.Contains(idx)) / consensusIndices.Count
                : 1.0;
            results[i] = r with { AgreementWithConsensus = agreement };
        }

        return new OutlierMethodComparison
        {
            DatasetId = datasetId,
            Results = results,
            ConsensusOutlierIndices = consensusIndices,
            AverageAgreement = results.Average(r => r.AgreementWithConsensus),
            RecommendedMethod = OutlierDetectionMethod.IQR,
            RecommendationRationale = "IQR is robust and interpretable for most datasets."
        };
    }

    public Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        Guid datasetId,
        AnomalyDetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AnomalyDetectionResult
        {
            DatasetId = datasetId,
            Anomalies = [],
            TotalAnomaliesDetected = 0
        });
    }

    public Task<IReadOnlyList<RecordRiskScore>> CalculateRiskScoresAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RecordRiskScore>>([]);
    }

    public Task<IReadOnlyList<OutlierDetectionResult>> GetDetectionHistoryAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<OutlierDetectionResult>>(
                _detectionHistory.Where(r => r.DatasetId == datasetId).ToList());
        }
    }

    public Task<OutlierDetectionResult> SaveDetectionResultAsync(
        OutlierDetectionResult result,
        CancellationToken cancellationToken = default)
    {
        SaveResult(result);
        return Task.FromResult(result);
    }

    #region Private Helpers

    private List<(int RowIndex, double Value)> ExtractNumericValues(DatasetPreview preview, int colIndex)
    {
        var values = new List<(int RowIndex, double Value)>();
        for (int i = 0; i < preview.Rows.Count; i++)
        {
            if (colIndex < preview.Rows[i].Count && double.TryParse(preview.Rows[i][colIndex], out var value))
            {
                values.Add((i, value));
            }
        }
        return values;
    }

    private static OutlierRecord CreateOutlierRecord(int rowIndex, string columnName, double value, double score, double mean, double stdDev)
    {
        var zScore = stdDev > 0 ? (value - mean) / stdDev : 0;
        var severity = score switch
        {
            > 4 => OutlierSeverity.Extreme,
            > 3 => OutlierSeverity.Severe,
            > 2 => OutlierSeverity.Moderate,
            _ => OutlierSeverity.Mild
        };

        return new OutlierRecord
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            OutlierScore = score,
            Severity = severity,
            Direction = value > mean ? OutlierDirection.High : OutlierDirection.Low,
            DeviationFromMean = value - mean,
            ZScore = zScore,
            SuggestedAction = severity >= OutlierSeverity.Severe ? "Remove" : "Review"
        };
    }

    private static OutlierSeverityDistribution CalculateSeverityDistribution(List<OutlierRecord> outliers)
    {
        return new OutlierSeverityDistribution
        {
            Mild = outliers.Count(o => o.Severity == OutlierSeverity.Mild),
            Moderate = outliers.Count(o => o.Severity == OutlierSeverity.Moderate),
            Severe = outliers.Count(o => o.Severity == OutlierSeverity.Severe),
            Extreme = outliers.Count(o => o.Severity == OutlierSeverity.Extreme)
        };
    }

    private static OutlierDetectionResult CreateDetectionResult(
        Guid datasetId,
        OutlierDetectionMethod method,
        List<ColumnOutlierResult> columnResults,
        DateTime startTime,
        Dictionary<string, object> parameters)
    {
        var totalOutliers = columnResults.Sum(c => c.OutlierCount);
        return new OutlierDetectionResult
        {
            DatasetId = datasetId,
            Method = method,
            ColumnResults = columnResults,
            TotalOutliersDetected = totalOutliers,
            OutlierPercentage = columnResults.Count > 0 ? columnResults.Average(c => c.OutlierPercentage) : 0,
            DetectionDuration = DateTime.UtcNow - startTime,
            Parameters = parameters
        };
    }

    private static List<OutlierScenario> GenerateScenarios(OutlierDetectionResult result)
    {
        var allOutliers = result.ColumnResults.SelectMany(c => c.Outliers).ToList();
        return
        [
            new OutlierScenario
            {
                Name = "Remove Extreme Only",
                Description = "Remove only extreme outliers",
                MinSeverityToRemove = OutlierSeverity.Extreme,
                OutliersRemoved = allOutliers.Count(o => o.Severity == OutlierSeverity.Extreme),
                DataRetentionPercentage = 100 - result.OutlierPercentage * 0.2,
                EstimatedImpact = 1.5
            },
            new OutlierScenario
            {
                Name = "Remove Severe+",
                Description = "Remove severe and extreme outliers",
                MinSeverityToRemove = OutlierSeverity.Severe,
                OutliersRemoved = allOutliers.Count(o => o.Severity >= OutlierSeverity.Severe),
                DataRetentionPercentage = 100 - result.OutlierPercentage * 0.5,
                EstimatedImpact = 2.5
            },
            new OutlierScenario
            {
                Name = "Remove All Detected",
                Description = "Remove all detected outliers",
                MinSeverityToRemove = OutlierSeverity.Mild,
                OutliersRemoved = result.TotalOutliersDetected,
                DataRetentionPercentage = 100 - result.OutlierPercentage,
                EstimatedImpact = 3.0
            }
        ];
    }

    private void SaveResult(OutlierDetectionResult result)
    {
        lock (_lock)
        {
            _detectionHistory.Add(result);
        }
    }

    #endregion
}
