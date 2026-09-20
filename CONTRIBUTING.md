# Aimran Data Science Lab - Contributing Guide

## Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please read our Code of Conduct before contributing.

## Getting Started

### Development Setup

1. **Fork & Clone**
   ```bash
   git clone https://github.com/your-username/aimran-data-science-lab.git
   cd aimran-data-science-lab
   ```

2. **Create Development Environment**
   ```bash
   make install-dev
   ```

3. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Workflow

### 1. Code Style

We use Black and isort for code formatting:

```bash
# Format code
make format

# Check formatting
make lint
```

### 2. Testing

All new code must include tests:

```bash
# Run tests
make test

# Run specific test
pytest tests/test_models.py::test_base_model_evaluation -v
```

### 3. Documentation

- Add docstrings to all functions and classes
- Update README.md for user-facing changes
- Add type hints to function signatures

```python
def load_data(filepath: str, format: str = "csv") -> pd.DataFrame:
	"""
	Load data from file.

	Args:
		filepath: Path to data file
		format: File format (csv, parquet, etc.)

	Returns:
		Loaded DataFrame
	"""
	pass
```

### 4. Commit Messages

Use clear, descriptive commit messages:

```
feat: Add feature selection utility
fix: Handle missing values in preprocessor
docs: Update API documentation
test: Add tests for feature engineering
chore: Update dependencies
```

## Submitting Changes

1. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request**
   - Fill out PR template completely
   - Link related issues
   - Add description of changes

3. **Code Review**
   - Address reviewer comments
   - Ensure all CI checks pass
   - Squash commits if requested

4. **Merge**
   - Maintainer will merge when approved
   - Delete feature branch

## Guidelines

### For Data Processing
- Handle edge cases (empty data, missing values)
- Document assumptions clearly
- Include unit tests for preprocessing logic

### For Model Development
- Validate model performance on held-out test set
- Log all hyperparameters
- Track metrics with MLflow

### For API Development
- Use FastAPI with proper type hints
- Validate input with Pydantic
- Add proper error handling
- Document all endpoints

### For Infrastructure
- Use Docker for containerization
- Include health checks
- Add monitoring and logging

## Architecture Decisions

Major changes should include an Architecture Decision Record (ADR):

```markdown
# ADR-XXX: Title

## Context
Why are we making this decision?

## Decision
What are we doing?

## Consequences
What are the trade-offs?
```

## Running Locally

```bash
# Start all services
make docker-up

# Access Jupyter Lab
# http://localhost:8888

# Access API
# http://localhost:8000/docs

# Access MLflow
# http://localhost:5000
```

## Debugging

```bash
# Enable debug logging
export LOG_LEVEL=DEBUG
python -m src.api.main

# Run tests with verbose output
pytest tests/ -vv -s

# Check code coverage
pytest tests/ --cov=src --cov-report=html
open htmlcov/index.html
```

## Common Issues

### Virtual Environment
```bash
# Recreate environment
rm -rf venv/
python -m venv venv
source venv/bin/activate
make install-dev
```

### Docker Issues
```bash
# Clear Docker cache
docker system prune -a

# Rebuild images
make docker-build
```

### Dependency Conflicts
```bash
# Update requirements
pip install --upgrade -r requirements.txt
pip freeze > requirements.txt
```

## Performance Profiling

For performance-critical code:

```bash
# Profile memory usage
python -m memory_profiler script.py

# Profile execution time
python -m cProfile -s cumulative script.py | head -20
```

## Release Process

1. Update version in `src/__init__.py`
2. Update CHANGELOG.md
3. Create git tag
4. Push to main
5. GitHub Actions will build and deploy

## Questions?

- Check existing issues and discussions
- Open a new discussion for questions
- Read the main documentation at docs/index.md

Thank you for contributing! 🎉
