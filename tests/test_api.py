import pytest
from fastapi.testclient import TestClient
from src.api.main import app
from src.models.registry import register_model
from src.models.random_forest import RandomForestModel
import pandas as pd
import numpy as np


client = TestClient(app)


def test_root_endpoint():
    response = client.get("/")
    assert response.status_code == 200
    assert "name" in response.json()


def test_health_endpoint():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] in ("healthy", "degraded")


def test_status_endpoint():
    response = client.get("/status")
    assert response.status_code == 200
    assert "api" in response.json()


def test_list_models():
    response = client.get("/models")
    assert response.status_code == 200
    assert "models" in response.json()


def test_predict_without_model_returns_404():
    payload = {
        "features": [1.0, 2.0, 3.0],
        "model_id": "nonexistent"
    }
    response = client.post("/predict", json=payload)
    assert response.status_code == 404


def test_predict_with_registered_model():
    X = pd.DataFrame({'feat1': [1.0, 2.0], 'feat2': [3.0, 4.0], 'feat3': [5.0, 6.0]})
    y = pd.Series([0, 1])
    model = RandomForestModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)
    register_model("test_api_rf", model)

    payload = {
        "features": [1.0, 2.0, 3.0],
        "model_id": "test_api_rf"
    }
    response = client.post("/predict", json=payload)
    assert response.status_code == 200
    assert "prediction" in response.json()
    assert response.json()["model_id"] == "test_api_rf"


def test_predict_batch():
    X = pd.DataFrame({'feat1': [1.0, 2.0], 'feat2': [3.0, 4.0], 'feat3': [5.0, 6.0]})
    y = pd.Series([0, 1])
    model = RandomForestModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)
    register_model("test_batch_rf", model)

    payload = {
        "requests": [
            {"features": [1.0, 2.0, 3.0], "model_id": "test_batch_rf"},
            {"features": [4.0, 5.0, 6.0], "model_id": "test_batch_rf"},
        ]
    }
    response = client.post("/predict-batch", json=payload)
    assert response.status_code == 200
    assert response.json()["status"] == "completed"
    assert response.json()["count"] == 2


def test_register_model():
    payload = {
        "model_type": "random_forest",
        "task": "classification",
        "model_id": "api_test_model",
        "params": {"n_estimators": 10, "random_state": 42}
    }
    response = client.post("/models/train", json=payload)
    assert response.status_code == 200
    assert response.json()["model_id"] == "api_test_model"
    assert response.json()["status"] == "registered"


def test_metrics_endpoint():
    response = client.get("/metrics")
    assert response.status_code == 200
