using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AimranDataScienceLab.ServiceTemplates.Tools;

namespace AimranDataScienceLab.ServiceTemplates.Cli
{
    /// <summary>
    /// CLI tool for generating service templates
    /// Usage: ServiceTemplateGenerator.exe generate --name MyService --label my-service --output ./generated
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var rootCommand = BuildRootCommand(serviceProvider);
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error");
                return 1;
            }
        }

        private static RootCommand BuildRootCommand(ServiceProvider serviceProvider)
        {
            var rootCommand = new RootCommand("AIMRAN Service Template Generator");

            // Generate command
            var generateCommand = new Command("generate", "Generate new service templates")
            {
                new Option<string>(
                    new[] { "--name", "-n" },
                    "Service name (e.g., AnalystService)") { IsRequired = true },

                new Option<string>(
                    new[] { "--label", "-l" },
                    "Service label for deployment (e.g., analyst-service)") { IsRequired = true },

                new Option<string>(
                    new[] { "--image", "-i" },
                    "Docker image name (default: service name in lowercase)"),

                new Option<string>(
                    new[] { "--output", "-o" },
                    description: "Output directory for generated files",
                    getDefaultValue: () => "./generated") { IsRequired = false },

                new Option<int>(
                    new[] { "--replicas", "-r" },
                    description: "Number of replicas (default: 3)",
                    getDefaultValue: () => 3),

                new Option<int>(
                    new[] { "--max-replicas", "-m" },
                    description: "Maximum replicas for autoscaling (default: 10)",
                    getDefaultValue: () => 10),

                new Option<string>(
                    new[] { "--type", "-t" },
                    description: "Service type: stateless or stateful (default: stateless)",
                    getDefaultValue: () => "stateless"),

                new Option<string>(
                    new[] { "--cpu-request" },
                    description: "CPU request (default: 500m)"),

                new Option<string>(
                    new[] { "--cpu-limit" },
                    description: "CPU limit (default: 2)"),

                new Option<string>(
                    new[] { "--memory-request" },
                    description: "Memory request (default: 512Mi)"),

                new Option<string>(
                    new[] { "--memory-limit" },
                    description: "Memory limit (default: 2Gi)"),

                new Option<bool>(
                    new[] { "--docker-local" },
                    description: "Generate docker-compose local template (default: true)",
                    getDefaultValue: () => true),

                new Option<bool>(
                    new[] { "--docker-prod" },
                    description: "Generate docker-compose production template (default: true)",
                    getDefaultValue: () => true),

                new Option<bool>(
                    new[] { "--kubernetes" },
                    description: "Generate Kubernetes manifests (default: true)",
                    getDefaultValue: () => true),

                new Option<bool>(
                    new[] { "--windows-service" },
                    description: "Generate Windows Service template (default: true)",
                    getDefaultValue: () => true),
            };

            generateCommand.Handler = CommandHandler.Create<string, string, string, string, int, int, string,
                string, string, string, string, bool, bool, bool, bool>(
                async (name, label, image, output, replicas, maxReplicas, type,
                    cpuRequest, cpuLimit, memoryRequest, memoryLimit,
                    dockerLocal, dockerProd, kubernetes, windowsService) =>
            {
                return await HandleGenerateCommand(serviceProvider,
                    name, label, image, output, replicas, maxReplicas, type,
                    cpuRequest, cpuLimit, memoryRequest, memoryLimit,
                    dockerLocal, dockerProd, kubernetes, windowsService);
            });

            // List command
            var listCommand = new Command("list", "List available templates");
            listCommand.Handler = CommandHandler.Create(() => HandleListCommand());

            // Validate command
            var validateCommand = new Command("validate", "Validate generated templates")
            {
                new Option<string>(
                    new[] { "--path", "-p" },
                    "Path to generated templates") { IsRequired = true },
            };

            validateCommand.Handler = CommandHandler.Create<string>(
                (path) => HandleValidateCommand(path));

            rootCommand.AddCommand(generateCommand);
            rootCommand.AddCommand(listCommand);
            rootCommand.AddCommand(validateCommand);

            return rootCommand;
        }

        private static async Task<int> HandleGenerateCommand(ServiceProvider serviceProvider,
            string name, string label, string image, string output, int replicas, int maxReplicas, string type,
            string cpuRequest, string cpuLimit, string memoryRequest, string memoryLimit,
            bool dockerLocal, bool dockerProd, bool kubernetes, bool windowsService)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Generating templates for service: {ServiceName}", name);

                var @params = new ServiceGenerationParams
                {
                    ServiceName = name,
                    ServiceLabel = label,
                    ServiceId = Guid.NewGuid().ToString("N"),
                    ServiceImage = image ?? name.ToLowerInvariant(),
                    OutputDirectory = Path.GetFullPath(output),
                    ReplicasCount = replicas,
                    MaxReplicas = maxReplicas,
                    ServiceType = Enum.Parse<ServiceType>(type, ignoreCase: true),
                    CpuRequest = cpuRequest ?? "500m",
                    CpuLimit = cpuLimit ?? "2",
                    MemoryRequest = memoryRequest ?? "512Mi",
                    MemoryLimit = memoryLimit ?? "2Gi",
                    GenerateDockerComposeLocal = dockerLocal,
                    GenerateDockerComposeProduction = dockerProd,
                    GenerateKubernetes = kubernetes,
                    GenerateWindowsService = windowsService,
                };

                var templatesBaseDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "Deployment");

                var generator = new ServiceTemplateGenerator(
                    serviceProvider.GetRequiredService<ILogger<ServiceTemplateGenerator>>(),
                    templatesBaseDir);

                await generator.GenerateAsync(@params);

                logger.LogInformation("✓ Templates generated successfully to: {OutputPath}", @params.OutputDirectory);
                return 0;
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to generate templates");
                return 1;
            }
        }

        private static int HandleListCommand()
        {
            Console.WriteLine("Available service templates:");
            Console.WriteLine("  - ServiceBase: Base class for all services");
            Console.WriteLine("  - Docker: Docker and docker-compose templates");
            Console.WriteLine("  - Kubernetes: K8s manifests (Deployment/StatefulSet)");
            Console.WriteLine("  - Windows Service: Windows Service templates");
            Console.WriteLine("  - Configuration: Environment-specific configs");
            return 0;
        }

        private static int HandleValidateCommand(string path)
        {
            Console.WriteLine($"Validating templates in: {path}");

            var files = new[]
            {
                "Dockerfile",
                "docker-compose.local.yml",
                "kubernetes-*.yaml",
                "config/appsettings.json"
            };

            var allValid = true;
            foreach (var pattern in files)
            {
                var found = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                if (found.Length == 0)
                {
                    Console.WriteLine($"  ✗ Missing: {pattern}");
                    allValid = false;
                }
                else
                {
                    Console.WriteLine($"  ✓ Found: {pattern}");
                }
            }

            return allValid ? 0 : 1;
        }
    }
}
