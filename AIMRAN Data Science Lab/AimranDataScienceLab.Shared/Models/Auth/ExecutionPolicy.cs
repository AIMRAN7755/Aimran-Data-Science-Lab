namespace AIMRAN_Data_Science_Lab.Models.Auth;

/// <summary>
/// Defines resource limits for sandboxed code execution.
/// </summary>
public sealed record ExecutionPolicy
{
    /// <summary>Maximum execution time before forced termination.</summary>
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>Maximum memory in megabytes the execution process may consume.</summary>
    public int MaxMemoryMb { get; init; } = 2048;

    /// <summary>Maximum CPU percentage (0–100) the execution may use.</summary>
    public int MaxCpuPercent { get; init; } = 80;

    /// <summary>Whether network access is allowed during execution.</summary>
    public bool AllowNetworkAccess { get; init; } = true;

    /// <summary>Whether file-system writes outside the working directory are allowed.</summary>
    public bool AllowFileSystemWrite { get; init; } = true;

    /// <summary>
    /// Blocked code patterns that should be rejected before execution
    /// (e.g., os.system, subprocess, shutil.rmtree).
    /// </summary>
    public IReadOnlyList<string> BlockedPatterns { get; init; } = [];

    /// <summary>Default policy for interactive notebook cells.</summary>
    public static ExecutionPolicy Notebook => new()
    {
        MaxDuration = TimeSpan.FromMinutes(5),
        MaxMemoryMb = 1024,
        MaxCpuPercent = 70,
        AllowNetworkAccess = false,
        AllowFileSystemWrite = true,
        BlockedPatterns =
        [
            "os.system(", "subprocess.", "shutil.rmtree(",
            "exec(", "eval(", "__import__(",
            "system(", "unlink("
        ]
    };

    /// <summary>Default policy for model training jobs.</summary>
    public static ExecutionPolicy Training => new()
    {
        MaxDuration = TimeSpan.FromHours(2),
        MaxMemoryMb = 4096,
        MaxCpuPercent = 95,
        AllowNetworkAccess = true,
        AllowFileSystemWrite = true,
        BlockedPatterns = ["os.system(", "subprocess.call(", "shutil.rmtree("]
    };

    /// <summary>Restrictive policy for untrusted extension code.</summary>
    public static ExecutionPolicy Extension => new()
    {
        MaxDuration = TimeSpan.FromMinutes(1),
        MaxMemoryMb = 512,
        MaxCpuPercent = 50,
        AllowNetworkAccess = false,
        AllowFileSystemWrite = false,
        BlockedPatterns =
        [
            "os.system(", "subprocess.", "shutil.rmtree(",
            "exec(", "eval(", "__import__(",
            "system(", "unlink(", "File.Delete(",
            "Process.Start(", "Environment.Exit("
        ]
    };
}

/// <summary>
/// Result of a sandbox code validation check.
/// </summary>
public record CodeValidationResult
{
    public bool IsAllowed { get; init; }
    public IReadOnlyList<string> Violations { get; init; } = [];

    public static CodeValidationResult Allowed => new() { IsAllowed = true };

    public static CodeValidationResult Denied(IReadOnlyList<string> violations) =>
        new() { IsAllowed = false, Violations = violations };
}
