# Service Migration and Upgrade Guide

## Overview

This guide covers migrating existing services to the AIMRAN template system and upgrading services to new versions.

## Table of Contents

- [Pre-Migration Assessment](#pre-migration-assessment)
- [Migration Planning](#migration-planning)
- [Service Migration Steps](#service-migration-steps)
- [Data Migration](#data-migration)
- [Version Upgrade Steps](#version-upgrade-steps)
- [Rollback Procedures](#rollback-procedures)
- [Post-Migration Validation](#post-migration-validation)

---

## Pre-Migration Assessment

### Current State Analysis

- [ ] Document current service architecture
- [ ] List all service dependencies
- [ ] Identify current deployment platforms (Docker, Kubernetes, Windows, etc.)
- [ ] Document current resource allocations
- [ ] Review current configuration management
- [ ] List all integrations
- [ ] Document current monitoring and logging
- [ ] Identify custom implementations
- [ ] Document known issues and workarounds
- [ ] Get team input and concerns

### Compatibility Check

- [ ] Verify .NET version compatibility (target: .NET 10)
- [ ] Check dependency versions
- [ ] Validate third-party integrations
- [ ] Confirm database compatibility
- [ ] Review API contracts
- [ ] Check authentication/authorization mechanisms
- [ ] Validate message formats
- [ ] Review security requirements

### Timeline Planning

- [ ] Assess migration complexity (Low/Medium/High)
- [ ] Estimate duration per service
- [ ] Plan total timeline with buffer
- [ ] Identify blockers and dependencies
- [ ] Schedule migration window
- [ ] Plan communication to stakeholders
- [ ] Prepare rollback plan

---

## Migration Planning

### Service Mapping

Document how existing services map to AIMRAN roles:

```
Current Service → AIMRAN Role → Base Class
─────────────────────────────────────────────
QueryService → Analyst → AnalystServiceBase
ModelService → Scientist → ScientistServiceBase
PipelineService → Engineer → EngineerServiceBase
PredictionService → AI Engineer → AIEngineerServiceBase
APIService → Gateway → GatewayServiceBase
DiscoveryService → Registry → RegistryServiceBase
EventService → IPC Broker → IPCBrokerServiceBase
TelemetryService → Monitoring → MonitoringServiceBase
```

### Dependency Management

Create a dependency order:

```
1. Infrastructure Services (no dependencies)
   - Service Registry
   - IPC Broker
   - Monitoring Service

2. Adapter Services (depend on infrastructure)
   - Service Gateway (depends on Registry)

3. Role Services (depend on infrastructure + adapters)
   - Analyst Service
   - Scientist Service
   - Engineer Service
   - AI Engineer Service
```

### Risk Assessment

- [ ] High risk dependencies identified
- [ ] Fallback options planned
- [ ] Testing strategy defined
- [ ] Rollback strategy ready
- [ ] Communication plan established

---

## Service Migration Steps

### Phase 1: Template Generation

```bash
# 1. Identify service role
# Example: MyAnalysisService → Analyst Role

# 2. Determine current resources
# CPU: 1000m → 1 (Kubernetes notation)
# Memory: 2Gi → 2Gi
# Replicas: 3

# 3. Generate templates
dotnet ServiceTemplateGenerator.dll generate \
  --name MyAnalysisService \
  --label my-analysis \
  --role analyst \
  --output ./migration/my-analysis-service \
  --replicas 3 \
  --cpu-request 1000m \
  --cpu-limit 2 \
  --memory-request 2Gi \
  --memory-limit 4Gi \
  --kubernetes \
  --docker-local \
  --docker-prod

# 4. Review generated templates
# - Verify resource allocations match requirements
# - Review Dockerfile for optimizations
# - Check Kubernetes manifests for best practices
# - Validate configuration templates
```

### Phase 2: Code Migration

**Step 1: Inherit from Role Base Class**

```csharp
// Before
public class MyAnalysisService : IService
{
	// Manual implementation of IService
}

// After
using AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates;

public class MyAnalysisService : AnalystServiceBase
{
	public MyAnalysisService(ILogger<MyAnalysisService> logger)
		: base(logger)
	{
	}

	// Implement role-specific methods
	protected override Task<string> ExecuteQueryAsync(...)
	protected override Task<Dictionary<string, object?>> GetDashboardDataAsync(...)
	protected override Task<byte[]> GenerateReportAsync(...)
}
```

**Step 2: Implement Role-Specific Methods**

```csharp
public class MyAnalysisService : AnalystServiceBase
{
	private readonly IDatabaseService _database;

	public MyAnalysisService(
		ILogger<MyAnalysisService> logger,
		IDatabaseService database)
		: base(logger)
	{
		_database = database;
	}

	protected override async Task<string> ExecuteQueryAsync(
		string query, 
		CancellationToken cancellationToken = default)
	{
		// Migrate implementation from old service
		var results = await _database.ExecuteAsync(query);
		return JsonSerializer.Serialize(results);
	}

	// Continue for other methods...
}
```

**Step 3: Update Configuration**

```csharp
// Override configuration if needed
protected override ServiceConfiguration GetServiceConfiguration() =>
	new()
	{
		MaxConcurrentRequests = 100,
		RequestTimeout = TimeSpan.FromSeconds(60),
		HealthCheckInterval = TimeSpan.FromSeconds(30),
		Features = new()
		{
			{ "MyCustomFeature", "true" },
			{ "CachingEnabled", "true" }
		}
	};
```

### Phase 3: Integration Testing

```bash
# 1. Run unit tests
dotnet test

# 2. Set up local environment
docker-compose -f generated/docker-compose.local.yml up

# 3. Test connectivity
curl http://localhost:5010/health

# 4. Test service methods
# Create integration tests

# 5. Verify logging
docker-compose logs my-service

# 6. Test error handling
# Simulate failures and verify behavior

# 7. Performance testing
# Compare with previous version
```

### Phase 4: Docker Migration

**For Local Development:**
```bash
# 1. Build your image
cd migration/my-analysis-service
docker build -t my-analysis:v1.0 .

# 2. Test locally
docker-compose -f docker-compose.local.yml up

# 3. Verify service
curl http://localhost:5010/health
```

**For Production:**
```bash
# 1. Build production image
docker build -t registry.example.com/my-analysis:v1.0.0 \
  -f Dockerfile .

# 2. Test image
docker run -p 5000:5000 registry.example.com/my-analysis:v1.0.0

# 3. Push to registry
docker push registry.example.com/my-analysis:v1.0.0

# 4. Deploy
docker stack deploy -c docker-compose.production.yml aimran
```

### Phase 5: Kubernetes Migration

**For Development Cluster:**
```bash
# 1. Create namespace (if new cluster)
kubectl create namespace aimran

# 2. Apply infrastructure services first
kubectl apply -f generated/namespace-ingress-rbac.yaml
kubectl apply -f generated/secrets-configmaps.yaml

# 3. Apply your service
kubectl apply -f generated/kubernetes-my-analysis.yaml

# 4. Verify deployment
kubectl get deployments -n aimran
kubectl get pods -n aimran
kubectl logs deployment/my-analysis-service -n aimran

# 5. Port-forward and test
kubectl port-forward svc/my-analysis-service 5000:5000 -n aimran
curl http://localhost:5000/health
```

**For Production Cluster:**
```bash
# 1. Prepare staging environment
kubectl create namespace aimran-staging

# 2. Deploy to staging
kubectl -n aimran-staging apply -f generated/kubernetes-my-analysis.yaml

# 3. Test in staging
kubectl -n aimran-staging rollout status deployment/my-analysis-service

# 4. Run integration tests

# 5. Deploy to production
kubectl -n aimran apply -f generated/kubernetes-my-analysis.yaml

# 6. Monitor deployment
kubectl -n aimran rollout status deployment/my-analysis-service
kubectl -n aimran get pods -w
```

---

## Data Migration

### Database Schema Migration

```bash
# 1. Backup current database
pg_dump -U aimran -d aimran_data > backup_$(date +%Y%m%d).sql

# 2. Create migration script
# migration_001.sql - Recreate tables/schemas for new service

# 3. Test migration locally
psql -U aimran -d aimran_data -f migration_001.sql

# 4. Validate data integrity
SELECT COUNT(*) FROM table_name;

# 5. Run on production (during maintenance window)
psql -U aimran -d aimran_data -f migration_001.sql
```

### Configuration Migration

```bash
# 1. Export current configuration
kubectl get configmap -n old-namespace -o yaml > config-backup.yaml

# 2. Transform configuration for new format
# Create appsettings.json from old config

# 3. Create new ConfigMap
kubectl create configmap my-service-config \
  --from-file=appsettings.json \
  -n aimran

# 4. Verify configuration
kubectl get configmap my-service-config -n aimran -o yaml
```

### State Transfer (if needed)

```bash
# 1. Drain old service of connections
kubectl scale deployment old-service --replicas=0 -n old-namespace

# 2. Export data
kubectl exec -it old-pod -n old-namespace -- \
  pg_dump -U user -d database > data.sql

# 3. Import to new service's database
kubectl exec -i new-pod -n aimran -- \
  psql -U user -d database < data.sql

# 4. Verify data integrity
```

---

## Version Upgrade Steps

### Pre-Upgrade Checklist

- [ ] Review release notes for breaking changes
- [ ] Backup current state
  - [ ] Kubernetes: `kubectl get all -n aimran -o yaml > backup.yaml`
  - [ ] Docker: `docker inspect container > backup.json`
  - [ ] Database: Full backup
- [ ] Verify staging environment
- [ ] Prepare rollback strategy
- [ ] Notify stakeholders
- [ ] Schedule maintenance window (if needed)

### Docker Upgrade

**Rolling Update:**
```bash
# 1. Build new version
docker build -t registry.example.com/my-service:v1.1.0 .

# 2. Push to registry
docker push registry.example.com/my-service:v1.1.0

# 3. Update service image
docker service update \
  --image registry.example.com/my-service:v1.1.0 \
  aimran_my-service

# 4. Monitor update
docker service ps aimran_my-service

# 5. Verify new version
curl http://localhost:5010/health
```

### Kubernetes Upgrade

**Rolling Update (automatic):**
```bash
# 1. Update image in deployment
kubectl set image deployment/my-service \
  my-service=registry.example.com/my-service:v1.1.0 \
  -n aimran

# 2. Check rollout status
kubectl rollout status deployment/my-service -n aimran

# 3. Monitor pods
kubectl get pods -n aimran -l app=my-service -w
```

**Canary Update (gradual):**
```bash
# 1. Create canary deployment with new version
kubectl apply -f canary-deployment.yaml -n aimran

# 2. Gradually increase canary replicas
kubectl scale deployment my-service-canary --replicas=1 -n aimran

# 3. Monitor metrics
kubectl get pods -n aimran
kubectl logs deployment/my-service-canary -n aimran

# 4. If good, scale up canary and down old
kubectl scale deployment my-service-canary --replicas=3 -n aimran
kubectl scale deployment my-service --replicas=0 -n aimran

# 5. Remove old deployment
kubectl delete deployment my-service -n aimran
kubectl rename deployment my-service-canary to my-service -n aimran
```

---

## Rollback Procedures

### Docker Rollback

```bash
# 1. Identify previous version
docker service ls
docker service ps aimran_my-service

# 2. Rollback to previous version
docker service update \
  --image registry.example.com/my-service:v1.0.0 \
  aimran_my-service

# 3. Monitor rollback
docker service ps aimran_my-service
```

### Kubernetes Rollback

```bash
# 1. View rollout history
kubectl rollout history deployment/my-service -n aimran

# 2. Check specific revision
kubectl rollout history deployment/my-service --revision=2 -n aimran

# 3. Rollback to previous version
kubectl rollout undo deployment/my-service -n aimran

# 4. Rollback to specific revision
kubectl rollout undo deployment/my-service --to-revision=2 -n aimran

# 5. Monitor rollback
kubectl rollout status deployment/my-service -n aimran
```

### Database Rollback

```bash
# If needed, restore from backup
psql -U aimran -d aimran_data < backup_database.sql

# Or point to previous data snapshot
```

---

## Post-Migration Validation

### Functional Testing

- [ ] Service starts without errors
- [ ] Health checks pass
- [ ] Service responds to API calls
- [ ] All endpoints working
- [ ] Data integrity verified
- [ ] Dependencies accessible
- [ ] Authentication/authorization working
- [ ] Configuration loaded correctly

### Performance Testing

- [ ] Response times within SLA
- [ ] Throughput meets requirements
- [ ] Resource usage acceptable
- [ ] Memory leaks none detected
- [ ] Database queries optimized
- [ ] No connection pool exhaustion

### Integration Testing

- [ ] Service-to-service communication works
- [ ] External API integrations functional
- [ ] Message routing working
- [ ] Event distribution functioning
- [ ] Logging aggregation capturing data
- [ ] Monitoring collecting metrics

### Security Validation

- [ ] Authentication working correctly
- [ ] Authorization enforced
- [ ] Secrets properly managed
- [ ] Network policies applied
- [ ] TLS/SSL enabled
- [ ] No unauthorized access observed

### Documentation

- [ ] Architecture documentation updated
- [ ] Configuration documented
- [ ] Operational procedures documented
- [ ] Troubleshooting guide updated
- [ ] Team trained on new service
- [ ] Runbooks updated

---

## Sign-Off

- [ ] Technical validation complete
- [ ] Performance acceptable
- [ ] Security requirements met
- [ ] Documentation complete
- [ ] Team trained
- [ ] Go-live approval received

**Migration Date:** _______________
**Migrated By:** _______________
**Approved By:** _______________
**Notes:** _______________

---

## Troubleshooting Common Issues

### Service won't start after upgrade

1. Check version compatibility
   ```bash
   dotnet --version
   kubectl describe pod pod-name -n aimran
   ```

2. Review logs for errors
   ```bash
   kubectl logs pod-name -n aimran
   docker logs container-name
   ```

3. Check configuration
   ```bash
   kubectl get configmap -n aimran
   echo $SERVICE_ENV_VAR
   ```

### Data migration failed

1. Check backup integrity
   ```bash
   pg_restore --list backup.sql | head -20
   ```

2. Verify target schema
   ```sql
   \dt  -- in psql
   ```

3. Run migration in dry-run mode
   ```bash
   pg_restore --dry-run --single-transaction backup.sql
   ```

### Performance degradation after migration

1. Check resource allocation
   ```bash
   kubectl top pods -n aimran
   ```

2. Review query performance
   ```sql
   EXPLAIN ANALYZE SELECT ...;
   ```

3. Check for new bottlenecks
   - Database locks
   - Network latency
   - Cache misses
   - Connection pool exhaustion

### Connectivity issues

1. Verify DNS resolution
   ```bash
   kubectl exec pod-name -n aimran -- nslookup service-name
   ```

2. Check service discovery
   ```bash
   kubectl get svc -n aimran
   kubectl get endpoints -n aimran
   ```

3. Test connectivity
   ```bash
   kubectl exec pod-name -n aimran -- curl http://other-service:5000/health
   ```
