# AIMRAN Data Science Lab

A comprehensive, production-ready platform for data science, machine learning, and analytics workflows. Built with .NET 10, Kubernetes, and modern open-source technologies.

## 🎯 Features

### Data Management
- **Data Lake**: Centralized data ingestion and storage with quality monitoring
- **Data Warehouse**: OLAP cubes, dimensional modeling, and aggregations
- **Master Data Management**: Data governance, versioning, and deduplication
- **Data Catalog**: Metadata management, asset discovery, and lineage tracking

### Machine Learning
- **Feature Store**: Feature engineering, versioning, and serving
- **Model Registry**: Model artifact management with versioning and promotion
- **Experiment Tracking**: MLflow integration for experiment management
- **Model Serving**: Multi-environment deployment with inference API

### Monitoring & Observability
- **Metrics Collection**: Prometheus integration for system metrics
- **Distributed Tracing**: Jaeger integration for request tracing
- **Centralized Logging**: ELK Stack for log aggregation
- **Health Monitoring**: Component health checks and alerting

### Security & Authentication
- **OAuth2/OIDC**: Modern authentication with JWT tokens
- **Secrets Management**: Encrypted credential storage with rotation
- **RBAC**: Role-based access control and granular permissions
- **Audit Logging**: Complete traceability of all operations

## 📋 Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Desktop/Web UI (MAUI)                  │
└──────────────────────────┬──────────────────────────────┘
						   │
┌──────────────────────────┴──────────────────────────────┐
│                   API Gateway Layer                     │
├──────────────────────────────────────────────────────────┤
│  • Authentication & Authorization                       │
│  • Request Routing & Rate Limiting                      │
│  • Response Serialization & Compression                 │
└──────────────────────────┬──────────────────────────────┘
						   │
┌──────────────────────────┴──────────────────────────────┐
│              Core Service Layer                         │
├──────────────┬──────────────┬────────────┬──────────────┤
│   Data Mgmt  │   ML Engine  │ Monitoring │   Security   │
└──────────────┴──────────────┴────────────┴──────────────┘
						   │
┌──────────────────────────┴──────────────────────────────┐
│               Data & Storage Layer                      │
├──────────────┬──────────────┬──────────────────────────┤
│  PostgreSQL  │     Redis    │   Object Storage (S3)    │
└──────────────┴──────────────┴──────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL 14+
- Redis 6+

### Local Development

```bash
# Clone repository
git clone https://github.com/aimran-ai/aimran-dsl.git
cd aimran-dsl

# Install dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Start with Docker Compose
docker-compose -f docker-compose.dev.yml up -d

# Access services
# API: http://localhost:5000
# UI: http://localhost:3000
# Prometheus: http://localhost:9090
# Grafana: http://localhost:3001
```

### Production Deployment

```bash
# Build Docker images
docker build -t aimran-dsl:api ./AimranDataScienceLab.Web
docker build -t aimran-dsl:ml ./AimranDataScienceLab.Engine

# Deploy to Kubernetes
helm install aimran-dsl ./helm/aimran-dsl \
  --namespace aimran-dsl \
  --create-namespace \
  --values helm/values-prod.yaml

# Verify deployment
kubectl get pods -n aimran-dsl
```

## 📚 Project Structure

```
├── AIMRAN Data Science Lab/           # Main MAUI desktop application
│   ├── Services/                      # Business logic services
│   ├── UI/                           # MAUI UI pages and controls
│   └── App.xaml.cs                   # App entry point
│
├── AimranDataScienceLab.Engine/      # ML engine and algorithms
│   ├── ML/                           # Machine learning models
│   ├── Data/                         # Data processing
│   └── Models/                       # Core ML models
│
├── AimranDataScienceLab.Shared/      # Shared code and contracts
│   ├── Services/                     # Shared service interfaces
│   ├── Models/                       # Shared data models
│   └── Events/                       # Domain events
│
├── AimranDataScienceLab.Web/         # ASP.NET Core API
│   ├── Controllers/                  # API endpoints
│   ├── Middleware/                   # Custom middleware
│   └── Program.cs                    # Startup configuration
│
├── AimranDataScienceLab.Gateway/     # API Gateway & external integrations
│   ├── Clients/                      # External service clients
│   └── Middleware/                   # Gateway middleware
│
├── AimranDataScienceLab.Core/        # Core business services
│   ├── DataManagement/               # Data management services
│   ├── ML/                          # ML services
│   ├── Monitoring/                  # Observability services
│   └── Security/                    # Security services
│
├── docker/                           # Docker configurations
│   ├── Dockerfile                   # Multi-stage build
│   └── docker-compose.yml           # Local development
│
├── k8s/                             # Kubernetes manifests
│   ├── deployments/                 # Service deployments
│   ├── services/                    # K8s services
│   └── configmaps/                  # Configuration
│
├── helm/                            # Helm charts
│   ├── Chart.yaml
│   ├── values-dev.yaml
│   ├── values-prod.yaml
│   └── templates/
│
├── docs/                            # Documentation
│   ├── ARCHITECTURE.md              # System architecture
│   ├── API_DOCUMENTATION.md         # API reference
│   ├── DEPLOYMENT_GUIDE.md          # Deployment instructions
│   └── BEST_PRACTICES.md            # Development guidelines
│
├── tests/                           # Test projects
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
│
└── .github/workflows/               # GitHub Actions CI/CD
```

## 📖 Documentation

- **[Architecture](./ARCHITECTURE.md)** - System design, components, and deployment
- **[API Documentation](./API_DOCUMENTATION.md)** - Complete API reference
- **[Deployment Guide](./DEPLOYMENT_GUIDE.md)** - Production deployment steps
- **[Best Practices](./BEST_PRACTICES.md)** - Development guidelines

## Core Services

### Data Management Services

```csharp
// Data Lake
var dataLakeService = serviceProvider.GetRequiredService<IDataLakeService>();
var sourceId = await dataLakeService.RegisterDataSourceAsync(metadata);
var qualityReport = await dataLakeService.PerformQualityCheckAsync(datasetId);

// Data Warehouse
var warehouseService = serviceProvider.GetRequiredService<IDataWarehouseService>();
await warehouseService.LoadFactTableAsync(tableId, data);

// Master Data Management
var mdmService = serviceProvider.GetRequiredService<IMasterDataManagementService>();
var recordId = await mdmService.UpsertMasterDataAsync(record, userId, reason);

// Data Catalog
var catalogService = serviceProvider.GetRequiredService<IDataCatalogService>();
var assets = await catalogService.DiscoverAssetsAsync(criteria);
```

### ML Services

```csharp
// Feature Store
var featureStore = serviceProvider.GetRequiredService<IFeatureStoreService>();
var featureId = await featureStore.RegisterFeatureAsync(feature);
var vector = await featureStore.GetFeatureVectorAsync(entityId, featureGroupId);

// Model Registry
var modelRegistry = serviceProvider.GetRequiredService<IModelRegistryService>();
var modelId = await modelRegistry.RegisterModelAsync(model);
var deploymentId = await modelRegistry.DeployModelAsync(modelId, version, "production");

// Experiment Tracking
var experiments = serviceProvider.GetRequiredService<IExperimentTrackingService>();
var expId = await experiments.CreateExperimentAsync(experiment);
var runId = await experiments.StartRunAsync(expId, "run_1", userId);
```

### Monitoring Services

```csharp
// Metrics
var metrics = serviceProvider.GetRequiredService<IMetricsCollectorService>();
await metrics.RecordMetricAsync(new SystemMetric { ... });

// Observability
var observability = serviceProvider.GetRequiredService<IObservabilityService>();
var span = await observability.StartSpanAsync(traceId, "operation");
```

### Security Services

```csharp
// Authentication
var auth = serviceProvider.GetRequiredService<IAuthenticationProviderService>();
var token = await auth.AuthenticateAsync(username, password);

// Secrets Management
var secrets = serviceProvider.GetRequiredService<ISecretsManagerService>();
var secretId = await secrets.CreateSecretAsync(secret, userId);
```

## API Examples

### Register a Data Source

```bash
curl -X POST http://localhost:5000/api/v1/data-lake/sources \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
	"sourceName": "customer_database",
	"sourceType": "Database",
	"location": "postgresql://db.example.com/customers"
  }'
```

### Train an ML Model

```bash
curl -X POST http://localhost:5000/api/v1/experiments \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
	"experimentName": "churn_prediction",
	"description": "Testing churn prediction models"
  }'
```

### Deploy a Model

```bash
curl -X POST http://localhost:5000/api/v1/models/model_xyz/deploy \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
	"version": 1,
	"environment": "production",
	"replicas": 3
  }'
```

## 🔐 Security

- **Authentication**: OAuth2/OIDC with JWT
- **Encryption**: AES-256-GCM for secrets
- **Authorization**: Role-based access control
- **Secrets Management**: Encrypted credential storage
- **Audit Logging**: Complete operation traceability
- **TLS**: Encrypted data in transit

## 📊 Monitoring

- **Prometheus**: Metrics collection and querying
- **Grafana**: Dashboards and visualization
- **ELK Stack**: Centralized logging
- **Jaeger**: Distributed tracing
- **Health Checks**: Component status monitoring

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test AimranDataScienceLab.Engine.Tests

# With code coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## 🚢 Deployment

### Quick Deploy

```bash
# Docker Compose (Development)
docker-compose up -d

# Kubernetes (Production)
kubectl apply -f k8s/
```

### Helm Deploy

```bash
helm install aimran-dsl ./helm/aimran-dsl \
  --namespace aimran-dsl \
  --create-namespace
```

## 📈 Scaling

- **Horizontal**: Stateless services scale with load balancing
- **Vertical**: Increase resource requests/limits
- **Caching**: Redis for feature and query caching
- **Partitioning**: Database and feature store partitioning
- **Async**: Job queues for long-running operations

## 🤝 Contributing

1. Create feature branch
2. Make changes
3. Add tests
4. Submit pull request
5. Await code review

## 📝 License

MIT License - See LICENSE file

## 📞 Support

- **Issues**: GitHub Issues
- **Documentation**: See `/docs` directory
- **Examples**: See `/samples` directory

## 🗺️ Roadmap

- [ ] Advanced AutoML capabilities
- [ ] Real-time streaming support
- [ ] Graph database integration
- [ ] Advanced visualization tools
- [ ] Multi-tenant SaaS deployment
- [ ] Edge ML deployment support
- [ ] Advanced data governance

## 🎓 Learn More

- Review ARCHITECTURE.md for system design
- Check API_DOCUMENTATION.md for API details
- Follow DEPLOYMENT_GUIDE.md for production setup
- Study BEST_PRACTICES.md for code standards

---

**AIMRAN Data Science Lab** - Empowering data-driven organizations with production-grade ML infrastructure.
