using AIMRAN_Data_Science_Lab.Models.DataCleaning;
using System.Text.Json;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of cleaning rule service with self-learning capabilities.
/// </summary>
public sealed class LocalCleaningRuleService : ICleaningRuleService
{
    private readonly List<CleaningRule> _rules = [];
    private readonly List<CleaningPipeline> _pipelines = [];
    private readonly List<UserCleaningAction> _userActions = [];
    private readonly List<CleaningHistoryEntry> _history = [];
    private readonly object _lock = new();

    public LocalCleaningRuleService()
    {
        InitializeBuiltInTemplates();
    }

    #region Rule Management

    public Task<CleaningRule> CreateRuleAsync(CleaningRule rule, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _rules.Add(rule);
        }
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyList<CleaningRule>> GetRulesAsync(
        CleaningRuleScope? scope = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _rules.AsEnumerable();
            if (scope.HasValue)
                query = query.Where(r => r.Scope == scope.Value);
            if (!string.IsNullOrEmpty(projectId))
                query = query.Where(r => r.ProjectId == projectId || r.Scope == CleaningRuleScope.Global);

            return Task.FromResult<IReadOnlyList<CleaningRule>>(query.ToList());
        }
    }

    public Task<CleaningRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_rules.FirstOrDefault(r => r.Id == ruleId));
        }
    }

    public Task<CleaningRule> UpdateRuleAsync(CleaningRule rule, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _rules.FindIndex(r => r.Id == rule.Id);
            if (index >= 0)
                _rules[index] = rule;
            else
                _rules.Add(rule);
        }
        return Task.FromResult(rule);
    }

    public Task DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _rules.RemoveAll(r => r.Id == ruleId);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CleaningRule>> FindApplicableRulesAsync(
        Guid datasetId,
        DataProfile? profile = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var applicableRules = _rules
                .Where(r => r.IsEnabled)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.SuccessRate)
                .ToList();

            return Task.FromResult<IReadOnlyList<CleaningRule>>(applicableRules);
        }
    }

    #endregion

    #region Pipeline Management

    public Task<CleaningPipeline> CreatePipelineAsync(CleaningPipeline pipeline, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _pipelines.Add(pipeline);
        }
        return Task.FromResult(pipeline);
    }

    public Task<IReadOnlyList<CleaningPipeline>> GetPipelinesAsync(
        PipelineCategory? category = null,
        bool includeTemplates = true,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _pipelines.AsEnumerable();
            if (category.HasValue)
                query = query.Where(p => p.Category == category.Value);
            if (!includeTemplates)
                query = query.Where(p => !p.IsTemplate);

            return Task.FromResult<IReadOnlyList<CleaningPipeline>>(query.ToList());
        }
    }

    public Task<CleaningPipeline?> GetPipelineByIdAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_pipelines.FirstOrDefault(p => p.Id == pipelineId));
        }
    }

    public Task<CleaningPipeline> UpdatePipelineAsync(CleaningPipeline pipeline, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _pipelines.FindIndex(p => p.Id == pipeline.Id);
            if (index >= 0)
                _pipelines[index] = pipeline;
            else
                _pipelines.Add(pipeline);
        }
        return Task.FromResult(pipeline);
    }

    public Task DeletePipelineAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _pipelines.RemoveAll(p => p.Id == pipelineId);
        }
        return Task.CompletedTask;
    }

    public Task<CleaningPipeline> CreatePipelineVersionAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existing = _pipelines.FirstOrDefault(p => p.Id == pipelineId)
                ?? throw new InvalidOperationException($"Pipeline {pipelineId} not found.");

            var newVersion = existing with
            {
                Id = Guid.NewGuid(),
                Version = existing.Version + 1,
                ParentPipelineId = existing.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _pipelines.Add(newVersion);
            return Task.FromResult(newVersion);
        }
    }

    public Task<IReadOnlyList<CleaningPipeline>> GetIndustryTemplatesAsync(
        string industry,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var templates = _pipelines
                .Where(p => p.IsTemplate && string.Equals(p.Industry, industry, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult<IReadOnlyList<CleaningPipeline>>(templates);
        }
    }

    #endregion

    #region Self-Learning System

    public Task RecordUserActionAsync(UserCleaningAction action, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _userActions.Add(action);

            // Learn from action if successful
            if (action.WasSuccessful)
            {
                LearnFromAction(action);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CleaningRule>> GetLearnedRulesAsync(
        string? userId = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _rules.Where(r => r.IsLearned);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(r => r.CreatedByUserId == userId);
            if (!string.IsNullOrEmpty(projectId))
                query = query.Where(r => r.ProjectId == projectId);

            return Task.FromResult<IReadOnlyList<CleaningRule>>(query.ToList());
        }
    }

    public Task<IReadOnlyList<LearnedSuggestion>> GetLearnedSuggestionsAsync(
        Guid datasetId,
        DataProfile? profile = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var suggestions = new List<LearnedSuggestion>();

            // Analyze past actions to generate suggestions
            var actionsByColumn = _userActions
                .Where(a => a.WasSuccessful)
                .GroupBy(a => (a.ColumnDataType, a.OperationType))
                .ToList();

            foreach (var group in actionsByColumn)
            {
                var (dataType, operation) = group.Key;
                var successRate = group.Count(a => a.QualityImprovement > 0) / (double)group.Count();

                if (successRate > 0.6)
                {
                    suggestions.Add(new LearnedSuggestion
                    {
                        ColumnName = $"[{dataType} columns]",
                        SuggestedOperation = operation,
                        SuggestedParameters = group.First().Parameters,
                        ConfidenceScore = successRate,
                        Rationale = $"Based on {group.Count()} successful applications",
                        TimesPreviouslyUsed = group.Count(),
                        PreviousSuccessRate = successRate
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<LearnedSuggestion>>(suggestions);
        }
    }

    public Task TrainLearningModelAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var actions = string.IsNullOrEmpty(userId)
                ? _userActions
                : _userActions.Where(a => a.UserId == userId).ToList();

            // Group successful actions and create rules
            var patterns = actions
                .Where(a => a.WasSuccessful && a.QualityImprovement > 0)
                .GroupBy(a => (a.ColumnDataType, a.OperationType))
                .Where(g => g.Count() >= 3)
                .ToList();

            foreach (var pattern in patterns)
            {
                var existingRule = _rules.FirstOrDefault(r =>
                    r.IsLearned &&
                    r.Conditions.Any(c => c.DataType == pattern.Key.ColumnDataType) &&
                    r.Actions.Any(a => a.OperationType == pattern.Key.OperationType));

                if (existingRule == null)
                {
                    var rule = new CleaningRule
                    {
                        Name = $"Learned: {pattern.Key.OperationType} for {pattern.Key.ColumnDataType}",
                        Description = $"Automatically learned from {pattern.Count()} user actions",
                        Type = CleaningRuleType.ColumnBased,
                        Scope = string.IsNullOrEmpty(userId) ? CleaningRuleScope.Global : CleaningRuleScope.User,
                        IsLearned = true,
                        TimesApplied = pattern.Count(),
                        SuccessRate = pattern.Average(a => a.QualityImprovement ?? 0) > 0 ? 0.85 : 0.5,
                        CreatedByUserId = userId,
                        Conditions =
                        [
                            new RuleCondition { Type = RuleConditionType.DataType, DataType = pattern.Key.ColumnDataType }
                        ],
                        Actions =
                        [
                            new RuleAction
                            {
                                OperationType = pattern.Key.OperationType,
                                Parameters = pattern.First().Parameters
                            }
                        ]
                    };

                    _rules.Add(rule);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<LearningStatistics> GetLearningStatisticsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var actions = string.IsNullOrEmpty(userId)
                ? _userActions
                : _userActions.Where(a => a.UserId == userId).ToList();

            var learnedRules = string.IsNullOrEmpty(userId)
                ? _rules.Where(r => r.IsLearned)
                : _rules.Where(r => r.IsLearned && r.CreatedByUserId == userId);

            var operationFrequency = actions
                .GroupBy(a => a.OperationType)
                .ToDictionary(g => g.Key, g => g.Count());

            var patterns = actions
                .Where(a => a.WasSuccessful)
                .GroupBy(a => (a.ColumnDataType, a.OperationType))
                .Where(g => g.Count() >= 2)
                .Select(g => new PopularPattern
                {
                    Description = $"{g.Key.OperationType} for {g.Key.ColumnDataType}",
                    ApplicableColumnType = g.Key.ColumnDataType,
                    Operation = g.Key.OperationType,
                    TimesObserved = g.Count(),
                    SuccessRate = g.Count(a => a.QualityImprovement > 0) / (double)g.Count()
                })
                .OrderByDescending(p => p.TimesObserved)
                .Take(10)
                .ToList();

            return Task.FromResult(new LearningStatistics
            {
                TotalActionsRecorded = actions.Count,
                RulesLearned = learnedRules.Count(),
                PatternsDetected = patterns.Count,
                AverageSuggestionAccuracy = learnedRules.Any() ? learnedRules.Average(r => r.SuccessRate) : 0,
                OperationFrequency = operationFrequency,
                PopularPatterns = patterns,
                LastTrainingDate = actions.Any() ? actions.Max(a => a.PerformedAt) : DateTime.MinValue
            });
        }
    }

    #endregion

    #region Version Control

    public Task<IReadOnlyList<CleaningHistoryEntry>> GetCleaningHistoryAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CleaningHistoryEntry>>(
                _history.Where(h => h.DatasetId == datasetId).OrderByDescending(h => h.CreatedAt).ToList());
        }
    }

    public Task<CleaningHistoryEntry> CreateSnapshotAsync(
        Guid datasetId,
        string description,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existingVersions = _history.Where(h => h.DatasetId == datasetId).ToList();
            var newVersion = existingVersions.Count + 1;

            var entry = new CleaningHistoryEntry
            {
                DatasetId = datasetId,
                Version = newVersion,
                Action = CleaningHistoryAction.Snapshot,
                Description = description,
                UserId = "local-user"
            };

            _history.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task<CleaningDiff> GetDiffAsync(
        Guid datasetId,
        int version1,
        int version2,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CleaningDiff
        {
            DatasetId = datasetId,
            FromVersion = version1,
            ToVersion = version2,
            RowsAdded = 0,
            RowsRemoved = 0,
            RowsModified = 0,
            CellsModified = 0,
            ColumnDiffs = []
        });
    }

    public Task<CleaningHistoryEntry> RollbackToVersionAsync(
        Guid datasetId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var entry = new CleaningHistoryEntry
            {
                DatasetId = datasetId,
                Version = _history.Where(h => h.DatasetId == datasetId).Count() + 1,
                Action = CleaningHistoryAction.Reverted,
                Description = $"Rolled back to version {targetVersion}",
                UserId = "local-user"
            };

            _history.Add(entry);
            return Task.FromResult(entry);
        }
    }

    #endregion

    #region Templates

    public Task<IReadOnlyList<CleaningPipeline>> GetBuiltInTemplatesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CleaningPipeline>>(
                _pipelines.Where(p => p.IsTemplate && p.IsPublic).ToList());
        }
    }

    public Task<CleaningPipeline> CreateTemplateFromSessionAsync(
        Guid sessionId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var pipeline = new CleaningPipeline
        {
            Name = name,
            Description = description ?? "Created from cleaning session",
            IsTemplate = true,
            Steps = []
        };

        lock (_lock)
        {
            _pipelines.Add(pipeline);
        }

        return Task.FromResult(pipeline);
    }

    public Task<CleaningPipeline> ImportTemplateAsync(string templateJson, CancellationToken cancellationToken = default)
    {
        var pipeline = JsonSerializer.Deserialize<CleaningPipeline>(templateJson)
            ?? throw new InvalidOperationException("Invalid template JSON.");

        var imported = pipeline with { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

        lock (_lock)
        {
            _pipelines.Add(imported);
        }

        return Task.FromResult(imported);
    }

    public Task<string> ExportTemplateAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var pipeline = _pipelines.FirstOrDefault(p => p.Id == pipelineId)
                ?? throw new InvalidOperationException($"Pipeline {pipelineId} not found.");

            return Task.FromResult(JsonSerializer.Serialize(pipeline, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    #endregion

    #region Private Helpers

    private void InitializeBuiltInTemplates()
    {
        // Finance Template
        _pipelines.Add(new CleaningPipeline
        {
            Name = "Finance Data Cleaning",
            Description = "Standard cleaning pipeline for financial datasets",
            Category = PipelineCategory.Finance,
            Industry = "Finance",
            IsTemplate = true,
            IsPublic = true,
            Steps =
            [
                new PipelineStep { Order = 0, Name = "Remove duplicates", OperationType = CleaningOperationType.RemoveDuplicates },
                new PipelineStep { Order = 1, Name = "Normalize currency", OperationType = CleaningOperationType.NormalizeCurrency },
                new PipelineStep { Order = 2, Name = "Handle missing values", OperationType = CleaningOperationType.ImputeMedian },
                new PipelineStep { Order = 3, Name = "Detect outliers", OperationType = CleaningOperationType.CapOutliers }
            ]
        });

        // Healthcare Template
        _pipelines.Add(new CleaningPipeline
        {
            Name = "Healthcare Data Cleaning",
            Description = "HIPAA-compliant cleaning for healthcare data",
            Category = PipelineCategory.Healthcare,
            Industry = "Healthcare",
            IsTemplate = true,
            IsPublic = true,
            Steps =
            [
                new PipelineStep { Order = 0, Name = "Remove duplicates", OperationType = CleaningOperationType.RemoveDuplicates },
                new PipelineStep { Order = 1, Name = "Normalize dates", OperationType = CleaningOperationType.NormalizeDateTime },
                new PipelineStep { Order = 2, Name = "Handle missing values", OperationType = CleaningOperationType.ImputeKnn },
                new PipelineStep { Order = 3, Name = "Validate ranges", OperationType = CleaningOperationType.CapOutliers }
            ]
        });

        // E-commerce Template
        _pipelines.Add(new CleaningPipeline
        {
            Name = "E-commerce Data Cleaning",
            Description = "Cleaning pipeline for e-commerce and retail data",
            Category = PipelineCategory.Ecommerce,
            Industry = "Ecommerce",
            IsTemplate = true,
            IsPublic = true,
            Steps =
            [
                new PipelineStep { Order = 0, Name = "Remove duplicates", OperationType = CleaningOperationType.RemoveDuplicates },
                new PipelineStep { Order = 1, Name = "Clean text", OperationType = CleaningOperationType.TrimWhitespace },
                new PipelineStep { Order = 2, Name = "Normalize currency", OperationType = CleaningOperationType.NormalizeCurrency },
                new PipelineStep { Order = 3, Name = "Handle missing values", OperationType = CleaningOperationType.ImputeMode }
            ]
        });

        // General Template
        _pipelines.Add(new CleaningPipeline
        {
            Name = "General Data Cleaning",
            Description = "Universal cleaning pipeline for any dataset",
            Category = PipelineCategory.General,
            IsTemplate = true,
            IsPublic = true,
            Aggressiveness = CleaningAggressiveness.Balanced,
            Steps =
            [
                new PipelineStep { Order = 0, Name = "Drop high-missing columns", OperationType = CleaningOperationType.DropMissingColumns, Parameters = new Dictionary<string, object> { ["threshold"] = 0.7 } },
                new PipelineStep { Order = 1, Name = "Remove duplicates", OperationType = CleaningOperationType.RemoveDuplicates },
                new PipelineStep { Order = 2, Name = "Trim whitespace", OperationType = CleaningOperationType.TrimWhitespace },
                new PipelineStep { Order = 3, Name = "Handle missing values", OperationType = CleaningOperationType.ImputeMedian },
                new PipelineStep { Order = 4, Name = "Cap outliers", OperationType = CleaningOperationType.CapOutliers }
            ]
        });
    }

    private void LearnFromAction(UserCleaningAction action)
    {
        // Find or create a pattern for this action
        var existingRule = _rules.FirstOrDefault(r =>
            r.IsLearned &&
            r.Conditions.Any(c => c.DataType == action.ColumnDataType) &&
            r.Actions.Any(a => a.OperationType == action.OperationType));

        if (existingRule != null)
        {
            var index = _rules.IndexOf(existingRule);
            _rules[index] = existingRule with
            {
                TimesApplied = existingRule.TimesApplied + 1,
                LastAppliedAt = DateTime.UtcNow,
                SuccessRate = (existingRule.SuccessRate * existingRule.TimesApplied + (action.QualityImprovement > 0 ? 1 : 0)) / (existingRule.TimesApplied + 1)
            };
        }
    }

    #endregion
}
