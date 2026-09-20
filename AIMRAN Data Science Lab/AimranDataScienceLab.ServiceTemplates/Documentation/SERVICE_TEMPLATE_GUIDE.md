# Service Templates Guide

## Overview

The AIMRAN Service Templates provide a complete framework for deploying services across multiple platforms and environments. This guide covers how to use, customize, and deploy services using these templates.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Template Structure](#template-structure)
3. [Configuration](#configuration)
4. [Deployment Guides](#deployment-guides)
5. [CLI Usage](#cli-usage)
6. [Examples](#examples)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Generate New Service Templates

Using the CLI tool:
```bash
dotnet run -- generate \
  --name AnalystService \
  --label analyst-service \
  --output ./generated \
  --replicas 3 \
  --kubernetes \
  --docker-local \
  --docker-prod
```

This generates:
- Docker deployment files
- Kubernetes manifests
- Configuration files for all environments
- Windows Service template

### Run Generated Service

**Docker Local:**
```bash
cd generated
docker-compose -f docker-compose.local.yml up
```

**Kubernetes:**
```bash
kubectl apply -f kubernetes-analyst-service.yaml
```

**Windows Service:**
```bash
# Compile first
dotnet build
# Install service
sc create AnalystService binPath="path\to\program.exe"
# Start service
net start AnalystService
```

---

## Template Structure

```
ServiceTemplates/
├── Base/                           # Base classes and interfaces
│   ├── IService.cs                # Service contract
│   ├── ServiceBase.cs             # Abstract service base
│   └── ServiceHost.cs             # Lifecycle management
├── Deployment/
│   ├── Docker/
│   │   ├── Dockerfile.template
│   │   ├── docker-compose.template.yml
│   │   └── docker-compose.production.template.yml
│   ├── Kubernetes/
│   │   ├── service-template.yaml
│   │   ├── statefulset-template.yaml
│   │   ├── namespace-ingress-rbac.yaml
│   │   └── secrets-configmaps.yaml
│   └── WindowsServiceHost.cs
├── Configuration/
│   ├── appsettings.template.json
│   ├── appsettings.Development.template.json
│   ├── appsettings.Staging.template.json
│   ├── appsettings.Production.template.json
│   └── .env.template
└── Tools/
	├── ServiceTemplateGenerator.cs
	└── Cli/Program.cs
```

---

## Configuration

### Environment Variables

Create a `.env` file based on `.env.template`:

```bash
cp .env.template .env
# Edit .env with your values
```

Key variables:
- `ASPNETCORE_ENVIRONMENT`: Development, Staging, Production
- `SERVICE_REGISTRY_URL`: URL of service registry
- `DB_CONNECTION_STRING`: Database connection string
- `CACHE_CONNECTION_STRING`: Cache (Redis) connection string

### appsettings.json

Service configuration hierarchy (highest priority first):

1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

Example `appsettings.Production.json`:
```json
{
  "Service": {
	"Port": 5000,
	"RequestTimeout": "00:00:30"
  },
  "Resilience": {
	"CircuitBreakerThreshold": 5,
	"RetryCount": 2
  }
}
```

---

## Deployment Guides

### Docker - Local Development

1. **Build and Run**
   ```bash
   docker-compose -f docker-compose.local.yml up
   ```

2. **Check Services**
   ```bash
   docker ps
   docker logs <container_id>
   ```

3. **Stop Services**
   ```bash
   docker-compose -f docker-compose.local.yml down
   ```

### Docker - Production

1. **Build Images**
   ```bash
   docker build -t aimran/service-name:latest .
   docker push aimran/service-name:latest
   ```

2. **Deploy with Docker Swarm**
   ```bash
   docker stack deploy -c docker-compose.production.yml aimran
   ```

3. **Monitor Services**
   ```bash
   docker service ls
   docker service ps <service_name>
   ```

### Kubernetes

1. **Create Namespace**
   ```bash
   kubectl apply -f namespace-ingress-rbac.yaml
   ```

2. **Deploy Secrets and ConfigMaps**
   ```bash
   kubectl apply -f secrets-configmaps.yaml
   ```

3. **Deploy Service**
   ```bash
   kubectl apply -f kubernetes-service-name.yaml
   ```

4. **Verify Deployment**
   ```bash
   kubectl get deployments -n aimran
   kubectl get pods -n aimran
   kubectl logs -n aimran <pod_name>
   ```

5. **Scale Service**
   ```bash
   kubectl scale deployment service-name --replicas=5 -n aimran
   ```

### Windows Services

1. **Build Application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Install Service**
   ```bash
   sc create AnalystService binPath="C:\path\to\publish\program.exe"
   sc description AnalystService "AIMRAN Analyst Service"
   ```

3. **Start Service**
   ```bash
   net start AnalystService
   ```

4. **Check Status**
   ```bash
   sc query AnalystService
   ```

5. **Stop Service**
   ```bash
   net stop AnalystService
   ```

6. **Uninstall Service**
   ```bash
   sc delete AnalystService
   ```

---

## CLI Usage

### Generate Command

**Basic:**
```bash
dotnet ServiceTemplateGenerator.dll generate --name MyService --label my-service --output ./gen
```

**Advanced:**
```bash
dotnet ServiceTemplateGenerator.dll generate \
  --name AnalystService \
  --label analyst-service \
  --image docker.io/aimran/analyst:latest \
  --output ./generated \
  --replicas 3 \
  --max-replicas 20 \
  --type stateless \
  --cpu-request 1000m \
  --cpu-limit 4 \
  --memory-request 1Gi \
  --memory-limit 4Gi \
  --kubernetes \
  --docker-local \
  --docker-prod \
  --windows-service
```

### List Templates

```bash
dotnet ServiceTemplateGenerator.dll list
```

Output:
```
Available service templates:
  - ServiceBase: Base class for all services
  - Docker: Docker and docker-compose templates
  - Kubernetes: K8s manifests (Deployment/StatefulSet)
  - Windows Service: Windows Service templates
  - Configuration: Environment-specific configs
```

### Validate Templates

```bash
dotnet ServiceTemplateGenerator.dll validate --path ./generated
```

---

## Examples

### Example 1: Deploy Analyst Service Locally

```bash
# Generate templates
dotnet ServiceTemplateGenerator.dll generate \
  --name AnalystService \
  --label analyst-service \
  --output ./analyst

# Start services
cd analyst
docker-compose -f docker-compose.local.yml up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f analyst-service
```

### Example 2: Deploy to Kubernetes

```bash
# Generate Kubernetes manifests
dotnet ServiceTemplateGenerator.dll generate \
  --name ScientistService \
  --label scientist-service \
  --output ./scientist \
  --kubernetes \
  --type stateless \
  --replicas 3

# Deploy to cluster
kubectl apply -f scientist/kubernetes-scientist-service.yaml
kubectl apply -f scientist/config/

# Verify deployment
kubectl get all -n aimran | grep scientist
```

### Example 3: Deploy as Windows Service

```bash
# Generate Windows Service template
dotnet ServiceTemplateGenerator.dll generate \
  --name AIEngineerService \
  --label ai-engineer-service \
  --output ./ai-engineer \
  --windows-service

# Build service executable
cd ai-engineer
dotnet publish -c Release

# Install as Windows Service
sc create AIEngineerService \
  binPath="C:\ai-engineer\publish\AIEngineerService.exe"

# Start service
net start AIEngineerService
```

---

## Best Practices

### 1. Configuration Management

- **Never commit secrets** to version control
- Use **environment variables** for sensitive data
- Use **Kubernetes Secrets** for container deployments
- Use **.env files** locally (gitignore them)

### 2. Resource Management

- Set **appropriate resource requests/limits**
- Monitor actual usage with Prometheus metrics
- Adjust templates based on real-world data

### 3. Health Checks

- Implement liveness probes for container restart policies
- Implement readiness probes for traffic routing
- Implement startup probes for slow-starting applications

### 4. Logging and Monitoring

- Use structured logging (JSON format recommended)
- Configure log levels per environment:
  - Development: Debug/Information
  - Production: Warning/Error
- Enable metrics collection for observability

### 5. Security

- Run containers as **non-root users**
- Use **read-only root filesystems** when possible
- **Drop ALL capabilities** and add only required ones
- Implement **network policies** for pod-to-pod communication
- Use **RBAC** to limit service account permissions

### 6. Deployment Strategy

- Use **rolling updates** (RollingUpdate strategy)
- Set **maxSurge=1, maxUnavailable=0** for zero-downtime
- Use **pod disruption budgets** to ensure availability
- Implement **graceful shutdown** handling

---

## Troubleshooting

### Service Won't Start

Check logs:
```bash
# Docker
docker logs <container_id>

# Kubernetes
kubectl logs -n aimran <pod_name>

# Windows Service
Event Viewer → Windows Logs → Application
```

### Health Checks Failing

1. Verify endpoint is responding:
   ```bash
   curl http://localhost:5000/health/live
   ```

2. Check firewall rules:
   ```bash
   # Linux
   netstat -tlnp | grep 5000

   # Windows
   netstat -ano | findstr :5000
   ```

3. Review service logs for errors

### High Memory Usage

1. Check GC settings in configuration
2. Monitor with Prometheus: `dotnet_process_working_set_bytes`
3. Increase memory limits if necessary
4. Profile with dotnet-trace

### Kubernetes Pod Stuck in Pending

```bash
# Check events
kubectl describe pod <pod_name> -n aimran

# Common issues
# - Insufficient resources
# - Node selector mismatch
# - PVC binding issues
```

### Service Can't Reach Registry

1. Verify registry is running:
   ```bash
   curl http://service-registry:5000/health
   ```

2. Check network policies:
   ```bash
   kubectl get networkpolicies -n aimran
   ```

3. Verify DNS resolution:
   ```bash
   kubectl exec -it <pod_name> -n aimran -- nslookup service-registry
   ```

---

## Advanced Topics

### Custom Health Checks

Override in your service class:
```csharp
protected override Task<ServiceHealthStatus> OnCheckHealthAsync(
	CancellationToken cancellationToken)
{
	// Custom health check logic
	return Task.FromResult(new ServiceHealthStatus 
	{ 
		IsHealthy = true, 
		Status = "Healthy" 
	});
}
```

### Metrics Collection

The template automatically collects:
- Request count and latency
- Error rates
- Memory and CPU usage
- Custom metrics via `GetCustomMetrics()`

### Distributed Tracing

Enable tracing in configuration:
```json
{
  "Features": {
	"EnableTracing": true
  }
}
```

### Auto-Scaling

Kubernetes HPA automatically scales based on:
- CPU utilization (70%)
- Memory utilization (80%)

Customize in template or via:
```bash
kubectl autoscale deployment my-service --min=2 --max=20 --cpu-percent=80
```

---

## Support

For issues or questions:
1. Check [Troubleshooting](#troubleshooting) section
2. Review logs with suggested commands
3. Consult [Best Practices](#best-practices)
4. Check deployment platform documentation
