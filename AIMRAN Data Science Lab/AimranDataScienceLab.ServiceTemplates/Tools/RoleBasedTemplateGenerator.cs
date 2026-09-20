using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AimranDataScienceLab.ServiceTemplates.Tools;

namespace AimranDataScienceLab.ServiceTemplates.Tools
{
    /// <summary>
    /// Role type enumeration for AIMRAN services.
    /// </summary>
    public enum ServiceRole
    {
        /// <summary>Role-based services</summary>
        Analyst,
        Scientist,
        Engineer,
        AIEngineer,

        /// <summary>Infrastructure services</summary>
        Gateway,
        Registry,
        IPCBroker,
        Monitoring,

        /// <summary>Generic service</summary>
        Generic
    }

    /// <summary>
    /// Extended parameters for role-specific service generation.
    /// </summary>
    public class RoleSpecificGenerationParams : ServiceGenerationParams
    {
        /// <summary>
        /// Gets or sets the role type for this service.
        /// </summary>
        public ServiceRole ServiceRole { get; set; } = ServiceRole.Generic;

        /// <summary>
        /// Gets or sets role-specific environment variables.
        /// </summary>
        public Dictionary<string, string> RoleEnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Gets or sets role-specific features.
        /// </summary>
        public Dictionary<string, bool> RoleFeatures { get; set; } = new();

        /// <summary>
        /// Gets or sets role-specific resource requirements.
        /// </summary>
        public RoleResourceRequirements ResourceRequirements { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to generate role-specific base class implementation.
        /// </summary>
        public bool GenerateServiceImplementation { get; set; } = true;
    }

    /// <summary>
    /// Role-specific resource requirements.
    /// </summary>
    public class RoleResourceRequirements
    {
        public string CpuRequest { get; set; } = "500m";
        public string CpuLimit { get; set; } = "2";
        public string MemoryRequest { get; set; } = "512Mi";
        public string MemoryLimit { get; set; } = "2Gi";
        public string MinReplicas { get; set; } = "1";
        public string MaxReplicas { get; set; } = "10";
    }

    /// <summary>
    /// Enhanced service template generator with role-specific support.
    /// </summary>
    public class RoleBasedTemplateGenerator
    {
        private readonly ILogger<RoleBasedTemplateGenerator> _logger;
        private readonly ServiceTemplateGenerator _baseGenerator;

        public RoleBasedTemplateGenerator(ILogger<RoleBasedTemplateGenerator> logger, string templatesBaseDir)
        {
            _logger = logger;
            _baseGenerator = new ServiceTemplateGenerator(logger, templatesBaseDir);
        }

        /// <summary>
        /// Generates role-specific templates.
        /// </summary>
        public async Task GenerateRoleSpecificAsync(RoleSpecificGenerationParams @params)
        {
            try
            {
                _logger.LogInformation(
                    "Generating role-specific templates for {ServiceName} (Role: {Role})",
                    @params.ServiceName, @params.ServiceRole);

                // Apply role-specific defaults
                ApplyRoleDefaults(@params);

                // Generate base templates
                await _baseGenerator.GenerateAsync(@params);

                // Generate role-specific assets
                if (@params.GenerateServiceImplementation)
                {
                    await GenerateServiceImplementationAsync(@params);
                }

                // Generate role-specific configuration
                await GenerateRoleConfigurationAsync(@params);

                _logger.LogInformation(
                    "Role-specific templates generated successfully for {ServiceName}",
                    @params.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating role-specific templates for {ServiceName}", @params.ServiceName);
                throw;
            }
        }

        /// <summary>
        /// Applies role-specific default configurations.
        /// </summary>
        private void ApplyRoleDefaults(RoleSpecificGenerationParams @params)
        {
            var roleDefaults = GetRoleDefaults(@params.ServiceRole);

            // Apply CPU and memory defaults based on role
            @params.CpuRequest = roleDefaults.CpuRequest;
            @params.CpuLimit = roleDefaults.CpuLimit;
            @params.MemoryRequest = roleDefaults.MemoryRequest;
            @params.MemoryLimit = roleDefaults.MemoryLimit;
            @params.MinReplicas = int.Parse(roleDefaults.MinReplicas);
            @params.MaxReplicas = int.Parse(roleDefaults.MaxReplicas);

            // Merge role-specific environment variables
            if (@params.RoleEnvironmentVariables != null)
            {
                var envs = GetRoleEnvironmentVariables(@params.ServiceRole);
                foreach (var env in envs)
                {
                    if (!@params.RoleEnvironmentVariables.ContainsKey(env.Key))
                    {
                        @params.RoleEnvironmentVariables[env.Key] = env.Value;
                    }
                }
            }

            // Apply role-specific service type
            @params.ServiceType = GetRoleServiceType(@params.ServiceRole);
        }

        /// <summary>
        /// Gets default resource requirements for a role.
        /// </summary>
        private RoleResourceRequirements GetRoleDefaults(ServiceRole role) => role switch
        {
            ServiceRole.Analyst => new RoleResourceRequirements
            {
                CpuRequest = "500m",
                CpuLimit = "2",
                MemoryRequest = "512Mi",
                MemoryLimit = "2Gi",
                MinReplicas = "2",
                MaxReplicas = "10"
            },
            ServiceRole.Scientist => new RoleResourceRequirements
            {
                CpuRequest = "2000m",
                CpuLimit = "8",
                MemoryRequest = "4Gi",
                MemoryLimit = "16Gi",
                MinReplicas = "1",
                MaxReplicas = "5"
            },
            ServiceRole.Engineer => new RoleResourceRequirements
            {
                CpuRequest = "1000m",
                CpuLimit = "4",
                MemoryRequest = "2Gi",
                MemoryLimit = "8Gi",
                MinReplicas = "2",
                MaxReplicas = "10"
            },
            ServiceRole.AIEngineer => new RoleResourceRequirements
            {
                CpuRequest = "2000m",
                CpuLimit = "8",
                MemoryRequest = "4Gi",
                MemoryLimit = "16Gi",
                MinReplicas = "3",
                MaxReplicas = "20"
            },
            ServiceRole.Gateway => new RoleResourceRequirements
            {
                CpuRequest = "500m",
                CpuLimit = "2",
                MemoryRequest = "512Mi",
                MemoryLimit = "2Gi",
                MinReplicas = "3",
                MaxReplicas = "20"
            },
            ServiceRole.Registry => new RoleResourceRequirements
            {
                CpuRequest = "1000m",
                CpuLimit = "4",
                MemoryRequest = "1Gi",
                MemoryLimit = "4Gi",
                MinReplicas = "1",
                MaxReplicas = "3"
            },
            ServiceRole.IPCBroker => new RoleResourceRequirements
            {
                CpuRequest = "1000m",
                CpuLimit = "4",
                MemoryRequest = "2Gi",
                MemoryLimit = "8Gi",
                MinReplicas = "1",
                MaxReplicas = "5"
            },
            ServiceRole.Monitoring => new RoleResourceRequirements
            {
                CpuRequest = "500m",
                CpuLimit = "2",
                MemoryRequest = "1Gi",
                MemoryLimit = "4Gi",
                MinReplicas = "1",
                MaxReplicas = "3"
            },
            _ => new RoleResourceRequirements()
        };

        /// <summary>
        /// Gets environment variables for a role.
        /// </summary>
        private Dictionary<string, string> GetRoleEnvironmentVariables(ServiceRole role) => role switch
        {
            ServiceRole.Analyst => new Dictionary<string, string>
            {
                { "ANALYST_MODE", "true" },
                { "QUERY_CACHE_ENABLED", "true" },
                { "MAX_QUERY_TIMEOUT", "300" },
                { "DASHBOARD_ENABLED", "true" }
            },
            ServiceRole.Scientist => new Dictionary<string, string>
            {
                { "SCIENTIST_MODE", "true" },
                { "EXPERIMENT_TRACKING_ENABLED", "true" },
                { "MODEL_VERSIONING_ENABLED", "true" },
                { "DISTRIBUTED_TRAINING_ENABLED", "true" }
            },
            ServiceRole.Engineer => new Dictionary<string, string>
            {
                { "ENGINEER_MODE", "true" },
                { "PIPELINE_ORCHESTRATION_ENABLED", "true" },
                { "ETL_PROCESSING_ENABLED", "true" },
                { "DATA_VALIDATION_ENABLED", "true" }
            },
            ServiceRole.AIEngineer => new Dictionary<string, string>
            {
                { "AI_ENGINEER_MODE", "true" },
                { "INFERENCE_OPTIMIZATION_ENABLED", "true" },
                { "MODEL_VERSIONING_ENABLED", "true" },
                { "AB_TESTING_ENABLED", "true" }
            },
            ServiceRole.Gateway => new Dictionary<string, string>
            {
                { "GATEWAY_MODE", "true" },
                { "RATE_LIMITING_ENABLED", "true" },
                { "AUTHENTICATION_ENABLED", "true" },
                { "CACHING_ENABLED", "true" }
            },
            ServiceRole.Registry => new Dictionary<string, string>
            {
                { "REGISTRY_MODE", "true" },
                { "PERSISTENCE_ENABLED", "true" },
                { "REPLICATION_ENABLED", "true" },
                { "HEALTH_TRACKING_ENABLED", "true" }
            },
            ServiceRole.IPCBroker => new Dictionary<string, string>
            {
                { "IPC_BROKER_MODE", "true" },
                { "MESSAGE_PERSISTENCE_ENABLED", "true" },
                { "PUBLISH_SUBSCRIBE_ENABLED", "true" },
                { "DEAD_LETTER_QUEUE_ENABLED", "true" }
            },
            ServiceRole.Monitoring => new Dictionary<string, string>
            {
                { "MONITORING_MODE", "true" },
                { "METRICS_COLLECTION_ENABLED", "true" },
                { "LOG_AGGREGATION_ENABLED", "true" },
                { "TRACING_ENABLED", "true" }
            },
            _ => new Dictionary<string, string>()
        };

        /// <summary>
        /// Gets the service type (stateless/stateful) for a role.
        /// </summary>
        private ServiceType GetRoleServiceType(ServiceRole role) => role switch
        {
            ServiceRole.Registry => ServiceType.Stateful,
            ServiceRole.IPCBroker => ServiceType.Stateful,
            ServiceRole.Monitoring => ServiceType.Stateful,
            _ => ServiceType.Stateless
        };

        /// <summary>
        /// Generates role-specific service implementation code.
        /// </summary>
        private async Task GenerateServiceImplementationAsync(RoleSpecificGenerationParams @params)
        {
            var code = GenerateRoleServiceCode(@params);
            var outputPath = System.IO.Path.Combine(@params.OutputDirectory, $"{@params.ServiceName}Service.cs");

            await System.IO.File.WriteAllTextAsync(outputPath, code);
            _logger.LogInformation("Generated role-specific service implementation: {Path}", outputPath);
        }

        /// <summary>
        /// Generates role-specific configuration.
        /// </summary>
        private async Task GenerateRoleConfigurationAsync(RoleSpecificGenerationParams @params)
        {
            var configContent = GenerateRoleConfiguration(@params);
            var configDir = System.IO.Path.Combine(@params.OutputDirectory, "config");
            System.IO.Directory.CreateDirectory(configDir);

            var configPath = System.IO.Path.Combine(configDir, "role-config.json");
            await System.IO.File.WriteAllTextAsync(configPath, configContent);
            _logger.LogInformation("Generated role-specific configuration: {Path}", configPath);
        }

        private string GenerateRoleServiceCode(RoleSpecificGenerationParams @params)
        {
            var baseClass = GetRoleBaseClass(@params.ServiceRole);
            var capabilities = GetRoleCapabilities(@params.ServiceRole);

            return $@"using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AimranDataScienceLab.ServiceTemplates.Base;

namespace {@params.ServiceName}
{{
    /// <summary>
    /// {@params.ServiceName} Service - Auto-generated from template on {DateTime.UtcNow:O}
    /// </summary>
    public class {@params.ServiceName}Service : {baseClass}
    {{
        private readonly ILogger<{@params.ServiceName}Service> _logger;

        public {@params.ServiceName}Service(ILogger<{@params.ServiceName}Service> logger)
            : base(logger)
        {{
            _logger = logger;
        }}

        public override string ServiceId => ""{@params.ServiceId}"";
        public override string ServiceName => ""{@params.ServiceName}"";
        public override Version ServiceVersion => new(1, 0, 0);

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {{
            _logger.LogInformation(""Service {ServiceName} initializing"", ServiceName);
            // TODO: Implement role-specific initialization
            await Task.CompletedTask;
        }}

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {{
            _logger.LogInformation(""Service {ServiceName} starting"", ServiceName);
            // TODO: Implement role-specific startup logic
            await Task.CompletedTask;
        }}

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {{
            _logger.LogInformation(""Service {ServiceName} stopping"", ServiceName);
            // TODO: Implement role-specific cleanup
            await Task.CompletedTask;
        }}

        protected override async Task<ServiceHealthStatus> OnCheckHealthAsync(CancellationToken cancellationToken)
        {{
            return new ServiceHealthStatus
            {{
                IsHealthy = true,
                Status = ""Healthy"",
                Message = ""Service is operational"",
                LastChecked = DateTime.UtcNow
            }};
        }}

        protected override string GetServiceDescription() =>
            ""{@params.ServiceName} - {@params.ServiceRole} role service"";

        protected override string[] GetServiceDependencies() =>
            new[] {{
                // TODO: Add service-specific dependencies
            }};

        protected override string[] GetServiceCapabilities() =>
            new[] {{
                {capabilities}
            }};

        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {{
                {{ ""role"", ""{@params.ServiceRole.ToString().ToLower()}"" }},
                {{ ""version"", ServiceVersion.ToString() }},
                {{ ""environment"", ""development"" }}
            }};

        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {{
                MaxConcurrentRequests = 100,
                RequestTimeout = TimeSpan.FromSeconds(30),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Features = new()
                {{
                    // TODO: Add feature flags
                }}
            }};

        protected override string GetServiceDescription() =>
            ""{@params.ServiceRole} service: {@params.ServiceName}"";
    }}
}}
";
        }

        private string GenerateRoleConfiguration(RoleSpecificGenerationParams @params)
        {
            return $@"{{
  ""ServiceRole"": ""{@params.ServiceRole}"",
  ""ServiceName"": ""{@params.ServiceName}"",
  ""Version"": ""{@params.Version}"",
  ""Environment"": ""development"",
  ""EnvironmentVariables"": {{
{string.Join(",\n", @params.RoleEnvironmentVariables.Select(kvp => $"    \"{kvp.Key}\": \"{kvp.Value}\""))}
  }},
  ""ResourceRequirements"": {{
    ""CpuRequest"": ""{@params.CpuRequest}"",
    ""CpuLimit"": ""{@params.CpuLimit}"",
    ""MemoryRequest"": ""{@params.MemoryRequest}"",
    ""MemoryLimit"": ""{@params.MemoryLimit}""
  }},
  ""Deployment"": {{
    ""Replicas"": {@params.ReplicasCount},
    ""MinReplicas"": {@params.MinReplicas},
    ""MaxReplicas"": {@params.MaxReplicas},
    ""Strategy"": ""RollingUpdate""
  }}
}}
";
        }

        private string GetRoleBaseClass(ServiceRole role) => role switch
        {
            ServiceRole.Analyst => "AnalystServiceBase",
            ServiceRole.Scientist => "ScientistServiceBase",
            ServiceRole.Engineer => "EngineerServiceBase",
            ServiceRole.AIEngineer => "AIEngineerServiceBase",
            ServiceRole.Gateway => "GatewayServiceBase",
            ServiceRole.Registry => "RegistryServiceBase",
            ServiceRole.IPCBroker => "IPCBrokerServiceBase",
            ServiceRole.Monitoring => "MonitoringServiceBase",
            _ => "ServiceBase"
        };

        private string GetRoleCapabilities(ServiceRole role) => role switch
        {
            ServiceRole.Analyst => @"""SQL Execution"", ""Dashboard Management"", ""Report Generation"", ""Data Analysis""",
            ServiceRole.Scientist => @"""Model Training"", ""Experimentation"", ""Validation"", ""Metrics""",
            ServiceRole.Engineer => @"""Pipeline Orchestration"", ""ETL Processing"", ""Data Validation""",
            ServiceRole.AIEngineer => @"""Model Deployment"", ""Inference Serving"", ""Batch Prediction""",
            ServiceRole.Gateway => @"""Request Routing"", ""Load Balancing"", ""Authentication""",
            ServiceRole.Registry => @"""Service Registration"", ""Service Discovery"", ""Health Tracking""",
            ServiceRole.IPCBroker => @"""Message Routing"", ""Event Distribution"", ""Pub/Sub""",
            ServiceRole.Monitoring => @"""Metrics Collection"", ""Log Aggregation"", ""Tracing""",
            _ => @"""Generic Operation"""
        };
    }
}
