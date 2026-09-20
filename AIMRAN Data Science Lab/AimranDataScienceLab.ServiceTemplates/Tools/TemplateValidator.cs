using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Tools
{
    /// <summary>
    /// Result of template validation.
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object?> Summary { get; set; } = new();
    }

    /// <summary>
    /// Validates generated service templates for correctness and best practices.
    /// </summary>
    public class TemplateValidator
    {
        private readonly ILogger<TemplateValidator> _logger;

        public TemplateValidator(ILogger<TemplateValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates all generated templates for a service.
        /// </summary>
        public async Task<TemplateValidationResult> ValidateGeneratedTemplatesAsync(
            string outputDirectory)
        {
            var result = new TemplateValidationResult { IsValid = true };

            try
            {
                _logger.LogInformation("Validating templates in {Directory}", outputDirectory);

                // Validate Docker files
                await ValidateDockerFilesAsync(outputDirectory, result);

                // Validate Kubernetes files
                await ValidateKubernetesFilesAsync(outputDirectory, result);

                // Validate configuration files
                await ValidateConfigurationFilesAsync(outputDirectory, result);

                // Validate service implementation
                await ValidateServiceImplementationAsync(outputDirectory, result);

                result.IsValid = result.Errors.Count == 0;
                result.Summary["TotalErrors"] = result.Errors.Count;
                result.Summary["TotalWarnings"] = result.Warnings.Count;
                result.Summary["ValidatdAt"] = DateTime.UtcNow;

                if (result.IsValid)
                {
                    _logger.LogInformation("Template validation passed");
                }
                else
                {
                    _logger.LogError("Template validation failed with {ErrorCount} errors", result.Errors.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating templates");
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        private async Task ValidateDockerFilesAsync(
            string directory,
            TemplateValidationResult result)
        {
            var dockerfilePath = Path.Combine(directory, "Dockerfile");
            if (File.Exists(dockerfilePath))
            {
                var content = await File.ReadAllTextAsync(dockerfilePath);
                ValidateDocker(content, result);
            }

            var dcLocalPath = Path.Combine(directory, "docker-compose.local.yml");
            if (File.Exists(dcLocalPath))
            {
                var content = await File.ReadAllTextAsync(dcLocalPath);
                ValidateDockerCompose(content, result);
            }

            var dcProdPath = Path.Combine(directory, "docker-compose.production.yml");
            if (File.Exists(dcProdPath))
            {
                var content = await File.ReadAllTextAsync(dcProdPath);
                ValidateDockerCompose(content, result);
            }
        }

        private async Task ValidateKubernetesFilesAsync(
            string directory,
            TemplateValidationResult result)
        {
            var k8sFiles = Directory.GetFiles(directory, "kubernetes-*.yaml")
                .Concat(Directory.GetFiles(directory, "*-template.yaml"))
                .ToList();

            foreach (var filePath in k8sFiles)
            {
                var content = await File.ReadAllTextAsync(filePath);
                ValidateKubernetes(content, Path.GetFileName(filePath), result);
            }
        }

        private async Task ValidateConfigurationFilesAsync(
            string directory,
            TemplateValidationResult result)
        {
            var configDir = Path.Combine(directory, "config");
            if (Directory.Exists(configDir))
            {
                var jsonFiles = Directory.GetFiles(configDir, "*.json");
                foreach (var jsonFile in jsonFiles)
                {
                    var content = await File.ReadAllTextAsync(jsonFile);
                    ValidateJson(content, Path.GetFileName(jsonFile), result);
                }
            }
        }

        private async Task ValidateServiceImplementationAsync(
            string directory,
            TemplateValidationResult result)
        {
            var csFiles = Directory.GetFiles(directory, "*Service.cs");
            foreach (var csFile in csFiles)
            {
                var content = await File.ReadAllTextAsync(csFile);
                ValidateCSharp(content, Path.GetFileName(csFile), result);
            }
        }

        private void ValidateDocker(string content, TemplateValidationResult result)
        {
            // Check for required Dockerfile components
            if (!content.Contains("FROM "))
                result.Errors.Add("Dockerfile missing FROM directive");

            if (!content.Contains("WORKDIR "))
                result.Warnings.Add("Dockerfile should specify WORKDIR");

            if (!content.Contains("RUN "))
                result.Warnings.Add("Dockerfile should have RUN instructions");

            // Check for security issues
            if (content.Contains("USER root") && !content.Contains("USER "))
                result.Errors.Add("Dockerfile should not run as root");

            if (!Regex.IsMatch(content, @"HEALTHCHECK"))
                result.Warnings.Add("Dockerfile should include HEALTHCHECK");

            // Check for template variables
            if (Regex.IsMatch(content, @"\{\{.*?\}\}"))
                result.Errors.Add("Dockerfile contains unreplaced template variables");
        }

        private void ValidateDockerCompose(string content, TemplateValidationResult result)
        {
            try
            {
                // Basic YAML validation (check for required structure)
                if (!content.Contains("version:") && !content.Contains("version :"))
                    result.Errors.Add("docker-compose.yml missing version");

                if (!content.Contains("services:") && !content.Contains("services :"))
                    result.Errors.Add("docker-compose.yml missing services section");

                // Check for health checks
                if (!Regex.IsMatch(content, @"healthcheck:"))
                    result.Warnings.Add("Services in docker-compose should include healthcheck");

                // Check for unreplaced variables
                if (Regex.IsMatch(content, @"\$\{[A-Z_]+\}"))
                    result.Warnings.Add("Unreplaced environment variables found");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error parsing docker-compose: {ex.Message}");
            }
        }

        private void ValidateKubernetes(string content, string fileName, TemplateValidationResult result)
        {
            try
            {
                // Check for required Kubernetes fields
                if (!content.Contains("apiVersion:") && !content.Contains("apiVersion :"))
                    result.Errors.Add($"{fileName}: Missing apiVersion");

                if (!content.Contains("kind:") && !content.Contains("kind :"))
                    result.Errors.Add($"{fileName}: Missing kind");

                if (!content.Contains("metadata:") && !content.Contains("metadata :"))
                    result.Errors.Add($"{fileName}: Missing metadata");

                // Check for unreplaced template variables
                if (Regex.IsMatch(content, @"[A-Z_]{2,}(?=\s|$)"))
                {
                    var unreplaced = Regex.Matches(content, @"[A-Z_]{2,}(?=\s|$)")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .Distinct()
                        .ToList();

                    var knownPlaceholders = new[] { "CPU", "MEMORY", "REPLICAS", "SERVICE" };
                    var unknownPlaceholders = unreplaced
                        .Where(p => !knownPlaceholders.Any(kp => p.Contains(kp)))
                        .ToList();

                    if (unknownPlaceholders.Any())
                        result.Warnings.Add($"{fileName}: Possible unreplaced variables: {string.Join(", ", unknownPlaceholders)}");
                }

                // Check resource definitions
                if (content.Contains("kind: Deployment") || content.Contains("kind: Pod"))
                {
                    if (!content.Contains("resources:") && !content.Contains("resources :"))
                        result.Warnings.Add($"{fileName}: Pod specs should include resource limits");

                    if (!content.Contains("livenessProbe:") && !content.Contains("livenessProbe :"))
                        result.Warnings.Add($"{fileName}: Pods should include liveness probes");

                    if (!content.Contains("readinessProbe:") && !content.Contains("readinessProbe :"))
                        result.Warnings.Add($"{fileName}: Pods should include readiness probes");
                }

                // Check for security
                if (content.Contains("runAsUser: 0") || content.Contains("runAsUser : 0"))
                    result.Errors.Add($"{fileName}: Should not run containers as root (UID 0)");

                if (!content.Contains("securityContext:") && !content.Contains("securityContext :"))
                    result.Warnings.Add($"{fileName}: Should define security context");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating {fileName}: {ex.Message}");
            }
        }

        private void ValidateJson(string content, string fileName, TemplateValidationResult result)
        {
            try
            {
                JsonDocument.Parse(content);
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"{fileName}: Invalid JSON - {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{fileName}: Error parsing JSON - {ex.Message}");
            }
        }

        private void ValidateCSharp(string content, string fileName, TemplateValidationResult result)
        {
            // Check for required using statements
            if (!content.Contains("using System"))
                result.Warnings.Add($"{fileName}: Missing 'using System' statement");

            if (!content.Contains("namespace "))
                result.Errors.Add($"{fileName}: Missing namespace declaration");

            if (!content.Contains("class "))
                result.Errors.Add($"{fileName}: Missing class declaration");

            // Check for TODO comments (suggests incomplete implementation)
            var todoCount = Regex.Matches(content, @"//\s*TODO").Count;
            if (todoCount > 0)
                result.Warnings.Add($"{fileName}: Contains {todoCount} TODO comments - implementation may be incomplete");

            // Check for required methods
            if (!content.Contains("public override"))
                result.Warnings.Add($"{fileName}: Should override base class methods");
        }
    }
}
