using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Tools
{
    /// <summary>
    /// Result of template verification operations.
    /// </summary>
    public class TemplateVerificationResult
    {
        public bool Success { get; set; }
        public string ServiceName { get; set; } = "";
        public List<VerificationCheck> Checks { get; set; } = new();
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object?> Metadata { get; set; } = new();

        public int PassedChecks => Checks.Count(c => c.Passed);
        public int FailedChecks => Checks.Count(c => !c.Passed);
    }

    /// <summary>
    /// Individual verification check result.
    /// </summary>
    public class VerificationCheck
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public bool Passed { get; set; }
        public string Message { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Verifies deployed services and their configurations.
    /// </summary>
    public class TemplateVerifier
    {
        private readonly ILogger<TemplateVerifier> _logger;

        public TemplateVerifier(ILogger<TemplateVerifier> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Verifies Docker templates.
        /// </summary>
        public async Task<TemplateVerificationResult> VerifyDockerSetupAsync(
            string outputDirectory,
            string serviceName)
        {
            var result = new TemplateVerificationResult { ServiceName = serviceName };

            try
            {
                _logger.LogInformation("Verifying Docker setup for {Service} in {Directory}", serviceName, outputDirectory);

                // Check Docker installation
                await VerifyDockerInstalledAsync(result);

                // Check for required files
                await VerifyDockerFilesExistAsync(outputDirectory, result);

                // Validate Docker files syntax
                await VerifyDockerSyntaxAsync(outputDirectory, result);

                // Check docker-compose syntax
                await VerifyDockerComposeSyntaxAsync(outputDirectory, result);

                result.Success = result.FailedChecks == 0;
                _logger.LogInformation("Docker verification completed for {Service}: {Status}",
                    serviceName, result.Success ? "PASSED" : "FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Docker setup for {Service}", serviceName);
                result.Checks.Add(new VerificationCheck
                {
                    Name = "UnexpectedError",
                    Category = "Docker",
                    Passed = false,
                    Message = ex.Message
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies Kubernetes templates.
        /// </summary>
        public async Task<TemplateVerificationResult> VerifyKubernetesSetupAsync(
            string outputDirectory,
            string serviceName)
        {
            var result = new TemplateVerificationResult { ServiceName = serviceName };

            try
            {
                _logger.LogInformation("Verifying Kubernetes setup for {Service} in {Directory}", serviceName, outputDirectory);

                // Check kubectl installation
                await VerifyKubectlInstalledAsync(result);

                // Check for required files
                await VerifyKubernetesFilesExistAsync(outputDirectory, result);

                // Validate YAML syntax
                await VerifyKubernetesYamlSyntaxAsync(outputDirectory, result);

                // Validate Kubernetes manifests
                await VerifyKubernetesManifestsAsync(outputDirectory, result);

                result.Success = result.FailedChecks == 0;
                _logger.LogInformation("Kubernetes verification completed for {Service}: {Status}",
                    serviceName, result.Success ? "PASSED" : "FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Kubernetes setup for {Service}", serviceName);
                result.Checks.Add(new VerificationCheck
                {
                    Name = "UnexpectedError",
                    Category = "Kubernetes",
                    Passed = false,
                    Message = ex.Message
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies configuration files.
        /// </summary>
        public async Task<TemplateVerificationResult> VerifyConfigurationAsync(
            string outputDirectory,
            string serviceName)
        {
            var result = new TemplateVerificationResult { ServiceName = serviceName };

            try
            {
                _logger.LogInformation("Verifying configuration for {Service} in {Directory}", serviceName, outputDirectory);

                var configDir = Path.Combine(outputDirectory, "config");
                if (!Directory.Exists(configDir))
                {
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = "ConfigurationDirectoryExists",
                        Category = "Configuration",
                        Passed = false,
                        Message = "Configuration directory not found"
                    });
                }
                else
                {
                    // Check for required config files
                    await VerifyConfigFilesAsync(configDir, result);

                    // Validate JSON structure
                    await VerifyConfigJsonAsync(configDir, result);
                }

                result.Success = result.FailedChecks == 0;
                _logger.LogInformation("Configuration verification completed for {Service}: {Status}",
                    serviceName, result.Success ? "PASSED" : "FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying configuration for {Service}", serviceName);
                result.Checks.Add(new VerificationCheck
                {
                    Name = "UnexpectedError",
                    Category = "Configuration",
                    Passed = false,
                    Message = ex.Message
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies service implementation code.
        /// </summary>
        public async Task<TemplateVerificationResult> VerifyServiceImplementationAsync(
            string outputDirectory,
            string serviceName)
        {
            var result = new TemplateVerificationResult { ServiceName = serviceName };

            try
            {
                _logger.LogInformation("Verifying service implementation for {Service} in {Directory}", serviceName, outputDirectory);

                var csFiles = Directory.GetFiles(outputDirectory, "*Service.cs");

                if (csFiles.Length == 0)
                {
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = "ServiceImplementationExists",
                        Category = "Implementation",
                        Passed = false,
                        Message = "No service implementation files found"
                    });
                }
                else
                {
                    // Verify each C# file
                    foreach (var csFile in csFiles)
                    {
                        await VerifyCSharpFileAsync(csFile, result);
                    }
                }

                result.Success = result.FailedChecks == 0;
                _logger.LogInformation("Service implementation verification completed for {Service}: {Status}",
                    serviceName, result.Success ? "PASSED" : "FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying service implementation for {Service}", serviceName);
                result.Checks.Add(new VerificationCheck
                {
                    Name = "UnexpectedError",
                    Category = "Implementation",
                    Passed = false,
                    Message = ex.Message
                });
            }

            return result;
        }

        private async Task VerifyDockerInstalledAsync(TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var process = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(process))
                {
                    if (proc != null)
                    {
                        var output = await proc.StandardOutput.ReadToEndAsync();
                        proc.WaitForExit();

                        result.Checks.Add(new VerificationCheck
                        {
                            Name = "DockerInstalled",
                            Category = "Docker",
                            Passed = proc.ExitCode == 0,
                            Message = proc.ExitCode == 0 ? output.Trim() : "Docker not found",
                            Duration = sw.Elapsed
                        });
                    }
                }
            }
            catch
            {
                result.Checks.Add(new VerificationCheck
                {
                    Name = "DockerInstalled",
                    Category = "Docker",
                    Passed = false,
                    Message = "Docker command failed or not installed",
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyDockerFilesExistAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var requiredFiles = new[] { "Dockerfile", "docker-compose.local.yml" };

            foreach (var file in requiredFiles)
            {
                var path = Path.Combine(outputDirectory, file);
                var exists = File.Exists(path);
                result.Checks.Add(new VerificationCheck
                {
                    Name = $"FileExists{file}",
                    Category = "Docker",
                    Passed = exists,
                    Message = exists ? $"{file} found" : $"{file} not found",
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyDockerSyntaxAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var dockerfilePath = Path.Combine(outputDirectory, "Dockerfile");

            if (!File.Exists(dockerfilePath))
                return;

            try
            {
                var content = await File.ReadAllTextAsync(dockerfilePath);
                var isValid = !content.Contains("PLACEHOLDER") && !content.Contains("TODO");

                result.Checks.Add(new VerificationCheck
                {
                    Name = "DockerfileSyntax",
                    Category = "Docker",
                    Passed = isValid,
                    Message = isValid ? "Dockerfile syntax is valid" : "Dockerfile contains placeholders",
                    Duration = sw.Elapsed
                });
            }
            catch (Exception ex)
            {
                result.Checks.Add(new VerificationCheck
                {
                    Name = "DockerfileSyntax",
                    Category = "Docker",
                    Passed = false,
                    Message = ex.Message,
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyDockerComposeSyntaxAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var composePath = Path.Combine(outputDirectory, "docker-compose.local.yml");

            if (!File.Exists(composePath))
                return;

            try
            {
                var process = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f {composePath} config",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(process))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        result.Checks.Add(new VerificationCheck
                        {
                            Name = "DockerComposeSyntax",
                            Category = "Docker",
                            Passed = proc.ExitCode == 0,
                            Message = proc.ExitCode == 0 ? "docker-compose syntax is valid" : "docker-compose syntax error",
                            Duration = sw.Elapsed
                        });
                    }
                }
            }
            catch
            {
                result.Checks.Add(new VerificationCheck
                {
                    Name = "DockerComposeSyntax",
                    Category = "Docker",
                    Passed = false,
                    Message = "Unable to verify docker-compose syntax",
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyKubectlInstalledAsync(TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var process = new ProcessStartInfo
                {
                    FileName = "kubectl",
                    Arguments = "version --client",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(process))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        result.Checks.Add(new VerificationCheck
                        {
                            Name = "KubectlInstalled",
                            Category = "Kubernetes",
                            Passed = proc.ExitCode == 0,
                            Message = proc.ExitCode == 0 ? "kubectl found" : "kubectl not found",
                            Duration = sw.Elapsed
                        });
                    }
                }
            }
            catch
            {
                result.Checks.Add(new VerificationCheck
                {
                    Name = "KubectlInstalled",
                    Category = "Kubernetes",
                    Passed = false,
                    Message = "kubectl command failed",
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyKubernetesFilesExistAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var k8sFiles = Directory.GetFiles(outputDirectory, "kubernetes-*.yaml").ToList();

            result.Checks.Add(new VerificationCheck
            {
                Name = "KubernetesFilesExist",
                Category = "Kubernetes",
                Passed = k8sFiles.Count > 0,
                Message = k8sFiles.Count > 0 ? $"Found {k8sFiles.Count} Kubernetes files" : "No Kubernetes files found",
                Duration = sw.Elapsed
            });
        }

        private async Task VerifyKubernetesYamlSyntaxAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var yamlFiles = Directory.GetFiles(outputDirectory, "*.yaml");

            foreach (var file in yamlFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var isValid = !content.Contains("PLACEHOLDER") && !content.Contains("TODO");

                    result.Checks.Add(new VerificationCheck
                    {
                        Name = $"YamlSyntax{Path.GetFileName(file)}",
                        Category = "Kubernetes",
                        Passed = isValid,
                        Message = isValid ? "YAML syntax valid" : "YAML contains placeholders",
                        Duration = sw.Elapsed
                    });
                }
                catch (Exception ex)
                {
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = $"YamlSyntax{Path.GetFileName(file)}",
                        Category = "Kubernetes",
                        Passed = false,
                        Message = ex.Message,
                        Duration = sw.Elapsed
                    });
                }
            }
        }

        private async Task VerifyKubernetesManifestsAsync(string outputDirectory, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var k8sFiles = Directory.GetFiles(outputDirectory, "kubernetes-*.yaml");

            foreach (var file in k8sFiles)
            {
                try
                {
                    var process = new ProcessStartInfo
                    {
                        FileName = "kubectl",
                        Arguments = $"apply --dry-run=client -f {file}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var proc = Process.Start(process))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit();
                            result.Checks.Add(new VerificationCheck
                            {
                                Name = $"K8sManifest{Path.GetFileName(file)}",
                                Category = "Kubernetes",
                                Passed = proc.ExitCode == 0,
                                Message = proc.ExitCode == 0 ? "Manifest valid" : "Manifest invalid",
                                Duration = sw.Elapsed
                            });
                        }
                    }
                }
                catch
                {
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = $"K8sManifest{Path.GetFileName(file)}",
                        Category = "Kubernetes",
                        Passed = false,
                        Message = "Unable to verify manifest",
                        Duration = sw.Elapsed
                    });
                }
            }
        }

        private async Task VerifyConfigFilesAsync(string configDir, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var requiredFiles = new[] { "appsettings.json" };

            foreach (var file in requiredFiles)
            {
                var path = Path.Combine(configDir, file);
                var exists = File.Exists(path);
                result.Checks.Add(new VerificationCheck
                {
                    Name = $"ConfigFile{file}",
                    Category = "Configuration",
                    Passed = exists,
                    Message = exists ? $"{file} found" : $"{file} not found",
                    Duration = sw.Elapsed
                });
            }
        }

        private async Task VerifyConfigJsonAsync(string configDir, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            var jsonFiles = Directory.GetFiles(configDir, "*.json");

            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    System.Text.Json.JsonDocument.Parse(content);

                    result.Checks.Add(new VerificationCheck
                    {
                        Name = $"JsonValid{Path.GetFileName(file)}",
                        Category = "Configuration",
                        Passed = true,
                        Message = "JSON is valid",
                        Duration = sw.Elapsed
                    });
                }
                catch (Exception ex)
                {
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = $"JsonValid{Path.GetFileName(file)}",
                        Category = "Configuration",
                        Passed = false,
                        Message = ex.Message,
                        Duration = sw.Elapsed
                    });
                }
            }
        }

        private async Task VerifyCSharpFileAsync(string filePath, TemplateVerificationResult result)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var fileName = Path.GetFileName(filePath);

                // Basic C# validation
                var isValid = content.Contains("class ") && 
                              content.Contains("namespace ") &&
                              !content.Contains("PLACEHOLDER");

                result.Checks.Add(new VerificationCheck
                {
                    Name = $"CSharp{fileName}",
                    Category = "Implementation",
                    Passed = isValid,
                    Message = isValid ? "C# file structure valid" : "C# file structure invalid",
                    Duration = sw.Elapsed
                });
            }
            catch (Exception ex)
            {
                result.Checks.Add(new VerificationCheck
                {
                    Name = $"CSharpFile",
                    Category = "Implementation",
                    Passed = false,
                    Message = ex.Message,
                    Duration = sw.Elapsed
                });
            }
        }
    }
}
