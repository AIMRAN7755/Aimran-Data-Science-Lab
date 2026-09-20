using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// HTTP client that communicates with the Rust high-performance Resource Engine.
/// </summary>
internal sealed class RustEngineClient : IRustEngineClient
{
    private readonly HttpClient _http;
    private readonly RustEngineConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public RustEngineClient(HttpClient httpClient, IOptions<GatewayOptions> options)
    {
        _http = httpClient;
        _config = options.Value.RustEngine;
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
                EngineName = "Rust Resource Engine",
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
                EngineName = "Rust Resource Engine",
                IsHealthy = false,
                ResponseTime = sw.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Resource Monitoring

    public async Task<RustResourceSnapshot> GetResourceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<RustResourceSnapshot>(
            "/api/resources/snapshot", _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Rust engine returned null resource snapshot.");
    }

    public async IAsyncEnumerable<RustResourceSnapshot> StreamResourcesAsync(
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var intervalMs = (int)interval.TotalMilliseconds;
        using var response = await _http.GetAsync(
            $"/api/resources/stream?interval_ms={intervalMs}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var snapshot in JsonSerializer.DeserializeAsyncEnumerable<RustResourceSnapshot>(
            stream, _jsonOptions, cancellationToken))
        {
            if (snapshot is not null)
            {
                yield return snapshot;
            }
        }
    }

    public async Task<RustGpuInfo?> DetectGpuAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("/api/resources/gpu", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RustGpuInfo>(_jsonOptions, cancellationToken);
    }

    #endregion

    #region Delta Computation

    public async Task<RustDeltaResult> ComputeDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { base_path = baseFilePath, target_path = targetFilePath };
        using var response = await _http.PostAsJsonAsync("/api/delta/compute", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RustDeltaResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Rust engine returned null delta result.");
    }

    public async Task<RustApplyDeltaResult> ApplyDeltaAsync(
        string baseFilePath,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            base_path = baseFilePath,
            delta_data = Convert.ToBase64String(deltaData),
            output_path = outputPath
        };
        using var response = await _http.PostAsJsonAsync("/api/delta/apply", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RustApplyDeltaResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Rust engine returned null delta apply result.");
    }

    public async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { file_path = filePath };
        using var response = await _http.PostAsJsonAsync("/api/util/hash", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HashResponse>(_jsonOptions, cancellationToken);
        return result?.Hash ?? throw new InvalidOperationException("Rust engine returned null hash.");
    }

    #endregion

    #region File I/O

    public async Task<RustCsvParseResult> ParseCsvAsync(
        string filePath,
        int maxRows = 0,
        CancellationToken cancellationToken = default)
    {
        var payload = new { file_path = filePath, max_rows = maxRows };
        using var response = await _http.PostAsJsonAsync("/api/io/parse-csv", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RustCsvParseResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Rust engine returned null CSV parse result.");
    }

    public async Task<string> ConvertFileAsync(
        string sourcePath,
        string targetFormat,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var payload = new { source_path = sourcePath, target_format = targetFormat, output_path = outputPath };
        using var response = await _http.PostAsJsonAsync("/api/io/convert", payload, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>(_jsonOptions, cancellationToken);
        return result?.OutputPath ?? outputPath;
    }

    #endregion

    private sealed record HashResponse(string Hash);
    private sealed record ConvertResponse(string OutputPath);
}
