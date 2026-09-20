"""Tests for feature engineering"""

import pytest
import pandas as pd
import numpy as np
from src.features.engineering import FeatureEngineer, FeatureSelector


def test_create_polynomial_features(sample_data):
    """Test polynomial feature generation"""
    engineer = FeatureEngineer()
    result = engineer.create_polynomial_features(sample_data, columns=['feature_1', 'feature_2'], degree=2)

    # Original columns + polynomial features should exist
    assert len(result.columns) > len(sample_data.columns)


def test_create_interaction_features(sample_data):
    """Test interaction feature generation"""
    engineer = FeatureEngineer()
    result = engineer.create_interaction_features(sample_data, columns=['feature_1', 'feature_2', 'feature_3'])

    # Should create 3 interaction features
    assert 'feature_1_x_feature_2' in result.columns
    assert 'feature_1_x_feature_3' in result.columns
    assert 'feature_2_x_feature_3' in result.columns


def test_select_top_features(sample_data):
    """Test feature selection"""
    X = sample_data.drop('target', axis=1)
    y = sample_data['target']

    selector = FeatureSelector()
    top_features = selector.select_top_features(X, y, n_features=2)

    assert len(top_features) == 2
    assert all(feat in X.columns for feat in top_features)
