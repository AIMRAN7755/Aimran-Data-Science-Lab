using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIMRAN_Data_Science_Lab;

/// <summary>
/// Initializes and manages engine resources on first run.
/// Extracts engines from embedded resources or downloads them.
/// </summary>
public static class EngineResourceInitializer
{
    private const string PythonVersion = "3.11";
    private const string RustVersion = "1.0.0";

    /// <summary>
    /// Initialize engine resources on app startup.
    /// Extracts or downloads engines to AppData\AIMRAN Lab\Engines
    /// </summary>
    public static async Task InitializeEnginesAsync()
    {
        try
        {
            var enginesPath = Environment.GetEnvironmentVariable("AIMRAN_ENGINES_PATH");
            if (string.IsNullOrWhiteSpace(enginesPath))
            {
                enginesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AIMRAN Lab",
                    "Engines"
                );
            }

            // Ensure base engines directory exists
            if (!Directory.Exists(enginesPath))
                Directory.CreateDirectory(enginesPath);

            // Check and initialize Python
            await InitializePythonAsync(enginesPath);

            // Check and initialize Rust
            await InitializeRustAsync(enginesPath);

            // Check and initialize R (optional)
            await InitializeRAsync(enginesPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Engine initialization error: {ex.Message}");
            // Don't fail app startup if engines fail to initialize
        }
    }

    private static async Task InitializePythonAsync(string enginesPath)
    {
        var pythonPath = Path.Combine(enginesPath, "python");

        // If Python already exists, skip
        if (Directory.Exists(pythonPath) && File.Exists(Path.Combine(pythonPath, "python.exe")))
        {
            System.Diagnostics.Debug.WriteLine("Python engine already initialized");
            return;
        }

        try
        {
            // Check if Python is available in system PATH
            if (HasPythonInPath())
            {
                System.Diagnostics.Debug.WriteLine("Python found in system PATH");
                // Create symlink or copy reference to system Python
                CreatePythonReference(pythonPath);
                return;
            }

            // Otherwise, Python will need to be installed separately or bundled
            System.Diagnostics.Debug.WriteLine("Python not found. User will need to install it.");
            CreatePythonInitScript(pythonPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Python initialization failed: {ex.Message}");
        }
    }

    private static async Task InitializeRustAsync(string enginesPath)
    {
        var rustPath = Path.Combine(enginesPath, "rust");
        var rustExe = Path.Combine(rustPath, "rust-engine.exe");

        // If Rust exe already exists, skip
        if (File.Exists(rustExe))
        {
            System.Diagnostics.Debug.WriteLine("Rust engine already initialized");
            return;
        }

        try
        {
            if (!Directory.Exists(rustPath))
                Directory.CreateDirectory(rustPath);

            // Create a placeholder or download instruction file
            var readmeFile = Path.Combine(rustPath, "README.txt");
            if (!File.Exists(readmeFile))
            {
                await File.WriteAllTextAsync(readmeFile, """
                    AIMRAN Data Science Lab - Rust Engine

                    The Rust performance engine (rust-engine.exe) should be placed in this folder.

                    To build from source:
                    1. Navigate to: rust-engine/ directory in the app installation
                    2. Run: cargo build --release
                    3. Copy target/release/rust-engine.exe to this folder

                    Or download pre-built binary from:
                    https://github.com/yourusername/aimran-rust-engine/releases
                    """);
            }

            System.Diagnostics.Debug.WriteLine("Rust engine placeholder created");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Rust initialization failed: {ex.Message}");
        }
    }

    private static async Task InitializeRAsync(string enginesPath)
    {
        var rPath = Path.Combine(enginesPath, "r");

        try
        {
            if (!Directory.Exists(rPath))
                Directory.CreateDirectory(rPath);

            // Check if R is in system PATH
            if (HasRInPath())
            {
                System.Diagnostics.Debug.WriteLine("R found in system PATH");
                return;
            }

            // Create setup instruction file
            var readmeFile = Path.Combine(rPath, "README.txt");
            if (!File.Exists(readmeFile))
            {
                await File.WriteAllTextAsync(readmeFile, """
                    AIMRAN Data Science Lab - R Engine

                    The R engine is optional. To enable:
                    1. Download and install R from https://cran.r-project.org/
                    2. R will be auto-detected from system PATH

                    Required R packages will be auto-installed on first use:
                    - plumber (for API)
                    - ggplot2 (for visualization)
                    - tidyverse (for data manipulation)
                    """);
            }

            System.Diagnostics.Debug.WriteLine("R engine placeholder created");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"R initialization failed: {ex.Message}");
        }
    }

    private static void CreatePythonReference(string pythonPath)
    {
        if (!Directory.Exists(pythonPath))
            Directory.CreateDirectory(pythonPath);

        var infoFile = Path.Combine(pythonPath, "python-path.txt");
        var pythonExePath = GetSystemPythonPath();
        if (!string.IsNullOrEmpty(pythonExePath))
        {
            File.WriteAllText(infoFile, pythonExePath);
        }
    }

    private static void CreatePythonInitScript(string pythonPath)
    {
        if (!Directory.Exists(pythonPath))
            Directory.CreateDirectory(pythonPath);

        var scriptFile = Path.Combine(pythonPath, "install-python.ps1");
        File.WriteAllText(scriptFile, """
            # Install Python from Microsoft Store or python.org
            # This script can be run manually if Python is not installed

            Write-Host "AIMRAN Lab - Python Setup"
            Write-Host ""
            Write-Host "Python is required for the AI engine (port 8100)."
            Write-Host ""
            Write-Host "Option 1: Install from Microsoft Store (Recommended)"
            Write-Host "  1. Open Microsoft Store"
            Write-Host "  2. Search for 'Python 3.11'"
            Write-Host "  3. Click Install"
            Write-Host ""
            Write-Host "Option 2: Install from python.org"
            Write-Host "  1. Visit https://www.python.org/downloads/"
            Write-Host "  2. Download Python 3.11 or later"
            Write-Host "  3. Run installer (check 'Add Python to PATH')"
            Write-Host ""
            Write-Host "After installing Python, run this command to set up dependencies:"
            Write-Host "  pip install -r ai-engine\requirements.txt"
            """);
    }

    private static bool HasPythonInPath()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(1000);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool HasRInPath()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "Rscript",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(1000);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string? GetSystemPythonPath()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"import sys; print(sys.executable)\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            if (process?.WaitForExit(1000) == true)
            {
                var output = process.StandardOutput.ReadToEnd()?.Trim();
                return string.IsNullOrEmpty(output) ? null : output;
            }
        }
        catch { }

        return null;
    }
}
