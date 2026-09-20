# AIMRAN Data Science Lab - Deployment Guide

## Prerequisites

- **Kubernetes** 1.24+ cluster
- **kubectl** CLI configured
- **Docker** 20+ registry access
- **PostgreSQL** 14+ database
- **Redis** 6+ cache
- **Helm** 3+

## Quick Start

### Local Development with Docker Compose

```bash
cd docker
docker-compose up -d
```

Services available:
- API: `http://localhost:5000`
- UI: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3001`

### Prerequisites for Local Dev
```bash
# Install dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Start services
docker-compose -f docker-compose.dev.yml up
```

## Kubernetes Deployment

### 1. Prepare Cluster

```bash
# Create namespace
kubectl create namespace aimran-dsl

# Create secrets for credentials
kubectl create secret generic db-credentials \
  --from-literal=username=aimran \
  --from-literal=password=secure_password \
  -n aimran-dsl

# Create configmap for settings
kubectl create configmap app-config \
  --from-file=config/app.json \
  -n aimran-dsl
```

### 2. Deploy Database

```bash
# Apply PostgreSQL deployment
kubectl apply -f k8s/postgres-deployment.yaml -n aimran-dsl

# Wait for database to be ready
kubectl wait --for=condition=Ready pod \
  -l app=postgres -n aimran-dsl \
  --timeout=300s

# Run database migrations
kubectl run -it --rm db-migrate \
  --image=aimran-dsl:migrations \
  --restart=Never \
  -n aimran-dsl
```

### 3. Deploy Redis Cache

```bash
# Deploy Redis cluster
kubectl apply -f k8s/redis-deployment.yaml -n aimran-dsl

# Verify Redis is ready
kubectl get pods -l app=redis -n aimran-dsl
```

### 4. Deploy Application Services

```bash
# Build and push Docker images
docker build -t aimran-dsl:api ./AimranDataScienceLab.Web
docker build -t aimran-dsl:ml-engine ./AimranDataScienceLab.Engine
docker build -t aimran-dsl:gateway ./AimranDataScienceLab.Gateway

docker push aimran-dsl:api
docker push aimran-dsl:ml-engine
docker push aimran-dsl:gateway

# Deploy services using Helm
helm install aimran-dsl ./helm/aimran-dsl \
  -n aimran-dsl \
  --values helm/values-prod.yaml
```

### 5. Deploy Monitoring Stack

```bash
# Deploy Prometheus
kubectl apply -f k8s/prometheus-deployment.yaml -n aimran-dsl

# Deploy Grafana
kubectl apply -f k8s/grafana-deployment.yaml -n aimran-dsl

# Deploy ELK Stack
kubectl apply -f k8s/elasticsearch-deployment.yaml -n aimran-dsl
kubectl apply -f k8s/kibana-deployment.yaml -n aimran-dsl
```

### 6. Configure Ingress

```bash
# Apply ingress configuration
kubectl apply -f k8s/ingress.yaml -n aimran-dsl

# Get ingress URL
kubectl get ingress -n aimran-dsl
```

## Helm Deployment

### Chart Structure
```
helm/aimran-dsl/
├── Chart.yaml
├── values.yaml
├── values-dev.yaml
├── values-prod.yaml
└── templates/
	├── deployment.yaml
	├── service.yaml
	├── configmap.yaml
	└── ingress.yaml
```

### Install Release
```bash
helm install aimran-dsl ./helm/aimran-dsl \
  --namespace aimran-dsl \
  --create-namespace \
  --values helm/values-prod.yaml
```

### Upgrade Release
```bash
helm upgrade aimran-dsl ./helm/aimran-dsl \
  --namespace aimran-dsl \
  --values helm/values-prod.yaml
```

## Database Setup

### Initialize Database

```bash
# Connect to PostgreSQL
psql -h postgres.aimran-dsl.svc.cluster.local \
	 -U aimran \
	 -d aimran_dsl

# Run migrations
\i migrations/001_initial_schema.sql
\i migrations/002_create_indexes.sql
```

### Database Schema

```sql
-- Core tables
CREATE TABLE data_sources (
	id UUID PRIMARY KEY,
	name VARCHAR(255) NOT NULL,
	type VARCHAR(50),
	location TEXT,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE datasets (
	id UUID PRIMARY KEY,
	name VARCHAR(255) NOT NULL,
	source_id UUID REFERENCES data_sources(id),
	row_count INT,
	size_bytes BIGINT,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE models (
	id UUID PRIMARY KEY,
	name VARCHAR(255) NOT NULL,
	version INT,
	framework VARCHAR(50),
	status VARCHAR(50),
	artifact_location TEXT,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE experiments (
	id UUID PRIMARY KEY,
	name VARCHAR(255) NOT NULL,
	description TEXT,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE experiment_runs (
	id UUID PRIMARY KEY,
	experiment_id UUID REFERENCES experiments(id),
	name VARCHAR(255),
	status VARCHAR(50),
	started_at TIMESTAMP,
	completed_at TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX idx_datasets_source_id ON datasets(source_id);
CREATE INDEX idx_models_name ON models(name);
CREATE INDEX idx_experiments_created ON experiments(created_at);
CREATE INDEX idx_runs_experiment_id ON experiment_runs(experiment_id);
```

## Environment Configuration

### Development Environment

`.env.dev`:
```
DATABASE_URL=postgresql://aimran:password@postgres:5432/aimran_dsl_dev
REDIS_URL=redis://redis:6379/0
LOG_LEVEL=Debug
API_PORT=5000
ENVIRONMENT=Development
ENABLE_SWAGGER=true
```

### Production Environment

`.env.prod`:
```
DATABASE_URL=postgresql://aimran:$(DB_PASSWORD)@postgres-prod.database.azure.com:5432/aimran_dsl
REDIS_URL=redis://redis-prod.redis.azure.com:6379
LOG_LEVEL=Information
API_PORT=80
ENVIRONMENT=Production
ENABLE_SWAGGER=false
JWT_SECRET=$(SECRET_JWT_KEY)
ENCRYPTION_KEY=$(SECRET_ENCRYPTION_KEY)
```

## Scaling Configuration

### Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-autoscaler
  namespace: aimran-dsl
spec:
  scaleTargetRef:
	apiVersion: apps/v1
	kind: Deployment
	name: aimran-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
	resource:
	  name: cpu
	  target:
		type: Utilization
		averageUtilization: 70
  - type: Resource
	resource:
	  name: memory
	  target:
		type: Utilization
		averageUtilization: 80
```

### Resource Requests/Limits

```yaml
resources:
  requests:
	cpu: 500m
	memory: 512Mi
  limits:
	cpu: 1000m
	memory: 1Gi
```

## Health Checks

### Liveness Probe
```yaml
livenessProbe:
  httpGet:
	path: /health/live
	port: 5000
  initialDelaySeconds: 30
  periodSeconds: 10
  failureThreshold: 3
```

### Readiness Probe
```yaml
readinessProbe:
  httpGet:
	path: /health/ready
	port: 5000
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 2
```

## Monitoring Setup

### Prometheus Scrape Config

```yaml
scrape_configs:
  - job_name: 'aimran-api'
	static_configs:
	  - targets: ['api:5000']
	metrics_path: '/metrics'
	scrape_interval: 15s
```

### Grafana Dashboards

1. **System Dashboard**: CPU, Memory, Disk usage
2. **API Dashboard**: Request rates, latency, errors
3. **ML Dashboard**: Model training progress, metrics
4. **Data Dashboard**: Ingestion rates, quality scores

### Alert Rules

```yaml
groups:
  - name: aimran-alerts
	rules:
	  - alert: HighErrorRate
		expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05

	  - alert: HighLatency
		expr: histogram_quantile(0.95, http_request_duration_seconds) > 0.5
```

## Backup & Restore

### Database Backups

```bash
# Full backup
pg_dump -h postgres.aimran-dsl.svc.cluster.local \
		-U aimran \
		aimran_dsl > backup_$(date +%Y%m%d).sql

# Upload to S3
aws s3 cp backup_*.sql s3://aimran-backups/

# Restore from backup
psql -h postgres.aimran-dsl.svc.cluster.local \
	 -U aimran \
	 aimran_dsl < backup_20240115.sql
```

### Persistent Volume Backups

```bash
# Create snapshot
kubectl create persistentvolumeclaim backup-claim \
  --size=100Gi -n aimran-dsl

# Copy data
kubectl cp aimran-dsl/pod:/data backup/data
```

## Troubleshooting

### Pod Not Starting

```bash
# Check pod status
kubectl describe pod -n aimran-dsl

# Check logs
kubectl logs deployment/aimran-api -n aimran-dsl

# Check events
kubectl get events -n aimran-dsl --sort-by='.lastTimestamp'
```

### Database Connection Issues

```bash
# Test connection
psql -h postgres.aimran-dsl.svc.cluster.local \
	 -U aimran \
	 -c "SELECT version();"

# Check pod networking
kubectl exec -it deployment/aimran-api \
  -n aimran-dsl -- ping postgres
```

### Memory Issues

```bash
# Check memory usage
kubectl top pods -n aimran-dsl

# Increase limits
kubectl set resources deployment aimran-api \
  --limits=memory=2Gi -n aimran-dsl
```

## Upgrade Procedure

1. Test in staging environment
2. Create backup of production database
3. Deploy new version:
   ```bash
   kubectl set image deployment/aimran-api \
	 aimran-api=aimran-dsl:v2.0 \
	 -n aimran-dsl
   ```
4. Monitor rollout:
   ```bash
   kubectl rollout status deployment/aimran-api -n aimran-dsl
   ```
5. If issues arise, rollback:
   ```bash
   kubectl rollout undo deployment/aimran-api -n aimran-dsl
   ```

## Security Best Practices

- Use private container registry
- Implement network policies
- Enable RBAC for Kubernetes
- Encrypt secrets at rest and in transit
- Regular security updates
- Use ServiceAccounts with minimal permissions
- Implement Pod Security Policies
- Enable audit logging

## Performance Tuning

- Index database queries
- Configure connection pooling
- Enable query caching
- Use CDN for static content
- Implement rate limiting
- Monitor slow queries
- Optimize resource requests

## Cost Optimization

- Right-size pod resources
- Use spot instances for non-critical workloads
- Implement resource limits
- Monitor and cleanup unused resources
- Use reserved instances
- Implement auto-scaling based on demand
