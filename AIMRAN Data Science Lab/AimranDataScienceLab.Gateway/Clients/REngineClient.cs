using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// HTTP client that communicates with the R Plumber statistical engine.
/// </summary>
internal sealed class REngineClient : IREngineClient
{
    private readonly HttpClient _http;
    private readonly REngineConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public REngineClient(HttpClient httpClient, IOptions<GatewayOptions> options)
    {
        _http = httpClient;
        _config = options.Value.REngine;
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
                EngineName = "R Plumber Engine",
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
                EngineName = "R Plumber Engine",
                IsHealthy = false,
                ResponseTime = sw.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Code Execution

    public async Task<RCodeExecutionResult> ExecuteCodeAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { code, timeout_seconds = timeoutSeconds, working_directory = workingDirectory };
        using var response = await _http.PostAsJsonAsync("/api/code/execute", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RCodeExecutionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null execution result.");
    }

    #endregion

    #region Package Management

    public async Task<RPackageListResult> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<RPackageListResult>("/api/code/packages", _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null package list.");
    }

    public async Task<RPackageActionResult> InstallPackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { package_name = packageName, version };
        using var response = await _http.PostAsJsonAsync("/api/code/packages/install", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RPackageActionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null install result.");
    }

    public async Task<RPackageActionResult> UninstallPackageAsync(
        string packageName,
        CancellationToken cancellationToken = default)
    {
        var payload = new { package_name = packageName };
        using var response = await _http.PostAsJsonAsync("/api/code/packages/uninstall", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RPackageActionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null uninstall result.");
    }

    #endregion

    #region Visualization

    public async Task<RVisualizationResult> GenerateChartAsync(
        RVisualizationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/api/visualization/generate", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RVisualizationResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null visualization result.");
    }

    #endregion

    #region Statistical Analysis

    public async Task<RProfileResult> ProfileDatasetAsync(
        string datasetPath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath };
        using var response = await _http.PostAsJsonAsync("/api/profiling/profile", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RProfileResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null profile result.");
    }

    public async Task<RCorrelationResult> ComputeCorrelationAsync(
        string datasetPath,
        string method = "pearson",
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath, method };
        using var response = await _http.PostAsJsonAsync("/api/statistics/correlation", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RCorrelationResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null correlation result.");
    }

    public async Task<RRegressionResult> RunRegressionAsync(
        string datasetPath,
        string targetColumn,
        IReadOnlyList<string> featureColumns,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath, target_column = targetColumn, feature_columns = featureColumns };
        using var response = await _http.PostAsJsonAsync("/api/statistics/regression", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RRegressionResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null regression result.");
    }

    public async Task<RTTestResult> RunTTestAsync(
        string datasetPath,
        string columnA,
        string columnB,
        bool paired = false,
        CancellationToken cancellationToken = default)
    {
        var payload = new { dataset_path = datasetPath, column_a = columnA, column_b = columnB, paired };
        using var response = await _http.PostAsJsonAsync("/api/statistics/ttest", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RTTestResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("R engine returned null t-test result.");
    }

    #endregion
}
