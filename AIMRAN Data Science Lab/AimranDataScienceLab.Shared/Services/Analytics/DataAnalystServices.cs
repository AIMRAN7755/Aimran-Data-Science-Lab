using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.Analytics;

namespace AimranDataScienceLab.Core.Services.Analytics;

/// <summary>
/// SQL Explorer Service for Data Analysts.
/// Enables SQL query execution, result visualization, and query management.
/// </summary>
public interface ISQLExplorerService
{
    Task<QueryResult> ExecuteQueryAsync(string sqlQuery, Guid projectId, Guid userId);
    Task<SavedQuery> SaveQueryAsync(SavedQuery query);
    Task<SavedQuery?> GetSavedQueryAsync(Guid queryId);
    Task<SavedQuery[]> GetProjectQueriesAsync(Guid projectId);
    Task DeleteQueryAsync(Guid queryId);
    Task<QuerySuggestion[]> GetQuerySuggestionsAsync(Guid projectId);
}

public class QueryResult
{
    public Guid Id { get; set; }
    public string[] ColumnNames { get; set; } = Array.Empty<string>();
    public object[][] Rows { get; set; } = Array.Empty<object[]>();
    public int TotalRows { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class SavedQuery
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public string[]? Tags { get; set; }
    public int ExecutionCount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuerySuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SqlTemplate { get; set; } = string.Empty;
    public string[] RelatedColumns { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Dashboard Builder Service for Data Analysts.
/// Enables creation, management, and sharing of analytical dashboards.
/// </summary>
public interface IDashboardBuilderService
{
    Task<AnalyticsDashboard> CreateDashboardAsync(AnalyticsDashboard dashboard);
    Task<AnalyticsDashboard?> GetDashboardAsync(Guid dashboardId);
    Task<AnalyticsDashboard[]> GetProjectDashboardsAsync(Guid projectId);
    Task UpdateDashboardAsync(AnalyticsDashboard dashboard);
    Task DeleteDashboardAsync(Guid dashboardId);

    Task<DashboardWidget> AddWidgetAsync(Guid dashboardId, DashboardWidget widget);
    Task UpdateWidgetAsync(Guid dashboardId, DashboardWidget widget);
    Task RemoveWidgetAsync(Guid dashboardId, Guid widgetId);

    Task<AnalyticsDashboard> PublishDashboardAsync(Guid dashboardId);
    Task<DashboardSnapshot> GetDashboardSnapshotAsync(Guid dashboardId);
}

public class DashboardSnapshot
{
    public Guid DashboardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<Guid, object> WidgetData { get; set; } = new();
    public DateTime SnapshotAt { get; set; }
}

/// <summary>
/// Report Generator Service for Data Analysts.
/// Produces professional reports with insights and recommendations.
/// </summary>
public interface IReportGeneratorService
{
    Task<AnalyticsReport> GenerateReportAsync(ReportRequest request);
    Task<byte[]> ExportReportAsPdfAsync(Guid reportId);
    Task<string> ExportReportAsMarkdownAsync(Guid reportId);
    Task<AnalyticsReport?> GetReportAsync(Guid reportId);
    Task<AnalyticsReport[]> GetProjectReportsAsync(Guid projectId);
    Task ScheduleReportAsync(AnalyticsReport report, ReportSchedule schedule);
}

public class ReportRequest
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid[] InsightReportIds { get; set; } = Array.Empty<Guid>();
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public Guid RequestedByUserId { get; set; }
}

public class ReportSchedule
{
    public ScheduleType Type { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string[] RecipientEmails { get; set; } = Array.Empty<string>();
}

public enum ScheduleType
{
    Daily,
    Weekly,
    Monthly,
    Quarterly
}

/// <summary>
/// Service for exploratory data analysis and statistical summaries.
/// </summary>
public interface IEDAService
{
    Task<DatasetProfile> ProfileDatasetAsync(Guid datasetId);
    Task<CorrelationAnalysis> AnalyzeCorrelationsAsync(Guid datasetId);
    Task<DistributionAnalysis[]> AnalyzeDistributionsAsync(Guid datasetId);
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(Guid datasetId);
    Task<string> GenerateEDASummaryAsync(Guid datasetId);
}

public class DatasetProfile
{
    public Guid DatasetId { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public string[] ColumnNames { get; set; } = Array.Empty<string>();
    public Dictionary<string, ColumnProfile> ColumnProfiles { get; set; } = new();
}

public class ColumnProfile
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int MissingCount { get; set; }
    public double MissingPercent { get; set; }
    public int UniqueCount { get; set; }
    public object? Mode { get; set; }
    public double? Mean { get; set; }
    public double? Median { get; set; }
    public double? StdDev { get; set; }
}

public class CorrelationAnalysis
{
    public string[][] Columns { get; set; } = Array.Empty<string[]>();
    public double[][] CorrelationMatrix { get; set; } = Array.Empty<double[]>();
    public HighCorrelation[] HighCorrelations { get; set; } = Array.Empty<HighCorrelation>();
}

public class HighCorrelation
{
    public string Column1 { get; set; } = string.Empty;
    public string Column2 { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
}

public class DistributionAnalysis
{
    public string ColumnName { get; set; } = string.Empty;
    public string DistributionType { get; set; } = string.Empty;
    public Dictionary<string, int> Histogram { get; set; } = new();
}

public class AnomalyDetectionResult
{
    public int TotalAnomalies { get; set; }
    public string[] AnomalyRows { get; set; } = Array.Empty<string>();
    public string[] AnomalyColumns { get; set; } = Array.Empty<string>();
}
