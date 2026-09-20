# AIMRAN Data Science Lab - API Documentation

## API Overview

RESTful API for managing data science workflows, ML models, and infrastructure.

**Base URL**: `https://api.aimran-dsl.example.com/api/v1`
**Authentication**: Bearer token (JWT) or API Key
**Format**: JSON

## Authentication

### JWT Token Authentication

```bash
curl -X POST https://api.aimran-dsl.example.com/api/v1/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{
	"username": "user@example.com",
	"password": "secure_password"
  }'
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_value",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### API Key Authentication

```bash
curl -X GET https://api.aimran-dsl.example.com/api/v1/data-lake/sources \
  -H "Authorization: ApiKey sk_1234567890abcdef"
```

## Data Management APIs

### Data Lake

#### Register Data Source
```
POST /data-lake/sources
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "sourceName": "customer_database",
  "sourceType": "Database",
  "location": "postgresql://db.example.com/customers",
  "metadata": {
	"schema": "public",
	"tables": ["customers", "orders"]
  }
}
```

**Response** (201 Created):
```json
{
  "sourceId": "source_12345",
  "sourceName": "customer_database",
  "createdAt": "2024-01-15T10:30:00Z",
  "status": "registered"
}
```

#### Ingest Data
```
POST /data-lake/{sourceId}/ingest
Content-Type: application/json

{
  "dataPath": "s3://bucket/data/customers.csv",
  "transformRules": {
	"normalize_email": true,
	"remove_duplicates": true
  }
}
```

**Response**:
```json
{
  "ingestionId": "ingest_xyz",
  "status": "in_progress",
  "startTime": "2024-01-15T10:35:00Z"
}
```

#### Get Data Quality Report
```
GET /data-lake/{datasetId}/quality
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "datasetId": "dataset_123",
  "overallScore": 92.5,
  "completeness": 95,
  "validity": 98,
  "consistency": 92,
  "accuracy": 90,
  "issues": [
	{
	  "column": "email",
	  "type": "invalid_format",
	  "count": 15,
	  "severity": "warning"
	}
  ],
  "recommendations": [
	"Validate email format for 15 records",
	"Handle missing values in phone column"
  ]
}
```

### Data Catalog

#### Discover Assets
```
GET /data-catalog/assets?assetType=Table&owner=data_team&tags=customer
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "assets": [
	{
	  "assetId": "asset_001",
	  "assetName": "customers_prod",
	  "assetType": "Table",
	  "owner": "data_team",
	  "description": "Production customer records",
	  "tags": ["customer", "production", "critical"],
	  "sensitivity": "Confidential",
	  "createdAt": "2023-12-01T00:00:00Z"
	}
  ],
  "total": 1
}
```

#### Get Asset Lineage
```
GET /data-catalog/assets/{assetId}/lineage
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "assetId": "asset_001",
  "upstream": [
	{
	  "assetId": "source_001",
	  "assetName": "raw_customers",
	  "transformationSteps": ["cleaning", "normalization"]
	}
  ],
  "downstream": [
	{
	  "assetId": "model_001",
	  "assetName": "churn_model"
	}
  ]
}
```

## Machine Learning APIs

### Models

#### Register Model
```
POST /models
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "modelName": "churn_prediction_v1",
  "modelType": "Classification",
  "frameworkType": "XGBoost",
  "trainingDate": "2024-01-14T00:00:00Z",
  "hyperparameters": {
	"max_depth": 6,
	"learning_rate": 0.1,
	"n_estimators": 100
  },
  "metrics": {
	"accuracy": 0.92,
	"precision": 0.89,
	"recall": 0.85,
	"f1Score": 0.87,
	"auc": 0.93
  },
  "artifactLocation": "s3://models/churn_v1.pkl"
}
```

**Response** (201 Created):
```json
{
  "modelId": "model_xyz",
  "version": 1,
  "status": "Registered",
  "createdAt": "2024-01-15T11:00:00Z"
}
```

#### Get Model Details
```
GET /models/{modelId}
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "modelId": "model_xyz",
  "modelName": "churn_prediction_v1",
  "versions": [
	{
	  "version": 1,
	  "status": "Production",
	  "metrics": {...},
	  "deployments": [
		{
		  "environment": "production",
		  "endpoint": "https://inference.example.com/v1/classify",
		  "status": "Active"
		}
	  ]
	}
  ]
}
```

#### Deploy Model
```
POST /models/{modelId}/deploy
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "version": 1,
  "environment": "production",
  "replicas": 3
}
```

**Response**:
```json
{
  "deploymentId": "deployment_abc",
  "status": "Deploying",
  "endpoint": "https://inference.example.com/v1/classify",
  "estimatedDeploymentTime": 300
}
```

### Experiments

#### Create Experiment
```
POST /experiments
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "experimentName": "churn_model_optimization",
  "description": "Testing different hyperparameters for churn prediction"
}
```

**Response**:
```json
{
  "experimentId": "exp_001",
  "experimentName": "churn_model_optimization",
  "createdAt": "2024-01-15T12:00:00Z",
  "status": "Active"
}
```

#### Start Experiment Run
```
POST /experiments/{experimentId}/runs
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "runName": "run_with_lr_0.01",
  "parameters": {
	"learning_rate": 0.01,
	"batch_size": 32,
	"epochs": 50
  }
}
```

**Response**:
```json
{
  "runId": "run_001",
  "experimentId": "exp_001",
  "runName": "run_with_lr_0.01",
  "status": "Running",
  "startTime": "2024-01-15T12:05:00Z"
}
```

#### Log Metrics
```
POST /runs/{runId}/metrics
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "metrics": {
	"train_loss": 0.245,
	"val_loss": 0.267,
	"train_accuracy": 0.94,
	"val_accuracy": 0.91
  },
  "step": 50
}
```

**Response**:
```json
{
  "recorded": true,
  "timestamp": "2024-01-15T12:35:00Z"
}
```

### Features

#### Register Feature
```
POST /features
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "featureName": "customer_lifetime_value",
  "featureGroup": "customer_features",
  "dataType": "numeric",
  "description": "Total value of customer purchases",
  "transformationLogic": {
	"aggregation": "sum",
	"sourceTable": "orders",
	"groupBy": "customer_id"
  }
}
```

**Response**:
```json
{
  "featureId": "feat_001",
  "featureName": "customer_lifetime_value",
  "version": 1,
  "status": "registered"
}
```

#### Get Feature Vector
```
GET /features/{featureGroupId}/entities/{entityId}/vector
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "entityId": "cust_123",
  "timestamp": "2024-01-15T13:00:00Z",
  "features": {
	"customer_lifetime_value": 5432.50,
	"avg_order_value": 125.30,
	"purchase_frequency": 12,
	"days_since_last_purchase": 5
  }
}
```

## Monitoring APIs

### Metrics

#### Get System Metrics
```
GET /metrics?startTime=2024-01-15T00:00:00Z&endTime=2024-01-15T23:59:59Z
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "metrics": [
	{
	  "name": "api_request_duration_ms",
	  "value": 45.3,
	  "timestamp": "2024-01-15T12:00:00Z",
	  "labels": {
		"endpoint": "/models",
		"method": "GET"
	  }
	}
  ]
}
```

### Health Check

```
GET /health
```

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T13:30:00Z",
  "components": {
	"database": "healthy",
	"cache": "healthy",
	"messageQueue": "healthy"
  },
  "responseTime": 5
}
```

### Logs

#### Search Logs
```
GET /logs/search?component=model_service&level=ERROR&limit=50
Authorization: Bearer {accessToken}
```

**Response**:
```json
{
  "logs": [
	{
	  "timestamp": "2024-01-15T12:45:30Z",
	  "level": "ERROR",
	  "component": "model_service",
	  "message": "Failed to load model artifact",
	  "context": {
		"modelId": "model_xyz",
		"error": "S3 connection timeout"
	  }
	}
  ],
  "total": 1
}
```

## Error Responses

### Standard Error Format
```json
{
  "error": {
	"code": "INVALID_REQUEST",
	"message": "Required field 'modelName' is missing",
	"details": {
	  "field": "modelName",
	  "description": "Model name must be provided"
	},
	"timestamp": "2024-01-15T13:45:00Z",
	"requestId": "req_12345"
  }
}
```

### Common Error Codes
- `INVALID_REQUEST` (400): Malformed request
- `UNAUTHORIZED` (401): Missing or invalid authentication
- `FORBIDDEN` (403): Insufficient permissions
- `NOT_FOUND` (404): Resource not found
- `CONFLICT` (409): Resource already exists
- `RATE_LIMITED` (429): Too many requests
- `INTERNAL_ERROR` (500): Server error

## Rate Limiting

- Standard: 1000 requests/hour per API key
- Burst: 100 requests/minute
- Model inference: 10000 requests/minute

Headers included in responses:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1705336800
```

## Pagination

Endpoints returning lists support pagination:

```
GET /data-lake/sources?limit=20&offset=0
```

Response includes:
```json
{
  "items": [...],
  "limit": 20,
  "offset": 0,
  "total": 150,
  "next": "/data-lake/sources?limit=20&offset=20"
}
```

## Webhooks

Register webhooks for event notifications:

```
POST /webhooks
Content-Type: application/json

{
  "url": "https://your-app.com/webhooks/model-events",
  "events": ["model.trained", "model.deployed", "model.failed"],
  "retryPolicy": {
	"maxRetries": 3,
	"backoffMs": 100
  }
}
```

Events will be POSTed to your webhook URL with:
```json
{
  "eventId": "evt_001",
  "eventType": "model.trained",
  "timestamp": "2024-01-15T14:00:00Z",
  "data": {
	"modelId": "model_xyz",
	"version": 1,
	"metrics": {...}
  }
}
```
