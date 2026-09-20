# AIMRAN Service Templates

## Overview

This library provides a complete, production-ready framework for deploying AIMRAN services across multiple platforms:

- **Docker** - Local development and production containerization
- **Kubernetes** - Enterprise-grade orchestration with auto-scaling
- **Windows Services** - Native Windows application deployment

## Features

### 🏗️ Base Infrastructure
- `IService` - Unified service contract
- `ServiceBase` - Abstract base class with lifecycle management
- `ServiceHost` - Multi-platform service hosting
- Health checks and diagnostics

### 🐳 Docker Support
- Multi-stage Dockerfile templates
- Docker Compose for local development
- Production-optimized Docker Compose stack
- Network isolation and health checks

### ☸️ Kubernetes Support
- Deployment manifests with RBAC
- StatefulSet templates for stateful services
- ConfigMaps and Secrets management
- Horizontal Pod Autoscaler (HPA) templates
- Network Policies for security
- Ingress configuration

### 🪟 Windows Services
- Windows Service host base class
- Service installer/uninstaller utilities
- Graceful shutdown handling
- Event log integration

### ⚙️ Configuration
- Environment-specific settings (Dev, Staging, Prod)
- appsettings.json templates
- Environment variables (.env) templates
- Configuration inheritance chain

### 🛠️ Tools
- **ServiceTemplateGenerator** - Programmatic template generation
- **CLI Tool** - Command-line interface for generating templates
- Template validation and verification

## Quick Start

### Using the Templates

1. **Generate templates for a new service:**
```bash
dotnet ServiceTemplateGenerator.dll generate \
  --name AnalystService \
  --label analyst-service \
  --output ./generated \
  --kubernetes \
  --docker-local
```

2. **Deploy locally with Docker:**
```bash
cd generated
docker-compose -f docker-compose.local.yml up
```

3. **Deploy to Kubernetes:**
```bash
kubectl apply -f kubernetes-analyst-service.yaml
```

### Creating a Service

Inherit from `ServiceBase`:

```csharp
using AimranDataScienceLab.ServiceTemplates.Base;
using Microsoft.Extensions.Logging;

public class MyAnalystService : ServiceBase
{
	public MyAnalystService(ILogger<MyAnalystService> logger) 
		: base(logger)
	{
	}

	public override string ServiceId => "analyst-v1";
	public override string ServiceName => "AnalystService";
	public override Version ServiceVersion => new(1, 0, 0);

	protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
	{
		// Initialize your service
	}

	protected override async Task OnStartAsync(CancellationToken cancellationToken)
	{
		// Start your service
	}

	protected override async Task<ServiceHealthStatus> OnCheckHealthAsync(
		CancellationToken cancellationToken)
	{
		// Check health
		return new ServiceHealthStatus 
		{ 
			IsHealthy = true, 
			Status = "Healthy" 
		};
	}
}
```

### Register in DI Container

```csharp
services.AddTemplateService<MyAnalystService>();
services.AddServiceHealthChecks();
```

## Project Structure

```
AimranDataScienceLab.ServiceTemplates/
├── Base/                              # Core service infrastructure
│   ├── IService.cs                   # Service contract
│   ├── ServiceBase.cs                # Abstract base class
│   └── ServiceHost.cs                # Lifecycle manager
├── Deployment/
│   ├── Docker/                       # Docker templates
│   │   ├── Dockerfile.template
│   │   ├── docker-compose.template.yml
│   │   └── docker-compose.production.template.yml
│   ├── Kubernetes/                   # Kubernetes manifests
│   │   ├── service-template.yaml
│   │   ├── statefulset-template.yaml
│   │   ├── namespace-ingress-rbac.yaml
│   │   └── secrets-configmaps.yaml
│   └── WindowsServiceHost.cs        # Windows Service support
├── Configuration/                    # Configuration templates
│   ├── appsettings.template.json
│   ├── appsettings.Development.template.json
│   ├── appsettings.Staging.template.json
│   ├── appsettings.Production.template.json
│   └── .env.template
├── Tools/                           # Code generation tools
│   ├── ServiceTemplateGenerator.cs
│   └── Cli/Program.cs
└── Documentation/                   # Guides and references
	├── SERVICE_TEMPLATE_GUIDE.md
	└── QUICK_REFERENCE.md
```

## Documentation

### Essential Guides
- **[SERVICE_TEMPLATE_GUIDE.md](Documentation/SERVICE_TEMPLATE_GUIDE.md)** - Complete usage guide
- **[QUICK_REFERENCE.md](Documentation/QUICK_REFERENCE.md)** - Quick commands reference

### Key Topics Covered
1. Template generation with CLI
2. Docker local and production deployment
3. Kubernetes deployment and scaling
4. Windows Service installation
5. Configuration management
6. Health checks and monitoring
7. Security best practices
8. Troubleshooting guide

## Deployment Platforms

### Docker
- **For:** Local development, simple deployments, Docker Swarm
- **Benefits:** Easy setup, deterministic environments, fast iteration
- **Templates:** docker-compose.local.yml, docker-compose.production.yml

### Kubernetes
- **For:** Enterprise, multi-cloud, auto-scaling, high availability
- **Benefits:** Orchestration, auto-scaling, self-healing, rolling updates
- **Templates:** Deployment, StatefulSet, HPA, NetworkPolicy, RBAC

### Windows Services
- **For:** Windows-only environments, legacy infrastructure
- **Benefits:** Native Windows integration, Event Log, service management
- **Templates:** WindowsServiceHost, installer utilities

## Configuration Management

### Hierarchy (highest to lowest priority)
1. Environment Variables
2. appsettings.{Environment}.json
3. appsettings.json

### Supported Environments
- Development (local debugging)
- Staging (pre-production validation)
- Production (live deployment)

## Service Health Monitoring

All services provide health endpoints:

```
GET /health/live     -> Liveness (ready to restart if needed)
GET /health/ready    -> Readiness (ready to accept traffic)
GET /health/startup  -> Startup (for slow-starting apps)
GET /diagnostics     -> Full diagnostics and metrics
```

## Built-in Features

- ✅ Lifecycle management (Init → Start → Stop)
- ✅ Health checks and diagnostics
- ✅ Request metrics (count, latency, errors)
- ✅ Memory and CPU monitoring
- ✅ Dependency health tracking
- ✅ Error history
- ✅ Security context support
- ✅ Graceful shutdown
- ✅ Resource limits
- ✅ Auto-scaling support

## Performance Considerations

All templates include:
- Multi-stage Docker builds for minimal image size
- Resource requests and limits
- Health check configurations
- Auto-scaling thresholds
- Connection pooling settings
- Cache configuration
- Timeout policies

## Security

Templates implement:
- Non-root container execution
- Read-only root filesystems
- Capability dropping
- Network policies
- RBAC configuration
- Secret management
- TLS/SSL support
- Audit logging

## Roadmap

- [ ] Helm chart generation
- [ ] Service mesh integration (Istio)
- [ ] Advanced metrics (Prometheus scraping)
- [ ] Distributed tracing (Jaeger)
- [ ] Multi-region deployment
- [ ] GitOps workflows (ArgoCD)

## Contributing

To add new templates:

1. Create template files in appropriate directory
2. Add variables using `{VAR_NAME}` format
3. Update `ServiceTemplateGenerator.cs`
4. Add documentation
5. Test with sample service

## License

Part of AIMRAN Data Science Lab ecosystem.

## Support

For issues or questions, refer to:
1. [Documentation](Documentation/SERVICE_TEMPLATE_GUIDE.md)
2. [Examples](Documentation/QUICK_REFERENCE.md)
3. [Troubleshooting Guide](Documentation/SERVICE_TEMPLATE_GUIDE.md#troubleshooting)

---

**Generated:** Service Templates v1.0.0
**Platform Support:** Docker, Kubernetes, Windows Services
**Target Framework:** .NET 10 LTS
