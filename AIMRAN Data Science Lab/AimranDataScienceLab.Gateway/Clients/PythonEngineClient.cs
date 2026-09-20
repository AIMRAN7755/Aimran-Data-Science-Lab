using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// HTTP client that communicates with the Python FastAPI AI Engine.
/// </summary>
internal sealed class PythonEngineClient : IPythonEngineClient
{
    private readonly HttpClient _http;
    private readonly PythonEngineConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public PythonEngineClient(HttpClient httpClient, IOptions<GatewayOptions> options)
    {
        _http = httpClient;
        _config = options.Value.PythonEngine;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    #region Health

    public async Task<EngineHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await _http.GetAsync(_config.HealthEndpoint, cancellationToken);
            sw.Stop();

            return new EngineHealthStatus
            {
                EngineName = "Python AI Engine",
                IsHealthy = response.IsSuccessStatusCode,
                ResponseTime = sw.Elapsed,
                Version = response.Headers.TryGetValues("X-Engine-Version", out var values) ? values.FirstOrDefault() : null
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            return new EngineHealthStatus
            {
                EngineName = "Python AI Engine",
                IsHealthy = false,
                ResponseTime = sw.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Experiment Operations

    public async Task<PythonExperimentResult> SubmitExperimentAsync(
        SubmitExperimentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            experiment_id = request.ExperimentId.ToString(),
            name = request.Name,
            dataset_path = request.DatasetPath,
            hyperparameters = request.Hyperparameters,
            compute_target = request.ComputeTarget
        };

        using var response = await _http.PostAsJsonAsync("/api/experiments/submit", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonExperimentResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null experiment result.");
    }

    public async Task<PythonExperimentStatus> GetExperimentStatusAsync(
        string experimentRunId,
        CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<PythonExperimentStatus>(
            $"/api/experiments/{experimentRunId}/status", _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null experiment status.");
    }

    public async Task CancelExperimentAsync(string experimentRunId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"/api/experiments/{experimentRunId}/cancel", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async IAsyncEnumerable<ExperimentMetricUpdate> StreamMetricsAsync(
        string experimentRunId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            $"/api/experiments/{experimentRunId}/metrics/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var update in JsonSerializer.DeserializeAsyncEnumerable<ExperimentMetricUpdate>(
            stream, _jsonOptions, cancellationToken))
        {
            if (update is not null)
            {
                yield return update;
            }
        }
    }

    #endregion

    #region Model Operations

    public async Task<PythonTrainResult> TrainModelAsync(
        TrainModelRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/api/models/train", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonTrainResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null training result.");
    }

    public async Task<PythonEvalResult> EvaluateModelAsync(
        string modelPath,
        string testDatasetPath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { model_path = modelPath, test_dataset_path = testDatasetPath };
        using var response = await _http.PostAsJsonAsync("/api/models/evaluate", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonEvalResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null evaluation result.");
    }

    public async Task<PythonPredictionResult> PredictAsync(
        string modelPath,
        IReadOnlyList<IReadOnlyDictionary<string, object>> inputData,
        CancellationToken cancellationToken = default)
    {
        var payload = new { model_path = modelPath, input_data = inputData };
        using var response = await _http.PostAsJsonAsync("/api/models/predict", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonPredictionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null prediction result.");
    }

    #endregion

    #region Data Profiling

    public async Task<PythonProfileResult> ProfileDatasetAsync(
        string datasetPath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath };
        using var response = await _http.PostAsJsonAsync("/api/profiling/profile", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonProfileResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null profile result.");
    }

    public async Task<PythonOutlierResult> DetectOutliersAsync(
        string datasetPath,
        string method,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath, method, parameters };
        using var response = await _http.PostAsJsonAsync("/api/profiling/outliers", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonOutlierResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null outlier result.");
    }

    #endregion

    #region Cleaning Intelligence

    public async Task<PythonCleaningRecommendation> GetCleaningRecommendationsAsync(
        string datasetPath,
        object? existingProfile = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath, existing_profile = existingProfile };
        using var response = await _http.PostAsJsonAsync("/api/cleaning/recommend", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonCleaningRecommendation>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null cleaning recommendation.");
    }

    #endregion

    #region Code Execution

    public async Task<PythonCodeExecutionResult> ExecuteCodeAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { code, timeout_seconds = timeoutSeconds, working_directory = workingDirectory };
        using var response = await _http.PostAsJsonAsync("/api/code/execute", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonCodeExecutionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null execution result.");
    }

    #endregion

    #region Package Management

    public async Task<PythonPackageListResult> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<PythonPackageListResult>("/api/code/packages", _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null package list.");
    }

    public async Task<PythonPackageActionResult> InstallPackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { package_name = packageName, version };
        using var response = await _http.PostAsJsonAsync("/api/code/packages/install", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonPackageActionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null install result.");
    }

    public async Task<PythonPackageActionResult> UninstallPackageAsync(
        string packageName,
        CancellationToken cancellationToken = default)
    {
        var payload = new { package_name = packageName };
        using var response = await _http.PostAsJsonAsync("/api/code/packages/uninstall", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonPackageActionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null uninstall result.");
    }

    #endregion

    #region Visualization

    public async Task<PythonVisualizationResult> GenerateChartAsync(
        PythonVisualizationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/api/visualization/generate", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonVisualizationResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Python engine returned null visualization result.");
    }

    #endregion
}
