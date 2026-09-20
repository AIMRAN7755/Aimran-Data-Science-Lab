# Aimran Data Science Lab - Documentation

## 📋 Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Getting Started](#getting-started)
3. [Project Structure](#project-structure)
4. [Core Components](#core-components)
5. [API Documentation](#api-documentation)
6. [Deployment Guide](#deployment-guide)
7. [Contributing Guide](#contributing-guide)

---

## Architecture Overview

The Aimran Data Science Lab follows a modular, scalable architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                    Data Science Lab                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Data Layer      Feature Layer      Model Layer   API Layer  │
│  ├─ Loaders      ├─ Engineering     ├─ Base       ├─ FastAPI │
│  ├─ Storage      ├─ Selection       ├─ Train      ├─ Routes  │
│  └─ Validators   └─ Transformers    └─ Predict    └─ Schemas │
│                                                               │
│  ┌──────────────────────────────────────────────────────────┤
│  │            MLOps & Monitoring Layer                       │
│  │  ├─ MLflow Tracking  ├─ DVC Version Control             │
│  │  ├─ Metrics          ├─ Alerts                          │
│  │  └─ Logging          └─ CI/CD                           │
│  └──────────────────────────────────────────────────────────┘
│
│  ┌──────────────────────────────────────────────────────────┤
│  │            Infrastructure Layer                           │
│  │  ├─ Docker           ├─ PostgreSQL                      │
│  │  ├─ Kubernetes       ├─ MLflow Server                   │
│  │  └─ GitHub Actions   └─ Jupyter Lab                     │
│  └──────────────────────────────────────────────────────────┘
│
└─────────────────────────────────────────────────────────────┘
```

---

## Getting Started

### Prerequisites
- Python 3.9+
- Docker & Docker Compose
- Git
- Make (optional, but recommended)

### Installation

```bash
# Clone repository
git clone https://github.com/your-org/aimran-data-science-lab.git
cd aimran-data-science-lab

# Copy environment template
cp .env.example .env

# Using Make (recommended)
make install
make docker-build
make docker-up

# OR Manual installation
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt
```

### Verify Installation

```bash
# Check API
curl http://localhost:8000/health

# Access Jupyter Lab
# Open browser: http://localhost:8888

# Access MLflow UI
# Open browser: http://localhost:5000
```

---

## Project Structure

```
aimran-data-science-lab/
├── data/                          # Data directory (ignored by git)
│   ├── raw/                       # Original data
│   ├── processed/                 # Processed datasets
│   └── interim/                   # Intermediate data
│
├── src/                           # Source code
│   ├── __init__.py
│   ├── config.py                  # Configuration management
│   ├── logger.py                  # Logging setup
│   ├── utils.py                   # Utilities
│   ├── data/
│   │   ├── loaders.py             # Data loading
│   │   ├── preprocessors.py       # Data cleaning
│   │   └── validators.py          # Data validation
│   ├── features/
│   │   ├── engineering.py         # Feature creation
│   │   └── selection.py           # Feature selection
│   ├── models/
│   │   ├── base_model.py          # Base model class
│   │   ├── classifiers.py         # Classification models
│   │   └── regressors.py          # Regression models
│   ├── api/
│   │   ├── main.py                # FastAPI app
│   │   ├── routes.py              # API routes
│   │   └── schemas.py             # Pydantic schemas
│   └── monitoring/
│       ├── metrics.py             # MLflow integration
│       └── alerts.py              # Alerting
│
├── notebooks/                     # Jupyter notebooks
│   ├── 01_exploratory_data_analysis.ipynb
│   ├── 02_feature_engineering.ipynb
│   └── 03_model_development.ipynb
│
├── pipelines/                     # Data & training pipelines
│   ├── etl_pipeline.py
│   ├── training_pipeline.py
│   └── inference_pipeline.py
│
├── tests/                         # Test suite
│   ├── conftest.py                # Pytest config
│   ├── test_data.py
│   ├── test_features.py
│   ├── test_models.py
│   └── test_api.py
│
├── docs/                          # Documentation
│   ├── architecture.md
│   ├── api.md
│   └── deployment.md
│
├── config/                        # Configuration files
│   ├── dev.yaml
│   ├── prod.yaml
│   └── test.yaml
│
├── docker-compose.yml             # Docker services
├── Dockerfile                     # Container image
├── Makefile                       # Build automation
├── setup.py                       # Package setup
├── pyproject.toml                 # Project config
├── requirements.txt               # Dependencies
├── .env.example                   # Environment template
├── .gitignore                     # Git ignore rules
└── README.md                      # Main documentation
```

---

## Core Components

### 1. Data Loading & Preprocessing (`src/data/`)

```python
from src.data.loaders import DataLoader, Preprocessor

# Load data
loader = DataLoader(data_path="./data")
df = loader.load_dataset("sample.csv")

# Clean and preprocess
clean_df = Preprocessor.clean_data(df, drop_na=False)
scaled_df, scaler = Preprocessor.scale_features(clean_df)
```

### 2. Feature Engineering (`src/features/`)

```python
from src.features.engineering import FeatureEngineer, FeatureSelector

# Create features
engineer = FeatureEngineer()
df = engineer.create_polynomial_features(df, columns=['col1', 'col2'], degree=2)
df = engineer.create_interaction_features(df, columns=['col1', 'col2', 'col3'])

# Select top features
selector = FeatureSelector()
top_features = selector.select_top_features(X, y, n_features=10)
```

### 3. Model Development (`src/models/`)

```python
from src.models.base_model import BaseModel
from sklearn.ensemble import RandomForestClassifier

# Train model
model = RandomForestClassifier(n_estimators=100)
model.fit(X_train, y_train)

# Wrap in BaseModel
base_model = BaseModel(model_type="classifier")
base_model.model = model
base_model.is_trained = True

# Evaluate
metrics = base_model.evaluate(X_test, y_test)
print(metrics)

# Save/Load
base_model.save("models/model.pkl")
base_model.load("models/model.pkl")
```

### 4. REST API (`src/api/`)

The FastAPI application provides RESTful endpoints:

- `GET /` - Root endpoint
- `GET /health` - Health check
- `GET /models` - List models
- `POST /predict` - Make predictions
- `POST /upload` - Upload data files
- `GET /status` - System status

See [API Documentation](#api-documentation) for detailed endpoints.

### 5. Training Pipeline (`pipelines/`)

```python
from pipelines.training_pipeline import ModelTrainingPipeline

pipeline = ModelTrainingPipeline()
model, metrics = pipeline.run_pipeline(
	data_filepath="data/raw/sample.csv",
	model_type="random_forest",
	save_path="models/model.pkl"
)
```

---

## API Documentation

### Base URL
```
http://localhost:8000
```

### Endpoints

#### Health Check
```
GET /health
```
Response:
```json
{
  "status": "healthy",
  "version": "0.3.0",
  "environment": "development"
}
```

#### Make Prediction
```
POST /predict
Content-Type: application/json

{
  "features": [1.0, 2.0, 3.0, 4.0],
  "model_id": "default"
}
```
Response:
```json
{
  "prediction": 0.95,
  "probability": [0.05, 0.95],
  "model_id": "default"
}
```

#### Upload Dataset
```
POST /upload
Content-Type: multipart/form-data

file: <binary data>
```
Response:
```json
{
  "filename": "data.csv",
  "rows": 1000,
  "columns": ["col1", "col2", "col3"],
  "shape": [1000, 3]
}
```

#### List Models
```
GET /models
```
Response:
```json
{
  "models": [
	{
	  "id": "default",
	  "name": "Default Model",
	  "status": "active"
	}
  ]
}
```

---

## Deployment Guide

### Docker Deployment

```bash
# Build image
docker build -t aimran-lab:latest .

# Run container
docker run -p 8000:8000 -p 8888:8888 -v $(pwd)/data:/app/data aimran-lab:latest

# Using Docker Compose
docker-compose up -d
```

### Production Deployment

For production deployment:

1. **Update environment**: Set `ENVIRONMENT=production` in `.env`
2. **Configure logging**: Set `LOG_LEVEL=WARNING`
3. **Setup database**: Configure PostgreSQL connection
4. **Enable HTTPS**: Configure SSL certificates
5. **Deploy**: Use Kubernetes or cloud provider (AWS, GCP, Azure)

---

## Testing

```bash
# Run all tests
make test

# Run specific test file
pytest tests/test_models.py -v

# Run with coverage
pytest tests/ --cov=src --cov-report=html
```

---

## Monitoring & MLOps

### MLflow Tracking
```bash
# Start MLflow UI
mlflow ui --host 0.0.0.0 --port 5000

# Access at http://localhost:5000
```

### Data Versioning with DVC
```bash
# Track dataset
dvc add data/raw/dataset.csv

# Push to remote
dvc push

# Pull data
dvc pull
```

---

## Contributing

See [Contributing Guide](CONTRIBUTING.md) for development setup and workflow.

---

## Support & Issues

For issues and feature requests, please use the [GitHub Issues](https://github.com/your-org/aimran-data-science-lab/issues) page.

---

## License

MIT License - See LICENSE file for details.
