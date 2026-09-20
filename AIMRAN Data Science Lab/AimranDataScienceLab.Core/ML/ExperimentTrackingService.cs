using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.ML
{
    /// <summary>
    /// Represents an experiment run.
    /// </summary>
    public class ExperimentRun
    {
        public string RunId { get; set; } = "";
        public string ExperimentId { get; set; } = "";
        public string RunName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, float> Metrics { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public Dictionary<string, string> Artifacts { get; set; } = new();
        public string Status { get; set; } = "Running"; // Running, Finished, Failed
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long DurationMs { get; set; }
        public string UserId { get; set; } = "";
    }

    public class Experiment
    {
        public string ExperimentId { get; set; } = "";
        public string ExperimentName { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ExperimentRun> Runs { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
    }

    public class HyperparameterTrial
    {
        public string TrialId { get; set; } = "";
        public Dictionary<string, object> HyperparametersSet { get; set; } = new();
        public Dictionary<string, float> ResultMetrics { get; set; } = new();
        public string Status { get; set; } = ""; // Running, Complete, Failed, Pruned
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Experiment Tracking service with MLflow integration.
    /// Tracks experiments, runs, hyperparameters, and metrics.
    /// </summary>
    public interface IExperimentTrackingService
    {
        /// <summary>
        /// Creates a new experiment.
        /// </summary>
        Task<string> CreateExperimentAsync(Experiment experiment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a new experiment run.
        /// </summary>
        Task<string> StartRunAsync(string experimentId, string runName, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs parameters to a run.
        /// </summary>
        Task<bool> LogParametersAsync(string runId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs metrics to a run.
        /// </summary>
        Task<bool> LogMetricsAsync(string runId, Dictionary<string, float> metrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finishes a run.
        /// </summary>
        Task<bool> FinishRunAsync(string runId, string status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets run details.
        /// </summary>
        Task<ExperimentRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compares multiple runs.
        /// </summary>
        Task<Dictionary<string, object?>> CompareRunsAsync(List<string> runIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks hyperparameter optimization trials.
        /// </summary>
        Task<string> TrackHyperparameterTrialAsync(string experimentId, HyperparameterTrial trial, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Experiment Tracking service.
    /// </summary>
    public class ExperimentTrackingService : IExperimentTrackingService
    {
        private readonly ILogger<ExperimentTrackingService> _logger;
        private readonly Dictionary<string, Experiment> _experiments = new();
        private readonly Dictionary<string, ExperimentRun> _runs = new();
        private readonly Dictionary<string, List<HyperparameterTrial>> _trials = new();

        public ExperimentTrackingService(ILogger<ExperimentTrackingService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CreateExperimentAsync(Experiment experiment, CancellationToken cancellationToken = default)
        {
            try
            {
                var experimentId = experiment.ExperimentId ?? Guid.NewGuid().ToString();
                experiment.ExperimentId = experimentId;
                experiment.CreatedAt = DateTime.UtcNow;

                _experiments[experimentId] = experiment;
                _logger.LogInformation("Created experiment: {ExperimentName} ({ExperimentId})", 
                    experiment.ExperimentName, experimentId);

                return experimentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating experiment");
                throw;
            }
        }

        public async Task<string> StartRunAsync(string experimentId, string runName, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_experiments.ContainsKey(experimentId))
                    throw new ArgumentException($"Experiment {experimentId} not found");

                var runId = Guid.NewGuid().ToString();
                var run = new ExperimentRun
                {
                    RunId = runId,
                    ExperimentId = experimentId,
                    RunName = runName,
                    StartTime = DateTime.UtcNow,
                    UserId = userId,
                    Status = "Running"
                };

                _runs[runId] = run;
                _experiments[experimentId].Runs.Add(run);

                _logger.LogInformation("Started run {RunName} ({RunId}) in experiment {ExperimentId}", 
                    runName, runId, experimentId);

                return runId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting run");
                throw;
            }
        }

        public async Task<bool> LogParametersAsync(string runId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_runs.TryGetValue(runId, out var run))
                {
                    foreach (var param in parameters)
                    {
                        run.Parameters[param.Key] = param.Value;
                    }

                    _logger.LogInformation("Logged {Count} parameters to run {RunId}", parameters.Count, runId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging parameters");
                throw;
            }
        }

        public async Task<bool> LogMetricsAsync(string runId, Dictionary<string, float> metrics, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_runs.TryGetValue(runId, out var run))
                {
                    foreach (var metric in metrics)
                    {
                        run.Metrics[metric.Key] = metric.Value;
                    }

                    _logger.LogInformation("Logged {Count} metrics to run {RunId}", metrics.Count, runId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging metrics");
                throw;
            }
        }

        public async Task<bool> FinishRunAsync(string runId, string status, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_runs.TryGetValue(runId, out var run))
                {
                    run.EndTime = DateTime.UtcNow;
                    run.DurationMs = (long)(run.EndTime.Value - run.StartTime).TotalMilliseconds;
                    run.Status = status;

                    _logger.LogInformation("Finished run {RunId} with status {Status} ({Duration}ms)", 
                        runId, status, run.DurationMs);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing run");
                throw;
            }
        }

        public async Task<ExperimentRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_runs.TryGetValue(runId, out var run))
                {
                    _logger.LogInformation("Retrieved run {RunId}", runId);
                    return run;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run");
                throw;
            }
        }

        public async Task<Dictionary<string, object?>> CompareRunsAsync(List<string> runIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var comparison = new Dictionary<string, object?>
                {
                    { "runCount", runIds.Count },
                    { "runs", new List<object>() }
                };

                foreach (var runId in runIds)
                {
                    if (_runs.TryGetValue(runId, out var run))
                    {
                        ((List<object>)comparison["runs"]!).Add(new
                        {
                            run.RunId,
                            run.RunName,
                            MetricsCount = run.Metrics.Count,
                            ParametersCount = run.Parameters.Count
                        });
                    }
                }

                _logger.LogInformation("Compared {Count} experiment runs", runIds.Count);
                return comparison;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing runs");
                throw;
            }
        }

        public async Task<string> TrackHyperparameterTrialAsync(string experimentId, HyperparameterTrial trial, CancellationToken cancellationToken = default)
        {
            try
            {
                var trialId = trial.TrialId ?? Guid.NewGuid().ToString();
                trial.TrialId = trialId;

                if (!_trials.ContainsKey(experimentId))
                    _trials[experimentId] = new List<HyperparameterTrial>();

                _trials[experimentId].Add(trial);

                _logger.LogInformation("Tracked hyperparameter trial {TrialId} for experiment {ExperimentId}", 
                    trialId, experimentId);

                return trialId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking hyperparameter trial");
                throw;
            }
        }
    }
}
