"""Test configuration and fixtures"""

import os

# No OTLP collector runs during tests; disabling the SDK avoids per-span export retries.
os.environ.setdefault("OTEL_SDK_DISABLED", "true")
os.environ.setdefault("PYTHONIOENCODING", "utf-8")

import pytest
import numpy as np
import pandas as pd
import tempfile
from pathlib import Path


@pytest.fixture
def sample_data():
    """Sample dataset for testing"""
    np.random.seed(42)
    return pd.DataFrame({
        'feature_1': np.random.randn(100),
        'feature_2': np.random.randn(100),
        'feature_3': np.random.randn(100),
        'target': np.random.randint(0, 2, 100)
    })


@pytest.fixture
def temp_data_dir():
    """Temporary directory for test data"""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def mock_model():
    """Mock model for testing"""
    from sklearn.ensemble import RandomForestClassifier
    return RandomForestClassifier(n_estimators=10, random_state=42)
