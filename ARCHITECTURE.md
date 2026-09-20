# Aimran Data Science Lab — Complete Architecture Document

> **Version:** 0.3.0 (Python) / 1.0.0 (CLI)  
> **License:** MIT  
> **Author:** Aimran Data Science Lab <lab@aimran.ai>

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Technology Stack](#2-technology-stack)
3. [Architecture Diagram](#3-architecture-diagram)
4. [Component Architecture](#4-component-architecture)
   - 4.1 Python Backend (Core)
   - 4.2 REST API Layer (FastAPI)
   - 4.3 AutoML Engine
   - 4.4 Data Layer
   - 4.5 Model Layer
   - 4.6 Experiment Tracking
   - 4.7 Explainability Engine
   - 4.8 Drift Detection
   - 4.9 Monitoring & Observability
   - 4.10 Multi-Language Service Architecture
   - 4.11 Security & Authentication
   - 4.12 C# .NET MAUI Blazor Desktop/Mobile App
   - 4.13 User Interfaces
   - 4.14 CLI (Click)
   - 4.15 Celery Worker
   - 4.16 Pipeline Orchestration
5. [API Endpoints Reference](#5-api-endpoints-reference)
6. [Deployment Architecture](#6-deployment-architecture)
7. [Database & Storage Layout](#7-database--storage-layout)
8. [Known Issues & Errors](#8-known-issues--errors)
9. [Areas Under Development](#9-areas-under-development)
10. [Areas Needing Improvement](#10-areas-needing-improvement)
11. [Testing Strategy](#11-testing-strategy)
12. [CI/CD Pipeline](#12-cicd-pipeline)
13. [Security Posture](#13-security-posture)
14. [Performance Characteristics](#14-performance-characteristics)

---

## 1. System Overview

Aimran Data Science Lab is a comprehensive, enterprise-grade data science and MLOps platform. It provides end-to-end capabilities spanning data ingestion, exploratory analysis, AutoML, model training, serving, monitoring, drift detection, explainability, experiment tracking, and multi-language service orchestration.

**Core Objectives:**
- Transform raw data into actionable intelligence via ML/AI
- Provide a unified platform for reproducible research
- Enable multi-language service orchestration (Python, C#, R, Rust)
- Support both real-time inference and batch/streaming pipelines
- Offer enterprise-grade security, rate limiting, and RBAC

---

## 2. Technology Stack

### 2.1 Python Stack (Primary)

| Layer | Technology | Version |
|---|---|---|
| **Language** | Python | >= 3.10 |
| **API Framework** | FastAPI | >= 0.104.0 |
| **ASGI Server** | Uvicorn | >= 0.24.0 |
| **ML Framework** | scikit-learn | >= 1.3.0 |
| **Data Processing** | pandas, numpy | pandas >= 2.1.0 |
| **Task Queue** | Celery + Redis | >= 5.3.4 |
| **Cache/Broker** | Redis | >= 5.0.1 |
| **Database ORM** | SQLAlchemy + Alembic | >= 2.0.0 |
| **Experiment Tracking** | MLflow | >= 2.8.0 |
| **CLI** | Click | >= 8.1.7 |
| **Dashboard** | Streamlit | latest |
| **Validation** | Pydantic | >= 2.4.0 |
| **HTTP Client** | httpx | >= 0.25.0 |
| **Metrics** | Prometheus Client | >= 0.18.0 |
| **Terminal UI** | Rich | >= 13.6.0 |
| **Testing** | pytest, pytest-cov, httpx | latest |
| **Build** | setuptools (PEP 621) | >= 68.0 |

### 2.2 C# .NET Stack (Desktop/Mobile)

| Component | Technology |
|---|---|
| **Framework** | .NET 10.0 |
| **UI** | .NET MAUI Blazor Hybrid |
| **Targets** | Windows, Android, iOS, macOS (Catalyst) |
| **Sub-projects** | Core, Engine, Gateway, Web, ServiceTemplates, Shared, Tests |
| **Integrations** | Python AI Engine, R Engine, Rust Engine |

### 2.3 Infrastructure & DevOps

| Component | Technology |
|---|---|
| **Containerization** | Docker (multi-stage) |
| **Orchestration** | Docker Compose (8 services) |
| **Kubernetes** | Deployment templates |
| **Monitoring** | Prometheus + Grafana |
| **CI/CD** | GitHub Actions (2 workflows) |
| **Cloud** | AWS CloudFormation templates |
| **Reverse Proxy** | Nginx (template) |
| **Process Mgmt** | Systemd (Linux), Windows Service (templates) |

---

## 3. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                USER INTERFACES                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────────┐  ┌──────────────────────────┐         │
│  │ Streamlit│  │ HTML/JS  │  │ .NET MAUI    │  │   CLI (Click)            │         │
│  │Dashboard │  │ Dashboard│  │ Blazor App   │  │  python cli.py <cmd>     │         │
│  │:8501     │  │:8080     │  │(Win/Android) │  │  27+ commands             │         │
│  └────┬─────┘  └────┬─────┘  └──────┬───────┘  └────────────┬─────────────┘         │
│       │              │              │                        │                       │
├───────┴──────────────┴──────────────┴────────────────────────┴───────────────────────┤
│                                API GATEWAY                                           │
│  ┌──────────────────────────────────────────────────────────────────────────────┐   │
│  │  FastAPI Application (port 8000)                                             │   │
│  │  ├── Middleware: GZip, CORS, API-Key Auth, Rate Limiter, Performance         │   │
│  │  ├── Endpoints: /predict, /predict-batch, /predict-stream                    │   │
│  │  ├── /models/*, /automl/*, /experiments/*, /explain/*, /drift/*              │   │
│  │  ├── /upload*, /admin/*, /health, /status, /metrics                          │   │
│  │  ├── WebSocket: /ws/metrics (real-time streaming)                            │   │
│  │  └── /v2/* (AutoML, Explainability, Drift Detection, Async Training)         │   │
│  └──────────────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                               SERVICE LAYER                                          │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌────────────────────────┐     │
│  │ Celery   │  │ Multi-Lang   │  │ Pipeline      │  │ Model Store            │     │
│  │ Worker   │  │ Orchestrator │  │ Orchestrator  │  │ (Versioning + Champion) │     │
│  │ (Redis)  │  │ (gRPC/IPC)   │  │ (DAG/Cron)    │  │                        │     │
│  └──────────┘  └──────┬───────┘  └──────┬────────┘  └────────────────────────┘     │
│                       │                  │                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │  Multi-Language Bridges: C# Bridge | Rust Bridge | R Engine Integration     │    │
│  │  Service Registry (Redis), IPC EventBus / Pub-Sub, Health Checks            │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                             ENGINE LAYER                                             │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌─────────────────────────┐    │
│  │ AutoML   │  │ Explain-     │  │ Drift         │  │ Feature Engineering     │    │
│  │ Engine   │  │ ability      │  │ Detection     │  │ + Feature Store         │    │
│  │(30+ algo)│  │ (SHAP/PDP)   │  │ (KS/Chi2)     │  │ (Parquet-backed)        │    │
│  └──────────┘  └──────────────┘  └──────┬────────┘  └─────────────────────────┘    │
│  ┌──────────┐  ┌──────────────┐         │           ┌─────────────────────────┐    │
│  │ Models   │  │ Experiment   │         │           │ Security               │    │
│  │ (RF/GB/  │  │ Manager      │         │           │ AuthManager, RBAC,     │    │
│  │  NN)     │  │ (JSON)       │         │           │ Rate Limiter           │    │
│  └──────────┘  └──────────────┘         │           └─────────────────────────┘    │
│  ┌──────────┐  ┌──────────────┐         │                                           │
│  │ Bias     │  │ Data Quality │         │                                           │
│  │ Detector │  │ Engine       │         │                                           │
│  └──────────┘  └──────────────┘         │                                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                          DATA & STORAGE LAYER                                       │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌──────────┐  ┌────────────┐  │
│  │PostgreSQL│  │ Redis        │  │ MLflow        │  │ Parquet  │  │ JSON Files │  │
│  │(persist) │  │ (cache/broker)│  │ (tracking)    │  │(features)│  │(config)    │  │
│  └──────────┘  └──────────────┘  └───────────────┘  └──────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                         MONITORING & OBSERVABILITY                                   │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐                                 │
│  │Prometheus│  │ Grafana      │  │ Application   │                                 │
│  │:9090     │  │ :3000        │  │ Logs          │                                 │
│  └──────────┘  └──────────────┘  └───────────────┘                                 │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Component Architecture

### 4.1 Python Backend (Core)

**Location:** `src/`

The Python backend is organized into a modular package structure:

| Module | Purpose | Key Classes/Functions |
|---|---|---|
| `src/config.py` | Environment config | `Config`, `DevelopmentConfig`, `ProductionConfig`, `TestingConfig` |
| `src/utils.py` | Utility functions | `DataLoader`, `DataPreprocessor`, `ModelEvaluator` |
| `src/db_cache.py` | Database/cache | `DatabasePool` (sync/async), `CacheManager` (Redis TTL) |
| `src/performance.py` | Performance tools | `PerformanceProfiler`, `PerformanceOptimizer`, `CacheOptimizer` |
| `src/__init__.py` | Package exports | Exposes: config, DataLoader, DataPreprocessor, ModelEvaluator |
| `src/validate_new_modules.py` | Module validation | Validation script for new modules |
| `src/IMPLEMENTATION_VERIFICATION.py` | Verification | Implementation verification script |

**Version:** 0.3.0 (consistent across all files since the fix)

---

### 4.2 REST API Layer (FastAPI)

**Location:** `src/api/main.py` + `src/api/v2.py`

**Main API (`main.py`):**
- 25+ REST endpoints + 1 WebSocket endpoint
- ThreadPoolExecutor (4 workers) for CPU-bound ML inference
- Middleware stack: GZip → CORS → Performance → API-Key Auth
- Async inference, batch prediction, streaming prediction (NDJSON)
- K8s readiness/liveness probes (`/readiness`, `/liveness`)

**v2 API (`v2.py`):**
- APIRouter with `/v2` prefix
- AutoML run, global explainability, local explainability
- Drift detector fit/detect/list
- Uses separate `ModelExplainer` and `DataDriftDetector`

**v2 auth:** v2 endpoints now use `dependencies=[require_auth()]` on the router to enforce API key authentication.

---

### 4.3 AutoML Engine

**Location:** `src/automl/`

| File | Description |
|---|---|
| `engine.py` | `AutoMLConfig`, `AutoMLEngine` — iterative model search over 30+ sklearn classifiers/regressors |
| `pipeline_engine.py` | `ModelCandidate`, `AutoMLResult` — cross-validated leaderboard generation |

**Capabilities:**
- Task: classification, regression, auto-detect
- Configurable: time budget, CV folds, trial count, scoring metric
- Best model auto-registered to ModelStore
- Leaderboard with cross-validation scores
- In-memory job tracking (no persistence across restarts)

**Supported Models (30+):**
- Classifiers: LogisticRegression, RandomForest, GradientBoosting, SVC, KNN, DecisionTree, ExtraTrees, AdaBoost, GaussianNB, BernoulliNB, XGBoost (if installed), LightGBM (if installed), MLP, RidgeClassifier, SGD, PassiveAggressive, Perceptron, LinearSVC, NuSVC, CalibratedClassifierCV, Bagging, HistGradientBoosting
- Regressors: LinearRegression, Ridge, Lasso, ElasticNet, RandomForestRegressor, GradientBoostingRegressor, SVR, KNeighborsRegressor, DecisionTreeRegressor, ExtraTreesRegressor, AdaBoostRegressor, HuberRegressor, PassiveAggressiveRegressor, RANSACRegressor, TheilSenRegressor, HistGradientBoostingRegressor

---

### 4.4 Data Layer

**Location:** `src/data/`

| Component | File | Purpose |
|---|---|---|
| Data Loader | `loaders.py` | `DataLoader` (CSV, Parquet, Excel, JSON), `Preprocessor` (clean, scale, encode, optimize dtypes) |
| Data Versioning | `versioning.py` | `DataVersionStore`, `DataVersion` — track dataset versions with checksums |
| Data Quality | `quality.py` | `DataQualityEngine`, `DataProfiler`, `QualityRule`, `Report` — automated quality scoring |
| Data Catalog | `catalog.py` | `DataCatalog`, `DataAsset` — metadata catalog for datasets |
| Drift Detector | `drift_detector.py` | `DataDriftDetector` — KS test (numeric), Chi-squared (categorical) drift detection |

---

### 4.5 Model Layer

**Location:** `src/models/`

| Component | File | Description |
|---|---|---|
| Base Model | `base_model.py` | Abstract `BaseModel` with `train()`, `predict()`, `evaluate()` interface |
| Random Forest | `random_forest.py` | `RandomForestModel` wrapping sklearn |
| Gradient Boosting | `gradient_boosting.py` | `GradientBoostingModel` wrapping sklearn |
| Neural Network | `neural_network.py` | `NeuralNetworkModel` — MLPClassifier/Regressor via sklearn |
| Model Store | `model_store.py` | `ModelStore` — versioning + champion/warm-auto selection |
| Registry | `registry.py` | `MODEL_REGISTRY` dict, `register_model()`, `get_model()`, `list_models()` |

**ModelStore features:**
- Version tracking with timestamps
- Champion model promotion
- Metadata storage (metrics, task, feature names)
- Warm (latest) version fallback

---

### 4.6 Experiment Tracking

**Location:** `src/experiments/manager.py`

- `ExperimentManager` — create/list/get experiments
- `Experiment` — name, description, tags, runs
- `ExperimentRun` — params, metrics, tags, start/end time
- Persistence: JSON files on disk (no MLflow required)
- Best-run retrieval by metric

---

### 4.7 Explainability Engine

**Location:** `src/explainability/`

| Component | File | Description |
|---|---|---|
| Main Engine | `shap_engine.py` | `ExplainabilityEngine` — permutation importance, PDP, LIME-style local explanations |
| Model Explainer | `shap_explainer.py` | `ModelExplainer` — SHAP-based (with permutation fallback) global + local explanations |
| Bias Detector | `bias_detector.py` | `BiasDetector`, `BiasMetrics` — demographic parity, equal opportunity, equalized odds |

**Capabilities:**
- Local explanations (per-prediction feature contributions)
- Global explanations (aggregate feature importance)
- Partial dependence plots
- Caching of explanations per model_id
- Bias metrics for fairness auditing

---

### 4.8 Drift Detection

**Location:** `src/monitoring/drift_detector.py` + `src/data/drift_detector.py`

**Implementation:**
- `src/monitoring/drift_detector.py` — `DataDriftDetector`, `ModelPerformanceDriftDetector`, `DriftReport` (used by the API)

**Statistical methods:**
- Kolmogorov-Smirnov (KS) test — numeric features
- Chi-squared test — categorical features

**Drift severity:** Low, Medium, High — based on p-value thresholds

---

### 4.9 Monitoring & Observability

**Location:** `src/monitoring/`

| Component | File | Description |
|---|---|---|
| Performance Middleware | `performance.py` | `PerformanceMiddleware`, `MetricsCollector`, `SystemMonitor` |
| Metrics Tracking | `metrics.py` | `ExperimentTracker` (MLflow wrapper), `DataVersionControl` |
| Drift Detection | `drift_detector.py` | Production monitoring drift detection |

**Prometheus config:** `monitoring/prometheus.yml` — scrapes `aimran-api:8000` and `prometheus:9090`

**Grafana:** Pre-configured in docker-compose at port 3000

---

### 4.10 Multi-Language Service Architecture

**Location:** `src/multi_language/`

This is an ambitious sub-system for orchestrating services written in different languages.

| Component | File | Description |
|---|---|---|
| Service Gateway | `gateway.py` | gRPC gateway, `HealthServer`, `ServiceInstance` |
| Service Registry | `service_registry.py` | `ServiceRegistry`, `ServiceDiscovery`, `LoadBalancer` (Redis-backed) |
| Service Manager | `service_manager.py` | `ServiceManager` — YAML config, process lifecycle management |
| Orchestrator | `orchestrator.py` | `ServiceBridgeFactory` — C#, Rust, Python bridges |
| IPC Broker | `ipc_broker.py` | `EventBus`, `MessageQueue`, Pub/Sub with Redis |
| C# Bridge | `csharp_bridge.py` | `CSharpServiceBridge` — **stub (not implemented)** |
| Rust Bridge | `rust_bridge.py` | `RustServiceBridge` — **stub (not implemented)** |
| Management API | `management_api.py` | FastAPI management endpoints for services, health, topics, WebSocket |
| Monitoring | `monitoring.py` | `MetricsCollector`, `HealthMonitor`, `PerformanceAnalyzer`, `RealtimeMonitoringSystem` |
| Health Checks | `health_checks.py` | `HealthChecker`, `AlertManager`, `HealthCheckScheduler`, `IntegratedHealthSystem` |

**Current Status:** Architecture is designed and scaffolded. The C# and Rust bridges are stubs (return `NotImplementedError`). The remaining components have functional implementations but may not be fully production-tested.

---

### 4.11 Security & Authentication

**Location:** `src/security/auth.py` + `src/api/auth.py`

**Dual auth modes:**
1. **API Key (machine-to-machine):** `X-API-Key` header — bcrypt-hashed via passlib, sliding-window rate limiter
2. **JWT Bearer (human users/dashboard):** `Authorization: Bearer <token>` — short-lived access (60 min) + long-lived refresh (7 days) tokens via HS256

**How they interact:**
- `main.py` uses `AuthMiddleware` (ASGI middleware) that checks both `X-API-Key` header and `Authorization: Bearer` header
- `src.api.auth` provides `require_auth()` FastAPI dependency used by v2 router and admin routes
- Login flow: `POST /auth/login` with username+password → returns JWT access + refresh tokens
- Token refresh: `POST /auth/refresh` with valid refresh token → new access + refresh tokens
- Admin user bootstrapped on startup with both API key and password

---

### 4.12 C# .NET MAUI Blazor Desktop/Mobile App

**Location:** `AIMRAN Data Science Lab/`

A separate .NET 10.0 solution with 7 projects:

| Project | Description |
|---|---|
| `AimranDataScienceLab.Core` | Core domain models and interfaces |
| `AimranDataScienceLab.Engine` | AI/ML engine (C# implementation) |
| `AimranDataScienceLab.Engine.Tests` | Engine unit tests |
| `AimranDataScienceLab.Gateway` | API gateway for C# services |
| `AimranDataScienceLab.ServiceTemplates` | Service scaffolding templates |
| `AimranDataScienceLab.Shared` | Shared utilities and DTOs |
| `AimranDataScienceLab.Web` | Blazor Hybrid web UI |

**Integration points:**
- Python AI engine bridge (via subprocess or gRPC)
- R engine integration (`r-engine/`)
- Rust engine integration (`rust-engine/`)
- Multi-target: `net10.0-windows`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`

---

### 4.13 User Interfaces

#### Streamlit Dashboard

**Location:** `ui/dashboard.py` (1097 lines)

Multi-page dashboard with sidebar navigation:
- **Overview** — system status, resource usage, health metrics
- **Models** — registry, versioning, champion promotion
- **AutoML** — run AutoML, view leaderboard
- **Predictions** — test model inference
- **Data** — upload, profile, data quality
- **Experiments** — create/view experiments and runs
- **Drift** — detect and visualize data drift
- **Monitor** — real-time metrics, performance graphs
- **Admin** — user management

**Tech:** Streamlit, Plotly, requests, psutil, pandas

#### HTML/JS Management Panel

**Location:** `ui/management_panel.html`

Single-page HTML dashboard with:
- Bootstrap 5 styling
- Chart.js for visualizations
- Socket.IO for real-time WebSocket metrics
- REST API integration with the FastAPI backend

---

### 4.14 CLI (Click)

**Location:** `cli.py` (693 lines)

**27+ commands:**

| Command | Description |
|---|---|
| `version` | Platform version info |
| `health` | System health check (quick or comprehensive) |
| `status` | System status (JSON or table) |
| `explore` | Explore CSV/Parquet files |
| `serve` | Start FastAPI server |
| `dashboard` | Launch Streamlit dashboard |
| `test` | Run pytest suite |
| `benchmark` | CPU/memory benchmarks |
| `setup` | Bootstrap environment |
| `logs` | View/follow application logs |
| `automl` | Run AutoML via API |
| `train` | Train a model |
| `predict` | Run prediction |
| `drift` | Detect data drift |
| `admin create-user` | Create platform user |

---

### 4.15 Celery Worker

**Location:** `worker.py`

Two async tasks:
- `train_model_task` — async model training with retry
- `process_dataset_task` — async dataset processing

**Graceful degradation:** If Celery is not installed, provides no-op fallback functions.

---

### 4.16 Pipeline Orchestration

**Location:** `pipelines/`

| File | Description |
|---|---|
| `orchestrator.py` | `PipelineOrchestrator` — DAG-based pipeline execution with retry, history, cron scheduling, timeout |
| `training_pipeline.py` | `ModelTrainingPipeline` — end-to-end sklearn pipeline (load → preprocess → train → evaluate → save) |

---

## 5. API Endpoints Reference

### Main API (`/`)

| Method | Path | Description | Auth |
|---|---|---|---|
| GET | `/` | Root info + feature list | Public |
| GET | `/health` | Health check (CPU, memory, models) | Public |
| GET | `/status` | Detailed system status | Public |
| GET | `/metrics` | Metrics summary | Public |
| GET | `/readiness` | K8s readiness probe | Public |
| GET | `/liveness` | K8s liveness probe | Public |
| POST | `/auth/login` | Username+password login → JWT tokens | Public |
| POST | `/auth/refresh` | Exchange refresh token for new access+refresh tokens | Public |
| POST | `/predict` | Single prediction (optional explanation) | API Key / JWT |
| POST | `/predict-batch` | Batch prediction | API Key |
| POST | `/predict-stream` | Streaming prediction (NDJSON) | API Key |
| POST | `/upload` | Upload CSV | API Key |
| POST | `/upload-and-profile` | Upload + data profiling | API Key |
| GET | `/models` | List registered models | API Key |
| GET | `/models/{id}` | Get model details | API Key |
| POST | `/models/train` | Register a model type | API Key |
| POST | `/models/train/{id}` | Train model with data | API Key |
| DELETE | `/models/{id}` | Deregister model | API Key |
| GET | `/models/{id}/metrics` | Get model metrics | API Key |
| POST | `/models/{id}/champion` | Promote champion version | API Key |
| POST | `/automl/train` | Run AutoML (multipart) | API Key |
| GET | `/automl/{id}/status` | AutoML job status | API Key |
| GET | `/automl/{id}/leaderboard` | AutoML leaderboard | API Key |
| POST | `/experiments` | Create experiment | API Key |
| GET | `/experiments` | List experiments | API Key |
| GET | `/experiments/{id}` | Get experiment | API Key |
| POST | `/experiments/{id}/runs` | Start experiment run | API Key |
| PUT | `/experiments/runs/{id}/metrics` | Log metrics to run | API Key |
| GET | `/experiments/{id}/best-run` | Get best run | API Key |
| POST | `/explain` | Explain prediction | API Key |
| GET | `/explain/{id}/global` | Global feature importance | API Key |
| POST | `/drift/detect` | Detect data drift | API Key |
| GET | `/drift/{id}/report` | Get drift report | API Key |
| POST | `/admin/users` | Create user (admin only) | API Key+Admin |
| GET | `/admin/users` | List users (admin only) | API Key+Admin |
| DELETE | `/admin/users/{id}` | Deactivate user (admin) | API Key+Admin |
| GET | `/audit/log` | Retrieve audit log entries (filterable) | API Key / JWT |
| WS | `/ws/metrics` | Real-time metrics stream | None |

### v2 API (`/v2`)

| Method | Path | Description |
|---|---|---|
| POST | `/v2/automl/run` | AutoML on uploaded CSV |
| POST | `/v2/explain/global` | Global feature importance |
| POST | `/v2/explain/local` | Local (per-prediction) explanation |
| POST | `/v2/drift/fit` | Fit drift detector on reference data |
| POST | `/v2/drift/detect` | Detect drift against reference |
| GET | `/v2/drift/detectors` | List active drift detectors |
| GET | `/v2/info` | v2 API capabilities info |

---

## 6. Deployment Architecture

### 6.1 Docker Compose (9 Services)

| Service | Image | Port | Purpose |
|---|---|---|---|
| `nginx` | nginx:1.25-alpine | 80/443 | TLS-terminating reverse proxy |
| `api` | Custom build | _internal_ | FastAPI application (via Nginx proxy) |
| `worker` | Custom build | — | Celery worker |
| `dashboard` | Custom build | _internal_ | Streamlit dashboard (via Nginx proxy) |
| `postgres` | postgres:15-alpine | 5432 | Primary database |
| `redis` | redis:7-alpine | 6379 | Cache + message broker |
| `mlflow` | mlflow:latest | 5000 | Experiment tracking |
| `prometheus` | prom/latest | 9090 | Metrics collection |
| `grafana` | grafana/latest | 3000 | Metrics visualization |
| `jupyter` | Custom build | 8888 | JupyterLab |

**TLS flow:** `Client → HTTPS :443 → Nginx (cert.pem) → HTTP :8000 → API (internal)`

### 6.2 Docker Multi-Stage Build

- **Stage 1 (Builder):** Installs all Python dependencies + system build tools
- **Stage 2 (Runtime):** Minimal slim image with runtime libs + copied deps
- Non-root user (`aimran`) for security

### 6.3 Deployment Templates

**Location:** `templates/SERVICE_TEMPLATES.py`

Includes templates for:
- Docker Compose
- Kubernetes manifests
- Systemd Linux service
- Windows background service
- AWS CloudFormation
- Nginx reverse proxy

---

## 7. Database & Storage Layout

### 7.1 File System Storage

```
data/
├── auth/            # User credentials (JSON)
├── catalog/         # Data catalog metadata
├── feature_store/   # Feature store (Parquet)
└── versioned/       # Versioned datasets

logs/
├── experiments/     # Experiment tracking data
└── pipeline_runs/   # Pipeline execution logs
```

### 7.2 Redis Usage
- Celery message broker
- Cache manager (TTL-based)
- Service registry backend (multi-language)
- IPC EventBus / Pub-Sub

### 7.3 PostgreSQL Usage
- Primary application database
- SQLAlchemy ORM + Alembic migrations

### 7.4 MLflow
- SQLite-backed tracking server
- Artifact storage on local volume

---

## 8. Known Issues & Errors

### Critical Issues

| # | Issue | Location | Description | Impact | Status |
|---|---|---|---|---|---|
| 1 | **Version inconsistency** | `pyproject.toml` vs `setup.py` vs `src/__init__.py` | Versions were 0.3.0 / 0.1.0 / 0.1.0 | Package metadata inconsistent | **FIXED** |
| 2 | **Dual build system** | `pyproject.toml` (PEP 621) + `setup.py` | Both build configs present; package discovery undefined | Potential conflicts, `pip install -e .` could silently fail | **FIXED** — `setup.py` deleted; `[tool.setuptools.packages.find]` added to `pyproject.toml` |
| 3 | **v2 endpoints unprotected** | `src/api/v2.py` | v2 routes had no effective auth dependency | Unauthenticated access to all v2 endpoints (AutoML, explanations, drift) | **FIXED** — explicit `Depends(require_auth)` on every v2 route; `Depends(require_analyst)` on `/v2/automl/run` |
| 4 | **Broken import in cli.py** | `cli.py:92` | `from src.health_check import health_manager` — module does not exist | CLI `health` command crashes | **FIXED** — created `src/health_check.py` with `HealthManager` |
| 5 | **SHA-256 key hashing** | `src/security/auth.py` | Fast hash — brute-forceable at billions/sec | Credential leak risk | **FIXED** — replaced with bcrypt |
| 6 | **No JWT auth** | `src/security/auth.py`, `src/api/auth.py` | Only API keys, no human login flow | No session/token management | **FIXED** — added JWT access + refresh tokens |
| 7 | **Auth middleware only checked API keys** | `src/api/main.py` | `APIKeyMiddleware` ignored Bearer tokens | JWT users couldn't authenticate | **FIXED** — `AuthMiddleware` checks both |
| 8 | **In-memory rate limiter** | `src/security/auth.py` | Per-process, useless with multiple workers/ replicas | Double request budget per worker | **FIXED** — Redis-backed via `limits` + slowapi |

### Issues in Documentation vs. Reality

| # | Issue | Details |
|---|---|---|
| 5 | **Missing files referenced** | `FILE_INDEX.md` references `src/logger.py`, `src/health_check.py`, `src/api/routes.py`, `src/api/dependencies.py`, `config/` directory — none exist |
| 6 | **Directory structure mismatch** | `DIRECTORY_STRUCTURE.md` and `docs/index.md` list files not matching actual structure |
| 7 | **Duplicate CI workflows** | Both `ci-cd.yml` (older) and `ci.yml` existed | Wasted CI minutes, confusing PR results | **FIXED** — deleted ci-cd.yml, consolidated into single ci.yml |

### Module-Level Issues

| # | Issue | Details | Status |
|---|---|---|---|
| 8 | **`observability/` package empty** | `src/observability/` is an empty directory — placeholder, no implementation | Pending |
| 9 | **C# and Rust bridges are stubs** | `csharp_bridge.py` and `rust_bridge.py` raise `NotImplementedError` | Pending |
| 10 | **In-memory job storage** | AutoML jobs, drift detectors, and model registry stored in Python dicts | Data lost on every restart — not production | **FIXED** — Redis (AutoML jobs TTL, drift pickle) + PostgreSQL (model_registry) |
| 11 | **Celery fallback masks errors** | `worker.py:83-91` — if Celery not installed, silent fallback to no-op functions | Async tasks silently do nothing | **FIXED** — raises ImportError; /health checks worker via inspect() |
| 12 | **Missing gRPC implementation** | `proto/services.proto` defines services, but no gRPC server/client is implemented | The proto file is defined but unused | Pending |

---

## 9. Areas Under Development

### 9.1 Multi-Language Service Architecture
- **Status:** Scaffolded, partially implemented
- **Working:** Service registry, gateway, IPC broker (Redis Pub/Sub), health checks, monitoring
- **Not Working:** C# bridge (stub), Rust bridge (stub)
- **Partial:** Orchestrator — bridge factory exists but bridges are stubs

### 9.2 C# .NET MAUI Blazor Application
- **Status:** Solution structure complete, 7 projects scaffolded
- **Working:** Project references, multi-target configuration, build scripts
- **Not Working:** Actual application logic — integration with Python AI engine is defined but not tested

### 9.3 Observability Module
- **Status:** Placeholder only
- **Location:** `src/observability/` — empty directory
- **Planned:** Likely OpenTelemetry integration, distributed tracing

### 9.4 gRPC Service Layer
- **Status:** Proto definitions created but no implementation
- **Files:** `proto/services.proto` — defines ServiceRegistry, ServiceBroker, ServiceHeartbeat RPCs

### 9.5 R and Rust Engine Integrations
- **Status:** Directory structure exists (`r-engine/`, `rust-engine/`) but no actual code
- These are referenced by the multi-language architecture but not yet built

---

## 10. Areas Needing Improvement

### High Priority

| Area | Issue | Recommendation | Status |
|---|---|---|---|
| **Empty observability** | No telemetry/distributed tracing | Implement OpenTelemetry tracing or remove the placeholder | Pending |
| **CI/CD duplication** | Both ci-cd.yml and ci.yml active | Consolidate into single workflow | **FIXED** |
| **In-memory state across restarts** | AutoML jobs, drift detectors, experiments lost on restart | Implement Redis or SQL-backed persistence | **FIXED** — Redis for jobs/drift, PostgreSQL for model registry |
| **Documentation accuracy** | FILE_INDEX.md, DIRECTORY_STRUCTURE.md out of date | Audit and sync all documentation with actual codebase | Pending |
| **CI/CD duplication** | Both ci-cd.yml and ci.yml active | Consolidate into single workflow, remove the older one | **FIXED** |

### Medium Priority

| Area | Issue | Recommendation | Status |
|---|---|---|---|
| **Test coverage** | Only 7 test files, limited coverage | Expand test coverage, especially for automl, explainability, drift | Pending |
| **Error handling** | `cli.py` uses threads + progress bars for AutoML | Fragile; use proper async patterns instead | Pending |
| **Unused imports** | `cli.py` had unused `import asyncio` | Clean up imports | **FIXED** |

### Low Priority

| Area | Issue | Recommendation |
|---|---|---|
| **C#/Rust bridges** | Stubs only | Implement or remove bridge interfaces |
| **gRPC server** | Proto defined, no server | Implement gRPC server or remove proto files |
| **Hardcoded API URL** | `cli.py:22` — `API_BASE = "http://localhost:8000"` | Make configurable via env var or CLI option |
| **Static CSS in HTML UI** | `management_panel.html` has inline CSS | Extract to external stylesheet |
| **Environment separation** | Config class exists but env-specific overrides are minimal | Strengthen Dev/Staging/Prod configuration separation |

---

## 11. Testing Strategy

**Location:** `tests/`

**Test framework:** pytest with auto-asyncio mode

**Test files (6):**

| File | Tests | Coverage |
|---|---|---|
| `conftest.py` | Fixtures: `sample_data`, `temp_data_dir`, `mock_model` | Shared test resources |
| `test_api.py` | 7 tests | FastAPI endpoints via TestClient |
| `test_data.py` | 7 tests | DataLoader, Preprocessor |
| `test_features.py` | 3 tests | FeatureEngineer, FeatureSelector |
| `test_models.py` | 7 tests | BaseModel, RF, GB, registry save/load |
| `test_performance.py` | 8 test classes (~15 tests) | Profiling, caching, batch, memory |

**Test markers:**
- `benchmark` — benchmark tests
- `integration` — integration tests
- `slow` — slow-running tests

**Coverage tool:** pytest-cov with HTML report generation

**Known gap:** No tests for:
- AutoML engine
- Explainability engine
- Drift detection
- v2 API endpoints
- CLI commands
- Multi-language components
- Celery tasks

---

## 12. CI/CD Pipeline

**Location:** `.github/workflows/`

### Single Workflow: `ci.yml`

**Triggers:** push to main/develop, PRs to main

**Sequential jobs (7 stages):**

| # | Job | Tools | Description |
|---|---|---|---|
| 1 | **lint** | flake8, black, isort | Code style + import order |
| 2 | **type-check** | mypy | Static type checking |
| 3 | **security-scan** | bandit | SAST security scan |
| 4 | **unit-tests** | pytest, pytest-cov | Unit + coverage (XML upload to Codecov) |
| 5 | **integration-tests** | pytest + Redis service | Service health + API smoke test via Docker |
| 6 | **docker-build** | Docker Buildx | Build image + verify it starts (main only) |
| 7 | **docker-push** | GHCR login + push | Push to GitHub Container Registry (main only) |

---

## 13. Security Posture

### Implemented

| Feature | Location | Description |
|---|---|---|
| API Key authentication | `src/api/auth.py`, `src/api/main.py` | Per-request X-API-Key validation |
| bcrypt key hashing | `src/security/auth.py` | API keys and passwords hashed via bcrypt (passlib) |
| JWT Bearer tokens | `src/security/auth.py` | HS256 access (60 min) + refresh (7 day) tokens |
| Login + Token Refresh | `src/api/main.py` | `POST /auth/login` and `POST /auth/refresh` |
| Dual-mode auth middleware | `src/api/main.py` | `AuthMiddleware` accepts both API key + Bearer JWT |
| Role-Based Access Control | `src/security/auth.py` | ADMIN, DATA_SCIENTIST, ANALYST, VIEWER roles |
| Rate Limiting | `src/security/auth.py`, `slowapi`, `limits` | Redis-backed sliding-window (shared across workers); slowapi for per-endpoint decorators; memory fallback when Redis unavailable |
| GZip compression | `src/api/main.py` | Minimum 1000 bytes |
| CORS middleware | `src/api/main.py` | Restricted origins via ALLOWED_ORIGINS env var |
| Nginx TLS termination | `nginx/nginx.conf`, `docker-compose.yml` | HTTP→HTTPS redirect, TLS 1.2/1.3, HSTS |
| Self-signed certs script | `scripts/generate-certs.ps1` | Local dev TLS setup |
| Non-root container user | `Dockerfile` | `aimran` user in runtime container |
| Environment-based config | `src/config.py` | Dev/Prod/Testing config separation |

### Missing / Weak

| Gap | Risk | Recommendation | Status |
|---|---|---|---|
| CORS was wide-open | CSRF in browser clients | Restrict `allow_origins` to specific domains | **FIXED** — env var controlled |
| `.env` may contain secrets | Secrets in repo | Remove from version control, use secret manager |
| Audit logging | `src/security/audit.py`, integrated in `main.py` | Structured JSON audit for all significant actions; ring buffer + API retrieval | **FIXED** |
| In-memory user store | Lost on restart | Migrate to PostgreSQL-backed user persistence | Pending |
| **In-memory model registry** | Models lost on restart | Now PostgreSQL-backed with `model_registry` table | **FIXED** |

---

## 14. Performance Characteristics

### Design Decisions

| Decision | Rationale |
|---|---|
| `ThreadPoolExecutor(max_workers=4)` | Matches typical CPU core count for ML inference |
| `GZipMiddleware(minimum_size=1000)` | Compresses large responses only |
| Async prediction + blocking executor | Combines FastAPI async I/O with CPU-bound sklearn inference |
| In-memory caches for drift/automl | Fast access during a session; production should use Redis |
| JSON-based experiment storage | Simple, portable; MLflow available for production scale |

### Expected Performance

| Operation | Expected Latency | Notes |
|---|---|---|
| Single prediction | 10-100ms | Depends on model complexity |
| Batch prediction (100) | 100-500ms | Parallel within executor |
| AutoML (20 trials, 60s budget) | 60-120s | Time-bounded, CPU intensive |
| Data profiling (10K rows) | 100-500ms | Depends on column count |
| Drift detection | 50-500ms | Depends on dataset size |
| File upload + parse | 50-500ms | CSV parsing in executor |
| WebSocket metrics push | Every 2 seconds | Configurable interval |

### Scalability Limitations

| Limitation | Details | Mitigation |
|---|---|---|
| In-memory model registry | Lost on restart; single-process only | Redis-backed registry |
| ThreadPoolExecutor (4 workers) | Bounded parallelism | Increase for high-throughput production |
| Single-process Uvicorn | Default config uses 1 worker | Run with multiple workers via uvicorn flag |
| No distributed processing | sklearn is single-node | Add Spark/Dask integration for large datasets |
| JSON file persistence | Not suitable for multi-instance | Migrate to PostgreSQL/Redis for all state |

---

## Appendix A: File Manifest

```
Root/
├── .env, .env.example              # Environment configuration
├── .github/workflows/ci.yml        # CI pipeline (7 stages)
├── pyproject.toml                   # PEP 621 build config (v0.3.0)
├── alembic.ini                     # Alembic migration config
├── requirements.txt                # Python dependencies
├── Dockerfile                      # Multi-stage Docker build
├── docker-compose.yml              # 9 services orchestration
├── Makefile                        # 25+ make targets
├── cli.py                          # Click CLI (27 commands)
├── worker.py                       # Celery async tasks
│
├── src/                            # Python core (15 modules)
│   ├── __init__.py                 # Package init (v0.3.0)
│   ├── config.py                   # Environment config
│   ├── utils.py                    # Data utilities
│   ├── db_cache.py                 # Database + Redis cache
│   ├── performance.py              # Profiling + optimization
│   ├── api/                        # FastAPI (main.py + v2.py + schemas + auth + rate_limiter)
│   ├── automl/                     # AutoML engine (30+ models)
│   ├── data/                       # Data layer (5 modules)
│   ├── experiments/                # Experiment tracking
│   ├── explainability/             # SHAP/PDP/LIME explainability + bias
│   ├── features/                   # Feature engineering + store
│   ├── models/                     # ML models (RF, GB, NN) + registry + store
│   ├── monitoring/                 # Performance monitoring + drift
│   ├── security/                   # Auth + RBAC + rate limiting
│   ├── storage/                    # Persistent backends (Redis + PostgreSQL)
│   │   ├── __init__.py             # Module exports
│   │   ├── redis_client.py         # Redis connection pool with fallback
│   │   ├── auto_jobs.py            # AutoML job state (Redis TTL 24h)
│   │   ├── drift_store.py          # Drift detector pickle store (Redis 7d)
│   │   └── model_registry_db.py    # SQLAlchemy ORM + CRUD for model_registry
│   ├── multi_language/             # Multi-service orchestration (10 files)
│   └── observability/              # EMPTY — placeholder
│
├── migrations/                     # Alembic DB migrations
│   ├── env.py                      # Alembic environment config
│   ├── script.py.mako              # Revision template
│   └── versions/
│       └── 0001_create_model_registry_table.py
│
├── ui/                             # User interfaces
│   ├── dashboard.py                # Streamlit multi-page (1097 lines)
│   └── management_panel.html       # HTML/JS dashboard (Bootstrap + Chart.js + Socket.IO)
│
├── tests/                          # 6 test files, ~40 tests
├── notebooks/                      # 2 Jupyter notebooks
├── pipelines/                      # DAG pipeline orchestrator + training pipeline
├── scripts/                        # Setup validation + system checks
├── proto/                          # gRPC service definitions (unused)
├── monitoring/prometheus.yml       # Prometheus scrape config
├── templates/SERVICE_TEMPLATES.py  # Deployment templates
├── docs/                           # Documentation (index.md + OPTIMIZATION.md)
├── data/                           # Runtime data (empty subdirs)
├── logs/                           # Runtime logs (empty subdirs)
│
├── AIMRAN Data Science Lab/        # C# .NET MAUI Blazor (7 projects)
│   ├── Core/                       # Domain models
│   ├── Engine/                     # C# AI/ML engine
│   ├── Engine.Tests/               # Engine tests
│   ├── Gateway/                    # API gateway
│   ├── ServiceTemplates/           # Service scaffolding
│   ├── Shared/                     # Shared library
│   ├── Web/                        # Blazor Hybrid UI
│   ├── ai-engine/                  # Python integration
│   ├── r-engine/                   # R integration
│   └── rust-engine/                # Rust integration
│
├── README.md                       # Project overview (104 lines)
├── CHANGELOG.md                    # v0.1.0 changelog
├── CONTRIBUTING.md                 # Contribution guide
├── QUICKSTART.md                   # 5-minute quick start
├── DIRECTORY_STRUCTURE.md          # Directory reference (needs update)
├── FILE_INDEX.md                   # File index (needs update — references non-existent files)
├── IMPLEMENTATION_COMPLETE.md      # 12-step implementation report
├── OPTIMIZATION_REPORT.md          # Performance optimization report
├── SYSTEM_SUMMARY.txt              # ASCII system diagram
├── LICENSE                         # MIT license
└── ARCHITECTURE.md                 # THIS DOCUMENT
```

---

## Appendix B: Environment Variables

| Variable | Default | Description |
|---|---|---|
| `DATABASE_URL` | — | PostgreSQL connection string |
| `REDIS_URL` | `redis://localhost:6379/0` | Redis connection string |
| `ENVIRONMENT` | `development` | Runtime environment |
| `API_KEYS` | — | Comma-separated valid API keys |
| `REQUIRE_API_KEY` | `false` | Enforce API key authentication |
| `MLFLOW_TRACKING_URI` | — | MLflow tracking server URL |
| `AWS_ACCESS_KEY_ID` | — | AWS credentials |
| `AWS_SECRET_ACCESS_KEY` | — | AWS credentials |
| `GCP_PROJECT_ID` | — | GCP project ID |
| `PYTHONUNBUFFERED` | `1` | Python stdout/stderr unbuffered |

---

## Appendix C: CLI Commands Reference

```
aimran-lab
├── version          Show platform version info
├── health           System health check (--quick for basic)
├── status           System status (--format json|table)
├── explore          Explore data files (--file, --rows)
├── serve            Start API server (--host, --port, --workers, --reload)
├── dashboard        Launch Streamlit dashboard
├── test             Run test suite (--test, --verbose, --coverage)
├── benchmark        Run performance benchmarks (--duration)
├── setup            Bootstrap environment
├── logs             View application logs (--tail, --follow)
├── automl           Run AutoML (--file, --target, --task, --budget, --trials)
├── train            Quick model training (--file, --model, --model-id, --task)
├── predict          Run prediction (--model-id, --features)
├── drift            Detect data drift (--reference, --current)
└── admin
    └── create-user  Create platform user (--username, --email, --role)
```
