.PHONY: help install install-dev serve dashboard worker \
        test test-cov lint format \
        docker-up docker-down docker-build docker-logs docker-ps \
        certs certs-prod \
        clean setup health validate benchmark migrate notebook

# ── Default target ────────────────────────────────────────────────────────────
.DEFAULT_GOAL := help

# ── Help ──────────────────────────────────────────────────────────────────────
help:
	@echo ""
	@echo "  Aimran Data Science Lab — Available Commands"
	@echo "  ============================================="
	@echo ""
	@echo "  Setup & Installation"
	@echo "    make install          Install all dependencies from requirements.txt"
	@echo "    make install-dev      Install deps + package in editable mode"
	@echo ""
	@echo "  Running Services"
	@echo "    make serve            Start FastAPI dev server (port 8000)"
	@echo "    make dashboard        Start Streamlit dashboard (port 8501)"
	@echo "    make worker           Start Celery worker"
	@echo "    make notebook         Start JupyterLab (port 8888)"
	@echo ""
	@echo "  Testing"
	@echo "    make test             Run tests with verbose output"
	@echo "    make test-cov         Run tests with HTML + terminal coverage report"
	@echo ""
	@echo "  Code Quality"
	@echo "    make lint             Run flake8 + black format check"
	@echo "    make format           Auto-format with black + isort"
	@echo ""
	@echo "  Docker"
	@echo "    make docker-up        Start all containers (detached) via Nginx TLS proxy"
	@echo "    make docker-down      Stop and remove containers"
	@echo "    make docker-build     Build Docker images"
	@echo "    make docker-logs      Tail logs from all containers"
	@echo "    make docker-ps        List running containers"
	@echo ""
	@echo "  TLS / HTTPS"
	@echo "    make certs            Generate self-signed TLS certs for local dev"
	@echo "    make certs-prod       Place Let's Encrypt certs in nginx/ssl/"
	@echo ""
	@echo "  Database"
	@echo "    make migrate          Run Alembic migrations (upgrade head)"
	@echo ""
	@echo "  Utilities"
	@echo "    make setup            Run CLI setup wizard"
	@echo "    make health           Run CLI health check"
	@echo "    make validate         Run full system validation script"
	@echo "    make benchmark        Run CLI benchmark suite"
	@echo "    make clean            Remove caches, compiled files, and build artifacts"
	@echo ""

# ── Setup & Installation ──────────────────────────────────────────────────────
install:
	pip install -r requirements.txt

install-dev:
	pip install -r requirements.txt
	pip install -e .

# ── Running Services ──────────────────────────────────────────────────────────
serve:
	uvicorn src.api.main:app --reload --host 0.0.0.0 --port 8000

dashboard:
	streamlit run ui/dashboard.py

worker:
	celery -A worker worker --loglevel=info

notebook:
	jupyter lab --ip=0.0.0.0

# ── Testing ───────────────────────────────────────────────────────────────────
test:
	pytest tests/ -v --tb=short

test-cov:
	pytest tests/ --cov=src --cov-report=html --cov-report=term-missing

# ── Code Quality ──────────────────────────────────────────────────────────────
lint:
	flake8 src/ tests/
	black --check src/ tests/

format:
	black src/ tests/ cli.py
	isort src/ tests/ cli.py

# ── Docker ────────────────────────────────────────────────────────────────────
docker-up:
	docker-compose up -d

docker-down:
	docker-compose down

docker-build:
	docker-compose build

docker-logs:
	docker-compose logs -f

# ── Database ──────────────────────────────────────────────────────────────────
migrate:
	alembic upgrade head

# ── Utilities ─────────────────────────────────────────────────────────────────
setup:
	python cli.py setup

health:
	python cli.py health

validate:
	python scripts/validate_system.py

benchmark:
	python cli.py benchmark

docker-ps:
	docker compose ps

# ── TLS / HTTPS ───────────────────────────────────────────────────────────────
certs:
	powershell -File scripts/generate-certs.ps1

certs-prod:
	@echo "Place your Let's Encrypt certificates in nginx/ssl/:"
	@echo "  nginx/ssl/cert.pem"
	@echo "  nginx/ssl/key.pem"
	@echo ""
	@echo "Or set up certbot:"
	@echo "  docker run --rm -v nginx/ssl:/etc/letsencrypt certbot/certbot certonly --webroot -d yourdomain.com"

clean:
	find . -type d -name __pycache__ -exec rm -rf {} + 2>/dev/null || true
	find . -type f -name "*.pyc" -delete 2>/dev/null || true
	find . -type f -name "*.pyo" -delete 2>/dev/null || true
	rm -rf build/ dist/ *.egg-info .eggs/
	rm -rf .pytest_cache .coverage htmlcov/
	rm -rf .mypy_cache .ruff_cache
