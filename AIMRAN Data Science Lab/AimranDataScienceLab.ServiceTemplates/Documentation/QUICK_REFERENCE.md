# Service Templates - Quick Reference

## Template Generation

```bash
# Basic generation
dotnet ServiceTemplateGenerator.dll generate \
  --name ServiceName \
  --label service-label \
  --output ./generated

# Full generation with all options
dotnet ServiceTemplateGenerator.dll generate \
  --name AnalystService \
  --label analyst-service \
  --image docker.io/aimran/analyst \
  --output ./generated \
  --replicas 3 \
  --max-replicas 10 \
  --type stateless \
  --cpu-request 500m \
  --cpu-limit 2 \
  --memory-request 512Mi \
  --memory-limit 2Gi \
  --kubernetes \
  --docker-local \
  --docker-prod \
  --windows-service
```

## Deployment Checklists

### Docker Local Development
- [ ] Generate templates: `generate --name ... --label ... --docker-local`
- [ ] Create `.env` file from template
- [ ] Build images: `docker-compose build`
- [ ] Start services: `docker-compose up`
- [ ] Verify services running: `docker ps`
- [ ] Check logs: `docker-compose logs -f`

### Docker Production
- [ ] Generate templates: `generate ... --docker-prod`
- [ ] Update credentials in secrets
- [ ] Build production images
- [ ] Push to registry
- [ ] Deploy stack: `docker stack deploy -c docker-compose.production.yml aimran`
- [ ] Monitor: `docker service ls`

### Kubernetes
- [ ] Setup Kubernetes cluster
- [ ] Generate manifests: `generate ... --kubernetes`
- [ ] Create namespace: `kubectl apply -f namespace-ingress-rbac.yaml`
- [ ] Deploy secrets: `kubectl apply -f secrets-configmaps.yaml`
- [ ] Deploy services: `kubectl apply -f kubernetes-*.yaml`
- [ ] Verify: `kubectl get all -n aimran`

### Windows Service
- [ ] Generate templates: `generate ... --windows-service`
- [ ] Compile: `dotnet build -c Release`
- [ ] Install: `sc create ServiceName binPath="path\to\exe"`
- [ ] Start: `net start ServiceName`
- [ ] Monitor: `sc query ServiceName`

## Common Commands

### Docker
```bash
# Local development
docker-compose -f docker-compose.local.yml up
docker-compose -f docker-compose.local.yml down
docker-compose logs -f service-name

# Production
docker build -t aimran/service:latest .
docker push aimran/service:latest
docker stack deploy -c docker-compose.production.yml aimran
```

### Kubernetes
```bash
# Deployment
kubectl apply -f kubernetes-service.yaml
kubectl get deployments -n aimran
kubectl describe deployment service-name -n aimran
kubectl logs -n aimran pod-name

# Scaling
kubectl scale deployment service-name --replicas=5 -n aimran

# Updates
kubectl set image deployment/service-name service-name=aimran/service:v2 -n aimran
kubectl rollout status deployment/service-name -n aimran
kubectl rollout undo deployment/service-name -n aimran

# Monitoring
kubectl get all -n aimran
kubectl top pods -n aimran
kubectl top nodes
```

### Windows Service
```bash
# Management
sc create ServiceName binPath="path\to\exe"
sc query ServiceName
sc start ServiceName
sc stop ServiceName
sc delete ServiceName

# Troubleshooting
Get-EventLog -LogName Application -Source "ServiceName" | tail -20
Get-Service | Where-Object {$_.Name -like "*ServiceName*"}
```

## Configuration Override Hierarchy

1. **Environment Variables** (highest priority)
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   export SERVICE__PORT=5000
   ```

2. **appsettings.{Environment}.json**
   ```json
   appsettings.Production.json
   ```

3. **appsettings.json** (lowest priority)
   ```json
   appsettings.json
   ```

## Health Endpoints

All services expose:

```bash
# Liveness check (restart if unhealthy)
GET /health/live

# Readiness check (remove from LB if not ready)
GET /health/ready

# Startup check (for slow-starting apps)
GET /health/startup

# Full diagnostics
GET /diagnostics
```

## Generated File Locations

After running `generate --output ./generated`:

```
generated/
├── Dockerfile                          # Container image definition
├── docker-compose.local.yml            # Local development stack
├── docker-compose.production.yml       # Production stack
├── kubernetes-service-label.yaml       # K8s manifests
├── WindowsService.cs                   # Windows Service implementation
├── config/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
└── README.md                           # Service-specific README
```

## Template Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| SERVICE_NAME | Required | Service name (e.g., AnalystService) |
| SERVICE_LABEL | Required | Deployment label (e.g., analyst-service) |
| REPLICAS_COUNT | 3 | Initial number of service replicas |
| MIN_REPLICAS | 1 | Minimum replicas for autoscaling |
| MAX_REPLICAS | 10 | Maximum replicas for autoscaling |
| CPU_REQUEST | 500m | Minimum CPU allocation |
| CPU_LIMIT | 2 | Maximum CPU allocation |
| MEMORY_REQUEST | 512Mi | Minimum memory allocation |
| MEMORY_LIMIT | 2Gi | Maximum memory allocation |
| STORAGE_SIZE | 10Gi | PVC size for stateful services |

## Troubleshooting Quick Links

| Issue | Solution |
|-------|----------|
| Service fails to start | Check logs: `docker logs` / `kubectl logs` |
| Health checks failing | Verify endpoint: `curl http://localhost:5000/health/live` |
| Can't reach registry | Check: `nslookup service-registry` |
| High memory usage | Review GC settings, increase limits |
| Pod pending in K8s | Check node resources: `kubectl describe node` |
| Windows Service won't start | Check Event Viewer Application log |

## Environment Variable Naming Convention

Docker Compose and Kubernetes resolve environment variables by replacing:
- `:` with `__` (double underscore)
- `.` with `__` (double underscore)

Examples:
```
Service:Port → SERVICE__PORT
Service:RegistryUrl → SERVICE__REGISTRYURL
Database:ConnectionString → DATABASE__CONNECTIONSTRING
Features:EnableCaching → FEATURES__ENABLECACHING
```

## Security Checklist

- [ ] Changed default passwords/secrets
- [ ] Used RBAC for Kubernetes deployments
- [ ] Created network policies
- [ ] Enabled TLS for external communication
- [ ] Configured log redaction for sensitive data
- [ ] Set resource limits
- [ ] Used non-root containers
- [ ] Scanned images for vulnerabilities

## Performance Tuning

### Memory
- Set requests equal to typical usage (80th percentile)
- Set limits to max acceptable (with buffer)
- Monitor GC behavior

### CPU
- Request = expected peak usage
- Limit = max allowed (prevents throttling other pods)

### Networking
- Reduce request timeout if possible
- Enable compression (Dockerfile)
- Use connection pooling

### Caching
- Enable Redis caching in production
- Set appropriate TTLs
- Monitor hit rates
