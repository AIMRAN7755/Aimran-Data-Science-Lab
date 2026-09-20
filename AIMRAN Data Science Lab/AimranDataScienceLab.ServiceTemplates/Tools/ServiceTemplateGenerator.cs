using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Tools
{
    /// <summary>
    /// Service template parameters for code generation.
    /// </summary>
    public class ServiceGenerationParams
    {
        public string ServiceName { get; set; }
        public string ServiceLabel { get; set; }
        public string ServiceId { get; set; }
        public string ServiceImage { get; set; }
        public string Registry { get; set; } = "docker.io";
        public string Version { get; set; } = "1.0.0";

        // Deployment parameters
        public int ReplicasCount { get; set; } = 3;
        public int MinReplicas { get; set; } = 1;
        public int MaxReplicas { get; set; } = 10;

        // Resource parameters
        public string CpuRequest { get; set; } = "500m";
        public string CpuLimit { get; set; } = "2";
        public string MemoryRequest { get; set; } = "512Mi";
        public string MemoryLimit { get; set; } = "2Gi";

        // Storage parameters
        public string StorageSize { get; set; } = "10Gi";

        // Service type: stateless, stateful
        public ServiceType ServiceType { get; set; } = ServiceType.Stateless;

        // Target platforms
        public bool GenerateDockerComposeLocal { get; set; } = true;
        public bool GenerateDockerComposeProduction { get; set; } = true;
        public bool GenerateKubernetes { get; set; } = true;
        public bool GenerateWindowsService { get; set; } = true;

        // Target directory
        public string OutputDirectory { get; set; }
    }

    public enum ServiceType
    {
        Stateless,
        Stateful
    }

    /// <summary>
    /// Generates service templates for different deployment platforms.
    /// </summary>
    public class ServiceTemplateGenerator
    {
        private readonly ILogger<ServiceTemplateGenerator> _logger;
        private readonly string _templatesBaseDir;

        public ServiceTemplateGenerator(ILogger<ServiceTemplateGenerator> logger, string templatesBaseDir)
        {
            _logger = logger;
            _templatesBaseDir = templatesBaseDir;
        }

        /// <summary>
        /// Generates all templates for a new service based on parameters.
        /// </summary>
        public async Task GenerateAsync(ServiceGenerationParams @params)
        {
            try
            {
                _logger.LogInformation(
                    "Generating templates for service {ServiceName} (Type: {ServiceType})",
                    @params.ServiceName, @params.ServiceType);

                EnsureOutputDirectory(@params.OutputDirectory);

                if (@params.GenerateDockerComposeLocal)
                    await GenerateDockerComposeLocalAsync(@params);

                if (@params.GenerateDockerComposeProduction)
                    await GenerateDockerComposeProductionAsync(@params);

                if (@params.GenerateKubernetes)
                    await GenerateKubernetesAsync(@params);

                if (@params.GenerateWindowsService)
                    await GenerateWindowsServiceAsync(@params);

                // Generate shared files
                await GenerateConfigurationAsync(@params);
                await GenerateDockerfileAsync(@params);

                _logger.LogInformation("Templates generated successfully for {ServiceName}", @params.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating templates for {ServiceName}", @params.ServiceName);
                throw;
            }
        }

        private async Task GenerateDockerComposeLocalAsync(ServiceGenerationParams @params)
        {
            var content = ReplaceTemplateVariables(
                ReadTemplateFile("docker-compose.template.yml"),
                @params);

            var outputPath = Path.Combine(@params.OutputDirectory, "docker-compose.local.yml");
            await WriteFileAsync(outputPath, content);
            _logger.LogInformation("Generated docker-compose local template: {Path}", outputPath);
        }

        private async Task GenerateDockerComposeProductionAsync(ServiceGenerationParams @params)
        {
            var content = ReplaceTemplateVariables(
                ReadTemplateFile("docker-compose.production.template.yml"),
                @params);

            var outputPath = Path.Combine(@params.OutputDirectory, "docker-compose.production.yml");
            await WriteFileAsync(outputPath, content);
            _logger.LogInformation("Generated docker-compose production template: {Path}", outputPath);
        }

        private async Task GenerateKubernetesAsync(ServiceGenerationParams @params)
        {
            var template = @params.ServiceType == ServiceType.Stateless
                ? "service-template.yaml"
                : "statefulset-template.yaml";

            var content = ReplaceTemplateVariables(
                ReadTemplateFile($"Kubernetes/{template}"),
                @params);

            var outputPath = Path.Combine(@params.OutputDirectory, $"kubernetes-{@params.ServiceLabel}.yaml");
            await WriteFileAsync(outputPath, content);
            _logger.LogInformation("Generated Kubernetes template: {Path}", outputPath);
        }

        private async Task GenerateWindowsServiceAsync(ServiceGenerationParams @params)
        {
            var content = GenerateWindowsServiceCode(@params);
            var outputPath = Path.Combine(@params.OutputDirectory, $"WindowsService.cs");
            await WriteFileAsync(outputPath, content);
            _logger.LogInformation("Generated Windows Service template: {Path}", outputPath);
        }

        private async Task GenerateConfigurationAsync(ServiceGenerationParams @params)
        {
            var configDir = Path.Combine(@params.OutputDirectory, "config");
            Directory.CreateDirectory(configDir);

            // Generate appsettings files
            var appSettings = ReplaceTemplateVariables(
                ReadTemplateFile($"../Configuration/appsettings.template.json"),
                @params);
            await WriteFileAsync(Path.Combine(configDir, "appsettings.json"), appSettings);

            var devSettings = ReplaceTemplateVariables(
                ReadTemplateFile($"../Configuration/appsettings.Development.template.json"),
                @params);
            await WriteFileAsync(Path.Combine(configDir, "appsettings.Development.json"), devSettings);

            var prodSettings = ReplaceTemplateVariables(
                ReadTemplateFile($"../Configuration/appsettings.Production.template.json"),
                @params);
            await WriteFileAsync(Path.Combine(configDir, "appsettings.Production.json"), prodSettings);

            _logger.LogInformation("Generated configuration files");
        }

        private async Task GenerateDockerfileAsync(ServiceGenerationParams @params)
        {
            var content = ReplaceTemplateVariables(
                ReadTemplateFile("Docker/Dockerfile.template"),
                @params);

            var outputPath = Path.Combine(@params.OutputDirectory, "Dockerfile");
            await WriteFileAsync(outputPath, content);
            _logger.LogInformation("Generated Dockerfile: {Path}", outputPath);
        }

        private string GenerateWindowsServiceCode(ServiceGenerationParams @params)
        {
            return $@"using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AimranDataScienceLab.ServiceTemplates.Deployment;

namespace {@params.ServiceName}
{{
    /// <summary>
    /// Windows Service host for {@params.ServiceName}
    /// Auto-generated from template on {DateTime.UtcNow:O}
    /// </summary>
    public class {@params.ServiceName}WindowsService : WindowsServiceHost
    {{
        protected override IHostBuilder CreateHostBuilder(string[] args)
        {{
            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((context, services) =>
                {{
                    // Add your service dependencies here
                    services.AddLogging();

                    // TODO: Register your service implementation
                    // services.AddSingleton<I{@params.ServiceLabel}Service, {@params.ServiceLabel}Service>();
                }});
        }}
    }}
}}
";
        }

        private string ReplaceTemplateVariables(string template, ServiceGenerationParams @params)
        {
            var result = template;

            // Replace all template variables
            var replacements = new Dictionary<string, string>
            {
                { "SERVICE_NAME", @params.ServiceName },
                { "SERVICE_LABEL", @params.ServiceLabel },
                { "SERVICE_ID", @params.ServiceId },
                { "SERVICE_IMAGE", @params.ServiceImage },
                { "REGISTRY", @params.Registry },
                { "VERSION", @params.Version },
                { "REPLICAS_COUNT", @params.ReplicasCount.ToString() },
                { "MIN_REPLICAS", @params.MinReplicas.ToString() },
                { "MAX_REPLICAS", @params.MaxReplicas.ToString() },
                { "CPU_REQUEST", @params.CpuRequest },
                { "CPU_LIMIT", @params.CpuLimit },
                { "MEMORY_REQUEST", @params.MemoryRequest },
                { "MEMORY_LIMIT", @params.MemoryLimit },
                { "STORAGE_SIZE", @params.StorageSize },
            };

            foreach (var kvp in replacements)
            {
                result = result.Replace(kvp.Key, kvp.Value ?? "");
            }

            return result;
        }

        private string ReadTemplateFile(string relativePath)
        {
            var fullPath = Path.Combine(_templatesBaseDir, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Template file not found: {fullPath}");
            }

            return File.ReadAllText(fullPath);
        }

        private async Task WriteFileAsync(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content);
        }

        private void EnsureOutputDirectory(string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }
    }
}
