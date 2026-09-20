using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.Monitoring
{
    /// <summary>
    /// Structured log entry for observability.
    /// </summary>
    public class LogEntry
    {
        public string LogId { get; set; } = "";
        public string TraceId { get; set; } = "";
        public string SpanId { get; set; } = "";
        public string Level { get; set; } = ""; // Debug, Info, Warning, Error, Critical
        public string Component { get; set; } = "";
        public string Message { get; set; } = "";
        public Dictionary<string, object> Context { get; set; } = new();
        public string? Exception { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public long DurationMs { get; set; }
    }

    public class TraceSpan
    {
        public string SpanId { get; set; } = "";
        public string TraceId { get; set; } = "";
        public string ParentSpanId { get; set; } = "";
        public string OperationName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new();
        public List<LogEntry> Logs { get; set; } = new();
        public string Status { get; set; } = ""; // Success, Error, Unknown
    }

    public class CorrelationContext
    {
        public string TraceId { get; set; } = "";
        public string? SpanId { get; set; }
        public Dictionary<string, string> BaggageItems { get; set; } = new();
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Comprehensive observability service with distributed tracing and structured logging.
    /// Integration with ELK Stack and Jaeger.
    /// </summary>
    public interface IObservabilityService
    {
        /// <summary>
        /// Logs a structured entry.
        /// </summary>
        Task<string> LogAsync(LogEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new trace span.
        /// </summary>
        Task<TraceSpan> StartSpanAsync(string traceId, string operationName, string? parentSpanId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes a trace span.
        /// </summary>
        Task<bool> CompleteSpanAsync(string traceId, string spanId, string status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches logs by criteria.
        /// </summary>
        Task<List<LogEntry>> SearchLogsAsync(Dictionary<string, string> criteria, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a full trace with all spans.
        /// </summary>
        Task<List<TraceSpan>> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates correlation context for distributed tracing.
        /// </summary>
        CorrelationContext CreateCorrelationContext();

        /// <summary>
        /// Exports logs in ELK compatible format.
        /// </summary>
        Task<string> ExportLogsForELKAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of observability service.
    /// </summary>
    public class ObservabilityService : IObservabilityService
    {
        private readonly ILogger<ObservabilityService> _logger;
        private readonly Dictionary<string, List<LogEntry>> _logs = new();
        private readonly Dictionary<string, List<TraceSpan>> _traces = new();
        private readonly Dictionary<string, TraceSpan> _activeSpans = new();

        public ObservabilityService(ILogger<ObservabilityService> logger)
        {
            _logger = logger;
        }

        public async Task<string> LogAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            try
            {
                entry.LogId = entry.LogId ?? Guid.NewGuid().ToString();
                entry.Timestamp = DateTime.UtcNow;

                var key = entry.TraceId ?? "default";
                if (!_logs.ContainsKey(key))
                    _logs[key] = new List<LogEntry>();

                _logs[key].Add(entry);

                _logger.LogInformation("[{Level}] {Component}: {Message}", entry.Level, entry.Component, entry.Message);
                return entry.LogId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging entry");
                throw;
            }
        }

        public async Task<TraceSpan> StartSpanAsync(string traceId, string operationName, string? parentSpanId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var spanId = Guid.NewGuid().ToString();
                var span = new TraceSpan
                {
                    SpanId = spanId,
                    TraceId = traceId,
                    ParentSpanId = parentSpanId ?? "",
                    OperationName = operationName,
                    StartTime = DateTime.UtcNow
                };

                _activeSpans[spanId] = span;

                if (!_traces.ContainsKey(traceId))
                    _traces[traceId] = new List<TraceSpan>();

                _traces[traceId].Add(span);

                _logger.LogInformation("Started span {SpanId} for operation {OperationName} in trace {TraceId}", spanId, operationName, traceId);
                return span;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting span");
                throw;
            }
        }

        public async Task<bool> CompleteSpanAsync(string traceId, string spanId, string status, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_activeSpans.TryGetValue(spanId, out var span))
                {
                    span.EndTime = DateTime.UtcNow;
                    span.Status = status;
                    _activeSpans.Remove(spanId);

                    _logger.LogInformation("Completed span {SpanId} with status {Status}", spanId, status);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing span");
                throw;
            }
        }

        public async Task<List<LogEntry>> SearchLogsAsync(Dictionary<string, string> criteria, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = new List<LogEntry>();

                foreach (var logList in _logs.Values)
                {
                    foreach (var entry in logList)
                    {
                        if (entry.Timestamp < startTime || entry.Timestamp > endTime)
                            continue;

                        bool matches = true;
                        foreach (var criterion in criteria)
                        {
                            if (criterion.Key == "Level" && entry.Level != criterion.Value)
                                matches = false;
                            else if (criterion.Key == "Component" && entry.Component != criterion.Value)
                                matches = false;
                            else if (criterion.Key == "Message" && !entry.Message.Contains(criterion.Value))
                                matches = false;
                        }

                        if (matches)
                            results.Add(entry);
                    }
                }

                _logger.LogInformation("Found {Count} log entries matching criteria", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching logs");
                throw;
            }
        }

        public async Task<List<TraceSpan>> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_traces.TryGetValue(traceId, out var spans))
                {
                    _logger.LogInformation("Retrieved trace {TraceId} with {SpanCount} spans", traceId, spans.Count);
                    return spans;
                }

                return new List<TraceSpan>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trace");
                throw;
            }
        }

        public CorrelationContext CreateCorrelationContext()
        {
            return new CorrelationContext
            {
                TraceId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                BaggageItems = new Dictionary<string, string>()
            };
        }

        public async Task<string> ExportLogsForELKAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                var elkFormat = new System.Text.StringBuilder();
                elkFormat.AppendLine("{\"@timestamp\": \"" + DateTime.UtcNow.ToString("O") + "\", \"source\": \"aimran-dsl\", \"entries\": [");

                var entries = new List<LogEntry>();
                foreach (var logList in _logs.Values)
                {
                    entries.AddRange(logList.Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime));
                }

                var logLines = entries.Select(e => $"  {{\"timestamp\": \"{e.Timestamp:O}\", \"level\": \"{e.Level}\", \"message\": \"{e.Message}\"}}");
                elkFormat.AppendLine(string.Join(",\n", logLines));
                elkFormat.AppendLine("]}");

                var output = elkFormat.ToString();
                _logger.LogInformation("Exported logs for ELK Stack");
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting logs");
                throw;
            }
        }
    }
}
