# Directory structure for Aimran Data Science Lab

# GitHub Actions
.github/
  workflows/
    ci.yml                     # 7-job CI pipeline (lint, type-check, security, unit, integration, docker)

# Source code (Python)
src/
  __init__.py                  # Package entry — version 0.3.0
  config.py                    # Configuration management + logging setup
  logger.py                    # Logging helpers
  utils.py                     # DataLoader, DataPreprocessor, ModelEvaluator
  performance.py               # Profiling, caching, parallel processing
  db_cache.py                  # Database pooling + Redis caching
  health_check.py              # HealthManager — system diagnostics
  api/
    __init__.py
    main.py                    # FastAPI v1 application entry point
    v2.py                      # v2 API — auth-protected AutoML/drift endpoints
    auth.py                    # require_auth / require_analyst / require_admin
    dependencies.py            # Shared FastAPI dependencies
    rate_limiter.py            # Redis-backed rate limiting
    schemas.py                 # Pydantic request/response models
  automl/
    __init__.py
    engine.py                  # AutoML orchestration
    pipeline_engine.py         # End-to-end AutoML pipeline
  data/
    __init__.py
    loaders.py                 # Chunked/parallel data loading
    catalog.py                 # Data catalog
    quality.py                 # Data quality checks
    drift_detector.py          # Data drift detection
    versioning.py              # DVC-style dataset versioning (parquet snapshots)
  experiments/
    __init__.py
    manager.py                 # Experiment tracking (JSON on disk)
  explainability/
    __init__.py
    shap_engine.py             # SHAP explanations
    shap_explainer.py          # SHAP explainer integration
    bias_detector.py           # Bias detection
  features/
    __init__.py
    engineering.py             # Feature engineering at scale
    store.py                   # Versioned feature store (parquet snapshots)
  models/
    __init__.py
    base_model.py              # Base model class (train/eval/importance/save-load)
    random_forest.py           # RandomForest wrapper
    gradient_boosting.py       # Gradient boosting wrapper
    neural_network.py          # Neural network wrapper
    model_store.py             # In-process versioned model store + champion
    registry.py                # Model registry (PostgreSQL primary + memory cache)
  monitoring/
    __init__.py
    metrics.py                 # Metrics collection
    performance.py             # Performance monitoring middleware
    drift_detector.py          # Production drift detection
  multi_language/
    csharp_bridge.py           # C# engine bridge (HTTP)
    rust_bridge.py             # Rust engine bridge (HTTP)
    gateway.py                 # Multi-language gateway
    orchestrator.py            # Service orchestration
    service_manager.py         # Service lifecycle management
    service_registry.py        # Service registry
    ipc_broker.py              # Inter-process communication broker
    management_api.py          # Management API
    monitoring.py              # Multi-language monitoring
    health_checks.py           # Multi-language health checks
    grpc_server.py             # gRPC server (ServiceRegistry/Broker/Heartbeat)
    grpc/                      # Generated protobuf stubs (services_pb2*.py)
  observability/
    __init__.py
    tracer.py                  # OpenTelemetry tracer setup
    middleware.py              # ASGI OpenTelemetry middleware
    instrumentors.py           # SQLAlchemy/Redis/Celery instrumentors
  security/
    __init__.py
    auth.py                    # AuthManager (bcrypt+JWT), RateLimiter
    audit.py                   # Audit logging
  storage/
    __init__.py
    redis_client.py            # Redis client with in-memory fallback
    auto_jobs.py               # AutoML job store (Redis-backed)
    drift_store.py             # Drift detector store (Redis-backed)
    model_registry_db.py       # PostgreSQL model registry (SQLAlchemy)

# Database migrations
migrations/
  env.py                       # Alembic environment
  script.py.mako               # Alembic migration template
  versions/
    0001_create_model_registry_table.py

# Pipelines
pipelines/
  __init__.py
  orchestrator.py              # Pipeline orchestration
  training_pipeline.py         # End-to-end model training

# Tests
tests/
  __init__.py
  conftest.py                  # Pytest fixtures
  test_api.py                  # API tests (requires httpx2)
  test_data.py                 # Data loading tests
  test_features.py             # Feature engineering tests
  test_models.py               # Model tests
  test_performance.py          # Performance benchmarks

# UI
ui/
  dashboard.py                 # Streamlit dashboard
  management_panel.html        # Web management panel

# Templates
templates/
  SERVICE_TEMPLATES.py         # Service templates (health checks, periodic tasks)

# Protobuf definitions
proto/
  services.proto               # gRPC service definitions

# Infrastructure
nginx/
  nginx.conf                   # Reverse proxy configuration
  ssl/                         # TLS certificates
monitoring/
  prometheus.yml               # Prometheus scrape config

# Scripts
scripts/
  check_setup.py               # Setup validation
  validate_system.py           # System validation
  generate-certs.ps1           # TLS certificate generation

# Documentation
docs/
  index.md                     # Main documentation
  OPTIMIZATION.md              # Performance optimization guide

# Jupyter notebooks
notebooks/
  01_exploratory_data_analysis.ipynb
  enterprise_demo.ipynb

# Data directories (in .gitignore)
data/
  auth/                        # Auth persistence
  catalog/                     # Data catalog
  feature_store/               # Versioned feature groups
  versioned/                   # Versioned dataset snapshots

# Logs (in .gitignore)
logs/
  experiments/
  pipeline_runs/
  profiles/                    # Performance profiles

# .NET / MAUI solution (nested subproject)
AIMRAN Data Science Lab/
  AimranDataScienceLab.Core/           # Core services (data, ML, monitoring, security)
  AimranDataScienceLab.Engine/         # Engine (sqlite connection factory, options)
  AimranDataScienceLab.Engine.Tests/   # Engine unit tests
  AimranDataScienceLab.Gateway/        # Python/R/Rust engine clients
  AimranDataScienceLab.ServiceTemplates/# Role-based service templates + deployment
  AimranDataScienceLab.Shared/         # Blazor components, domain models, services
  AimranDataScienceLab.Web/            # Blazor web app
  ai-engine/                           # Python AI engine (FastAPI routers)
  r-engine/                            # R engine (plumber API)
  rust-engine/                         # Rust engine (axum/handlers)
  AIMRAN Data Science Lab.slnx         # MAUI shell app solution

# Root level files
.env                           # Local environment (not committed)
.env.example                   # Environment template
AIMRAN_Data_Science_Lab.sln    # Main solution
ARCHITECTURE.md                # Architecture documentation
Aimran_Lab_Enterprise_Research_Report.docx
CHANGELOG.md                   # Version history
CONTRIBUTING.md                # Developer contribution guide
DIRECTORY_STRUCTURE.md         # This file
Dockerfile                     # Docker container definition
FILE_INDEX.md                  # File & feature index
IMPLEMENTATION_COMPLETE.md     # Implementation status
IMPLEMENTATION_VERIFICATION.py # Implementation verification script
LICENSE                        # MIT License
Makefile                       # Build automation
OPTIMIZATION_REPORT.md         # Performance optimization report
QUICKSTART.md                  # 5-minute quick start guide
README.md                      # Main project overview
SYSTEM_SUMMARY.txt             # System summary
aimran_market_research_report.html
alembic.ini                    # Alembic configuration
build-output.txt
cli.py                         # Command-line interface
docker-compose.yml             # Docker services orchestration
pyproject.toml                 # Project configuration (setuptools)
requirements.txt               # All dependencies
validate_new_modules.py        # Module validation script
worker.py                      # Celery worker (requires celery[redis])