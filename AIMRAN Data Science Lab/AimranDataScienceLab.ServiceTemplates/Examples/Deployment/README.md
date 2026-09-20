# AIMRAN Service Templates - Examples and Deployment Patterns

This directory contains comprehensive examples and patterns for deploying AIMRAN services across different platforms.

## Directory Structure

```
Examples/
├── Services/                    # Role-specific service implementations
│   ├── AnalystServiceImpl.cs    # Example Analyst service
│   ├── ScientistServiceImpl.cs  # Example Scientist service
│   └── ...
├── Deployment/                  # Full-stack deployment examples
│   ├── docker-compose-full-stack.yml
│   ├── kubernetes-full-stack.yaml
│   └── README.md
└── Configuration/               # Configuration examples
```

## Quick Start

### Local Development with Docker Compose

```bash
# Start the full stack
docker-compose -f Examples/Deployment/docker-compose-full-stack.yml up -d

# Wait for services to start
sleep 10

# Verify services
docker-compose -f Examples/Deployment/docker-compose-full-stack.yml ps

# Access services via gateway
curl http://localhost:8080/health

# View logs
docker-compose -f Examples/Deployment/docker-compose-full-stack.yml logs -f

# Shutdown
docker-compose -f Examples/Deployment/docker-compose-full-stack.yml down
```

### Kubernetes Deployment

```bash
# Apply the full-stack manifest
kubectl apply -f Examples/Deployment/kubernetes-full-stack.yaml

# Wait for pods to be ready
kubectl -n aimran wait --for=condition=ready pod -l tier=infrastructure --timeout=300s

# Verify deployment
kubectl -n aimran get all

# Port-forward to gateway
kubectl -n aimran port-forward svc/service-gateway 8080:5000

# Access services
curl http://localhost:8080/health

# View logs
kubectl -n aimran logs deployment/analyst-service -f

# Cleanup
kubectl delete namespace aimran
```

## Full Stack Architecture

### Infrastructure Services (Required)

1. **Service Registry** (`service-registry`)
   - Handles service discovery and registration
   - Replicas: 3 (high availability)
   - Port: 5000 (internal), 5001 (Docker)

2. **IPC Broker** (`ipc-broker`)
   - Routes messages between services
   - Stateful with persistent storage
   - Port: 5000 (internal), 5002 (Docker)

3. **Service Gateway** (`service-gateway`)
   - Routes incoming requests
   - Load balancing and authentication
   - Replicas: 3 (high availability)
   - Port: 5000 (internal), 8080 (Docker)

4. **Monitoring Service** (`monitoring-service`)
   - Collects metrics and logs
   - Provides observability
   - Port: 5000 (gRPC), 9090 (Prometheus metrics)

### Role-Specific Services

1. **Analyst Service** (`analyst-service`)
   - SQL query execution
   - Dashboard management
   - Report generation
   - Replicas: 2
   - Port: 5010 (Docker)

2. **Scientist Service** (`scientist-service`)
   - Model development and training
   - Experiment tracking
   - Hyperparameter optimization
   - Replicas: 1 (can be increased)
   - Port: 5011 (Docker)

3. **Engineer Service** (`engineer-service`)
   - Data pipeline orchestration
   - ETL processing
   - Data validation
   - Replicas: 2
   - Port: 5012 (Docker)

4. **AI Engineer Service** (`ai-engineer-service`)
   - Model deployment and serving
   - Inference pipelines
   - A/B testing
   - Replicas: 3
   - Port: 5013 (Docker)

### Supporting Services

- **PostgreSQL** - Primary data store
- **Model Registry** - Stores and versions trained models
- **Portainer** (optional) - Container management UI

## Service Connectivity Map

```
┌─────────────────────────────────────────────────────┐
│           External Client / User                     │
└──────────────┬──────────────────────────────────────┘
			   │
		┌──────▼──────┐
		│Service       │ (Load Balancer / Gateway)
		│Gateway       │ Handles routing & auth
		│(Port 8080)   │
		└──────┬───────┘
			   │
	┌──────────┼──────────┐
	│          │          │
┌───▼──┐  ┌────▼─┐   ┌────▼──┐
│      │  │      │   │       │
│Analyst  │Sci   │   │Eng    │
│Service  │entist│   │ineer  │
│ (5010)  │(5011)│   │(5012) │
│      │  │      │   │       │
└──┬─┬─┘  └──┬───┘   └───┬──┘
   │ │       │           │
   │ │   ┌───▼────────────┘
   │ │   │
   │ │   │   ┌─────────────┐
   │ │   └──►│AI Engineer  │
   │ │       │Service(5013)│
   │ │       └─────────────┘
   │ │
   │ └──────────┐
   │            │
   │    ┌───────▼────────┐
   │    │Service Registry│(Discovery)
   │    │    (5001)      │
   │    └────────────────┘
   │
   └─────────┬────────┐
			 │        │
		┌────▼──┐   ┌─▼──────┐
		│IPC    │   │Monitor │
		│Broker │   │Service │
		│(5002) │   │(5003)  │
		└───────┘   └────────┘
```

## Environment Configuration

### Docker Environment Variables

Services use these environment variables (editable in docker-compose-full-stack.yml):

- `ASPNETCORE_ENVIRONMENT`: Production/Development/Staging
- Service-specific URLs:
  - `Registry__Url`: Service registry endpoint
  - `IPCBroker__Url`: Message broker endpoint
  - `Database__ConnectionString`: PostgreSQL connection
- Feature flags:
  - `ANALYST_MODE=true`: Enable analyst-specific features
  - `QUERY_CACHE_ENABLED=true`: Enable caching
  - `EXPERIMENT_TRACKING_ENABLED=true`: Track experiments
  - `PIPELINE_ORCHESTRATION_ENABLED=true`: Enable pipelines
  - `AB_TESTING_ENABLED=true`: Enable A/B testing

### Kubernetes Secrets and ConfigMaps

The Kubernetes manifest expects these secrets:

```bash
# Create PostgreSQL secret
kubectl create secret generic postgres-secret \
  --from-literal=password=your-secure-password \
  --from-literal=connectionString='Host=postgres;Username=aimran;Password=your-secure-password;Database=aimran_data' \
  -n aimran
```

## Health Check Endpoints

All services expose health check endpoints:

```bash
# Check service health
curl http://localhost:5010/health  # Analyst
curl http://localhost:5011/health  # Scientist
curl http://localhost:5012/health  # Engineer
curl http://localhost:5013/health  # AI Engineer
curl http://localhost:8080/health  # Gateway
```

Expected response:
```json
{
  "isHealthy": true,
  "status": "Healthy",
  "message": "Service is operational",
  "lastChecked": "2024-01-15T10:30:00Z"
}
```

## Scaling Services

### Docker Swarm Scaling

```bash
# Scale analyst service to 5 replicas
docker service scale aimran_analyst-service=5

# Check scaling
docker service ps aimran_analyst-service
```

### Kubernetes Scaling

```bash
# Scale analyst service
kubectl scale deployment analyst-service --replicas=5 -n aimran

# Check scaling
kubectl get pods -n aimran -l app=analyst-service

# Use HPA for automatic scaling (included in templates)
kubectl get hpa -n aimran
```

## Monitoring and Observability

### Access Monitoring Dashboard

```bash
# Port-forward to Prometheus
kubectl port-forward svc/monitoring-service 9090:9090 -n aimran

# Access at http://localhost:9090
```

### View Service Metrics

```bash
# Get service pod status
kubectl get pods -n aimran -o wide

# Check resource usage
kubectl top pods -n aimran

# View service logs
kubectl logs deployment/analyst-service -n aimran
kubectl logs deployment/scientist-service -n aimran
```

### Generate Load for Testing

```bash
# Create a load generation service
kubectl run -it --rm=true --image=busybox --restart=Never \
  -n aimran -- sh -c 'while true; do wget -q -O- http://service-gateway:5000/health; done'
```

## Troubleshooting

### Service Won't Start

1. Check logs:
   ```bash
   # Docker
   docker-compose logs service-name

   # Kubernetes
   kubectl logs deployment/service-name -n aimran
   ```

2. Verify dependencies are running:
   ```bash
   # Docker
   docker-compose ps

   # Kubernetes
   kubectl get all -n aimran
   ```

3. Check resource availability:
   ```bash
   kubectl describe node
   kubectl describe pod pod-name -n aimran
   ```

### Service Not Responding

1. Verify service is running:
   ```bash
   kubectl get svc service-name -n aimran
   ```

2. Check endpoint availability:
   ```bash
   kubectl get endpoints service-name -n aimran
   ```

3. Port-forward and test locally:
   ```bash
   kubectl port-forward svc/service-name 5000:5000 -n aimran
   curl http://localhost:5000/health
   ```

### Connectivity Issues

1. Check network policies:
   ```bash
   kubectl get networkpolicies -n aimran
   kubectl describe networkpolicy policy-name -n aimran
   ```

2. Test connectivity:
   ```bash
   kubectl exec -it pod-name -n aimran -- curl http://other-service:5000
   ```

3. Check DNS resolution:
   ```bash
   kubectl exec -it pod-name -n aimran -- nslookup service-name
   ```

## Performance Tuning

### Resource Limits

Adjust resource requests/limits in deployment manifests based on your workload:

```yaml
resources:
  requests:
	cpu: 500m
	memory: 512Mi
  limits:
	cpu: 2
	memory: 2Gi
```

### Replica Counts

Increase replicas for high-traffic services:

```bash
# Scale up specific services
kubectl scale deployment analyst-service --replicas=10 -n aimran
kubectl scale deployment ai-engineer-service --replicas=20 -n aimran
```

### Database Optimization

- Ensure PostgreSQL resources are adequate
- Create indexes on frequently queried columns
- Monitor connection pool settings
- Regular vacuuming and maintenance

## Cleanup

### Docker Cleanup

```bash
# Stop all containers
docker-compose -f Examples/Deployment/docker-compose-full-stack.yml down -v

# Remove all Docker data
docker system prune -a --volumes
```

### Kubernetes Cleanup

```bash
# Delete everything in namespace
kubectl delete namespace aimran

# Verify deletion
kubectl get namespace aimran
```

## Next Steps

1. **Customize services** - Implement role-specific services for your use case
2. **Add persistence** - Configure persistent volumes for stateful services
3. **Setup monitoring** - Configure Prometheus and Grafana for metrics
4. **Implement security** - Add network policies, RBAC, and secrets management
5. **Plan disaster recovery** - Setup backups and failover procedures
6. **Load testing** - Test performance under expected load
7. **Documentation** - Document your specific deployment

## Support and Documentation

- See [ROLE_SPECIFIC_GUIDE.md](../Documentation/ROLE_SPECIFIC_GUIDE.md) for role-specific implementation details
- See [DEPLOYMENT_CHECKLIST.md](../Documentation/DEPLOYMENT_CHECKLIST.md) for deployment procedures
- See [QUICK_REFERENCE.md](../Documentation/QUICK_REFERENCE.md) for quick commands
