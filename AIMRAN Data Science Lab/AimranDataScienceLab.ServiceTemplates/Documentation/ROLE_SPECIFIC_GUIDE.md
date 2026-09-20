# AIMRAN Service Roles - Implementation Guide

## Overview

This guide covers how to implement services for each role in the AIMRAN ecosystem. Each role has specific capabilities, dependencies, and deployment requirements.

## Table of Contents

- [Analyst Role](#analyst-role)
- [Scientist Role](#scientist-role)
- [Engineer Role](#engineer-role)
- [AI Engineer Role](#ai-engineer-role)
- [Infrastructure Services](#infrastructure-services)
- [Deployment Patterns](#deployment-patterns)
- [Best Practices](#best-practices)

---

## Analyst Role

### Purpose

Analysts perform SQL execution, dashboard management, report generation, and exploratory data analysis (EDA). They focus on translating business questions into data queries and insights.

### Key Capabilities

- **SQL Query Execution**: Execute complex SQL queries against data warehouses
- **Dashboard Management**: Create, manage, and configure dashboards
- **Report Generation**: Generate reports in multiple formats (PDF, Excel, HTML)
- **Data Visualization**: Create charts, graphs, and visual representations
- **Query Optimization**: Analyze and optimize query performance
- **EDA Analysis**: Perform exploratory data analysis tasks

### Primary Dependencies

- `ServiceRegistry` - For service discovery
- `DatabaseService` - For data access
- `EventBus` - For event distribution
- `AuthenticationService` - For security
- `VisualizationEngine` - For rendering visualizations

### Resource Requirements

- **CPU Request**: 500m
- **CPU Limit**: 2
- **Memory Request**: 512Mi
- **Memory Limit**: 2Gi
- **Min Replicas**: 2
- **Max Replicas**: 10

### Implementation Example

```csharp
using AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates;

public class MyAnalystService : AnalystServiceBase
{
	public MyAnalystService(ILogger<MyAnalystService> logger)
		: base(logger)
	{
	}

	protected override async Task<string> ExecuteQueryAsync(
		string query, 
		CancellationToken cancellationToken = default)
	{
		// Implement SQL query execution
		// 1. Validate query
		// 2. Execute against database
		// 3. Return results
		var results = await _database.ExecuteQueryAsync(query);
		return JsonConvert.SerializeObject(results);
	}

	protected override async Task<Dictionary<string, object?>> GetDashboardDataAsync(
		string dashboardId, 
		CancellationToken cancellationToken = default)
	{
		// Fetch and aggregate dashboard data
		return await _dashboardService.GetDataAsync(dashboardId);
	}

	protected override async Task<byte[]> GenerateReportAsync(
		string reportId,
		Dictionary<string, string> parameters,
		CancellationToken cancellationToken = default)
	{
		// Generate report in specified format
		return await _reportService.GenerateAsync(reportId, parameters);
	}
}
```

### Deployment Configuration

**Docker Compose:**
```yaml
analyst-service:
  image: aimran/analyst-service:latest
  environment:
	- ASPNETCORE_ENVIRONMENT=Production
	- ANALYST_MODE=true
	- SERVICE_REGISTRY_URL=http://service-registry:5000
  depends_on:
	- service-registry
	- database-service
  ports:
	- "5010:5000"
```

**Kubernetes:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: analyst-service
spec:
  replicas: 2
  template:
	spec:
	  containers:
	  - name: analyst-service
		image: aimran/analyst-service:latest
		resources:
		  requests:
			cpu: 500m
			memory: 512Mi
		  limits:
			cpu: 2
			memory: 2Gi
```

---

## Scientist Role

### Purpose

Scientists develop machine learning models, run experiments, perform hyperparameter tuning, and validate models. They focus on creating and improving ML/AI solutions.

### Key Capabilities

- **Model Development**: Build and prototype ML models
- **Model Training**: Train models on datasets
- **Experiment Management**: Track experiments and configurations
- **Hyperparameter Tuning**: Optimize model hyperparameters
- **Model Validation**: Validate models with test datasets
- **Performance Metrics**: Calculate and track model metrics
- **Feature Engineering**: Create and engineer features

### Primary Dependencies

- `ServiceRegistry` - For service discovery
- `ModelRegistry` - For model storage and versioning
- `DataService` - For dataset access
- `ComputeCluster` - For distributed computation
- `ExperimentTracker` - For experiment tracking
- `EventBus` - For event distribution

### Resource Requirements

- **CPU Request**: 2000m (high for training)
- **CPU Limit**: 8
- **Memory Request**: 4Gi
- **Memory Limit**: 16Gi
- **Min Replicas**: 1
- **Max Replicas**: 5

### Implementation Example

```csharp
using AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates;

public class MyScientistService : ScientistServiceBase
{
	public MyScientistService(ILogger<MyScientistService> logger)
		: base(logger)
	{
	}

	protected override async Task<Dictionary<string, object?>> TrainModelAsync(
		string modelId,
		Dictionary<string, string> parameters,
		CancellationToken cancellationToken = default)
	{
		// Load data
		var data = await _dataService.LoadDatasetAsync(parameters["dataset_id"]);

		// Create and train model
		var model = _mlService.CreateModel(parameters["model_type"]);
		var metrics = await model.TrainAsync(data, parameters);

		// Save model
		await _modelRegistry.SaveModelAsync(modelId, model);

		return metrics;
	}

	protected override async Task<string> RunExperimentAsync(
		string experimentId,
		Dictionary<string, string> config,
		CancellationToken cancellationToken = default)
	{
		// Run experiment with configuration
		var result = await _experimentTracker.RunAsync(experimentId, config);
		return result.Id;
	}

	protected override async Task<Dictionary<string, object?>> ValidateModelAsync(
		string modelId,
		string datasetId,
		CancellationToken cancellationToken = default)
	{
		// Load model and validation data
		var model = await _modelRegistry.LoadModelAsync(modelId);
		var data = await _dataService.LoadDatasetAsync(datasetId);

		// Validate
		return await model.ValidateAsync(data);
	}
}
```

### Deployment Configuration

**Docker Compose:**
```yaml
scientist-service:
  image: aimran/scientist-service:latest
  environment:
	- ASPNETCORE_ENVIRONMENT=Production
	- SCIENTIST_MODE=true
	- EXPERIMENT_TRACKING_ENABLED=true
  depends_on:
	- service-registry
	- model-registry
  resources:
	limits:
	  cpus: '8'
	  memory: 16G
```

---

## Engineer Role

### Purpose

Engineers orchestrate data pipelines, perform ETL operations, validate data quality, and manage system integration. They focus on building reliable data infrastructure.

### Key Capabilities

- **Data Pipeline Orchestration**: Orchestrate complex data workflows
- **ETL Processing**: Extract, transform, and load data
- **Data Validation**: Validate data quality and integrity
- **Schema Management**: Manage and version data schemas
- **Data Quality Monitoring**: Monitor data quality metrics
- **Integration Management**: Integrate with various data sources
- **Batch/Stream Processing**: Handle batch and streaming data

### Primary Dependencies

- `ServiceRegistry` - For service discovery
- `DataStore` - For data storage
- `EventBus` - For event distribution
- `WorkflowEngine` - For pipeline orchestration
- `MetadataStore` - For metadata management
- `QualityMonitor` - For quality monitoring

### Resource Requirements

- **CPU Request**: 1000m
- **CPU Limit**: 4
- **Memory Request**: 2Gi
- **Memory Limit**: 8Gi
- **Min Replicas**: 2
- **Max Replicas**: 10

### Implementation Example

```csharp
using AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates;

public class MyEngineerService : EngineerServiceBase
{
	public MyEngineerService(ILogger<MyEngineerService> logger)
		: base(logger)
	{
	}

	protected override async Task<string> ExecutePipelineAsync(
		string pipelineId,
		Dictionary<string, string> parameters,
		CancellationToken cancellationToken = default)
	{
		// Execute pipeline
		var execution = await _workflowEngine.ExecuteAsync(pipelineId, parameters);
		return execution.Id;
	}

	protected override async Task<Dictionary<string, object?>> TransformDataAsync(
		string sourceId,
		string targetId,
		Dictionary<string, string> transformRules,
		CancellationToken cancellationToken = default)
	{
		// Load source data
		var data = await _dataStore.LoadDataAsync(sourceId);

		// Apply transformations
		var transformed = await _etlEngine.TransformAsync(data, transformRules);

		// Save to target
		await _dataStore.SaveDataAsync(targetId, transformed);

		return new Dictionary<string, object?> { { "rows_processed", transformed.Count } };
	}

	protected override async Task<Dictionary<string, object?>> ValidateDataAsync(
		string datasetId,
		CancellationToken cancellationToken = default)
	{
		// Load data and validate
		var data = await _dataStore.LoadDataAsync(datasetId);
		return await _qualityMonitor.ValidateAsync(data);
	}
}
```

---

## AI Engineer Role

### Purpose

AI Engineers deploy trained models, serve predictions, optimize models for performance, and monitor inference pipelines. They focus on production ML operations.

### Key Capabilities

- **Model Deployment**: Deploy models to production
- **Inference Serving**: Serve real-time predictions
- **Batch Prediction**: Generate batch predictions
- **Model Monitoring**: Monitor model performance
- **Performance Optimization**: Optimize for latency/throughput
- **A/B Testing**: Run A/B tests on models
- **Model Versioning**: Manage model versions

### Primary Dependencies

- `ServiceRegistry` - For service discovery
- `ModelRegistry` - For model access
- `InferenceEngine` - For serving predictions
- `MonitoringService` - For observability
- `LoggingService` - For logging
- `FeatureStore` - For feature access

### Resource Requirements

- **CPU Request**: 2000m
- **CPU Limit**: 8
- **Memory Request**: 4Gi
- **Memory Limit**: 16Gi
- **Min Replicas**: 3
- **Max Replicas**: 20

### Implementation Example

```csharp
using AimranDataScienceLab.ServiceTemplates.Base.RoleBasedServiceTemplates;

public class MyAIEngineerService : AIEngineerServiceBase
{
	public MyAIEngineerService(ILogger<MyAIEngineerService> logger)
		: base(logger)
	{
	}

	protected override async Task<string> DeployModelAsync(
		string modelId,
		string modelVersion,
		Dictionary<string, string> deploymentConfig,
		CancellationToken cancellationToken = default)
	{
		// Load model
		var model = await _modelRegistry.LoadModelAsync(modelId, modelVersion);

		// Deploy to inference engine
		var deploymentId = await _inferenceEngine.DeployAsync(model, deploymentConfig);

		return deploymentId;
	}

	protected override async Task<Dictionary<string, object?>> InferAsync(
		string modelId,
		Dictionary<string, object?> features,
		CancellationToken cancellationToken = default)
	{
		// Get prediction from deployed model
		var result = await _inferenceEngine.PredictAsync(modelId, features);

		// Log prediction for monitoring
		await _monitoringService.RecordPredictionAsync(modelId, result);

		return result;
	}

	protected override async Task<string> BatchPredictAsync(
		string modelId,
		string datasetId,
		CancellationToken cancellationToken = default)
	{
		// Load dataset
		var data = await _dataStore.LoadDataAsync(datasetId);

		// Generate batch predictions
		var jobId = await _inferenceEngine.BatchPredictAsync(modelId, data);

		return jobId;
	}
}
```

---

## Infrastructure Services

### Gateway Service

Routes requests, handles load balancing and authentication.

**Typical Deployment:**
- Replicas: 3-5 (high availability)
- Resource-light but critical

### Registry Service

Maintains service discovery and health information.

**Typical Deployment:**
- Replicas: 1-3 (quorum-based)
- Stateful service with persistent storage
- High availability critical

### IPC Broker Service

Routes messages between services.

**Typical Deployment:**
- Replicas: 1-5 (based on load)
- Stateful service with message persistence
- Critical for inter-service communication

### Monitoring Service

Collects metrics, logs, and traces.

**Typical Deployment:**
- Replicas: 1-3 (high availability)
- Stateful service with time-series storage
- Essential for observability

---

## Deployment Patterns

### Single Service Deployment

Deploy a single service:

```bash
# Generate templates
dotnet ServiceTemplateGenerator.dll generate \
  --name MyAnalystService \
  --label my-analyst \
  --role analyst \
  --output ./deployment

# Deploy to Kubernetes
kubectl apply -f deployment/kubernetes-my-analyst.yaml
```

### Full Stack Deployment

Deploy all services together:

```bash
# Generate all templates
./scripts/generate-all-services.sh

# Deploy infrastructure first
kubectl apply -f deployment/infrastructure/

# Deploy role services
kubectl apply -f deployment/services/

# Verify deployment
kubectl get all -n aimran
```

---

## Best Practices

### 1. Resource Management

- Set appropriate CPU and memory requests based on role
- Use horizontal pod autoscaling for stateless services
- Monitor resource usage and adjust as needed

### 2. Health Checks

- Implement health checks for all services
- Use livenessProbe and readinessProbe
- Monitor health status metrics

### 3. Configuration Management

- Use ConfigMaps for non-sensitive configuration
- Use Secrets for sensitive data (credentials, API keys)
- Use environment-specific configurations

### 4. Logging and Monitoring

- Implement structured logging
- Use centralized logging aggregation
- Set up distributed tracing for debugging
- Monitor business and technical metrics

### 5. Deployment Strategy

- Use rolling updates for new versions
- Implement blue-green deployments for critical services
- Test in staging before production
- Have rollback procedures ready

### 6. Security

- Run containers as non-root users
- Use network policies for traffic control
- Implement authentication and authorization
- Scan images for vulnerabilities
- Use secrets management solutions

### 7. Service Dependencies

- Document all service dependencies
- Implement graceful degradation
- Use circuit breakers for external calls
- Implement retry logic with exponential backoff

### 8. Version Management

- Use semantic versioning for services
- Tag images with version numbers
- Maintain version compatibility
- Plan for version upgrades

---

## Troubleshooting

### Common Issues

**Service fails to start:**
- Check logs: `kubectl logs deployment/service-name -n aimran`
- Verify configuration: `kubectl describe pod pod-name -n aimran`
- Check resource availability

**Service is not responding:**
- Check network connectivity
- Verify service registration in registry
- Check health check endpoints

**Performance issues:**
- Monitor resource usage
- Check for resource limits
- Review dependency performance
- Analyze slow queries/operations

**Data quality issues:**
- Verify data validation rules
- Check transformation logic
- Review data source
- Monitor quality metrics

---

## References

- [Service Template API Documentation](./API_DOCUMENTATION.md)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [AIMRAN Architecture](../../../docs/ARCHITECTURE.md)
