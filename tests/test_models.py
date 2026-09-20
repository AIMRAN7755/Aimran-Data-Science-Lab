import pytest
import numpy as np
import pandas as pd
from src.models.base_model import BaseModel
from src.models.random_forest import RandomForestModel
from src.models.gradient_boosting import GradientBoostingModel
from src.models.registry import register_model, get_model, list_models, save_model_registry, load_model_registry
from sklearn.ensemble import RandomForestClassifier


def test_base_model_evaluation(sample_data, mock_model):
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    mock_model.fit(X, y)

    model = BaseModel(model_type="classifier")
    model.model = mock_model
    model.is_trained = True
    model.feature_names = X.columns.tolist()

    metrics = model.evaluate(X, y, task="classification")

    assert 'accuracy' in metrics
    assert 'precision' in metrics
    assert 'recall' in metrics
    assert 'f1' in metrics


def test_feature_importance(sample_data, mock_model):
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    mock_model.fit(X, y)

    model = BaseModel()
    model.model = mock_model
    model.is_trained = True
    model.feature_names = X.columns.tolist()

    importance = model.get_feature_importance()

    assert importance is not None
    assert len(importance) == len(X.columns)


def test_random_forest_trains_and_predicts(sample_data):
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    model = RandomForestModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)

    assert model.is_trained
    assert len(model.feature_names) == 3

    preds = model.predict(X)
    assert len(preds) == len(y)
    assert all(p in [0, 1] for p in preds)


def test_random_forest_regression():
    np.random.seed(42)
    X = pd.DataFrame({'feat1': np.random.randn(50), 'feat2': np.random.randn(50)})
    y = pd.Series(np.random.randn(50))

    model = RandomForestModel(task="regression", n_estimators=10, random_state=42)
    model.train(X, y)

    assert model.is_trained
    preds = model.predict(X)
    assert len(preds) == len(y)


def test_gradient_boosting_trains(sample_data):
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    model = GradientBoostingModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)

    assert model.is_trained
    preds = model.predict(X)
    assert len(preds) == len(y)


def test_model_registry_register_and_get(sample_data):
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    model = RandomForestModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)

    register_model("test_rf", model)

    assert "test_rf" in list_models()
    retrieved = get_model("test_rf")
    assert retrieved is not None
    preds = retrieved.predict(X)
    assert len(preds) == len(y)


def test_model_registry_save_load(sample_data):
    import tempfile
    import os

    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    model = RandomForestModel(task="classification", n_estimators=10, random_state=42)
    model.train(X, y)

    register_model("test_save", model)

    tmp_dir = tempfile.mkdtemp()
    registry_file = os.path.join(tmp_dir, "registry_index.json")
    save_model_registry(registry_file)

    from src.models.registry import MODEL_REGISTRY
    MODEL_REGISTRY.clear()
    assert len(list_models()) == 0

    load_model_registry(registry_file)
    loaded = get_model("test_save")
    assert loaded is not None
    assert loaded.is_trained

    import shutil
    shutil.rmtree(tmp_dir, ignore_errors=True)
