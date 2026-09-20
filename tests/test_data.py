import pytest
import pandas as pd
import numpy as np
from pathlib import Path
from src.data.loaders import DataLoader, Preprocessor


def test_data_loader_info(sample_data):
    loader = DataLoader()
    info = loader.get_data_info(sample_data)

    assert info['shape'] == (100, 4)
    assert len(info['columns']) == 4
    assert 'target' in info['columns']


def test_data_split(sample_data):
    loader = DataLoader()
    train, test = loader.split_data(sample_data, test_size=0.2)

    assert len(train) + len(test) == len(sample_data)
    assert len(test) == 20


def test_clean_data(sample_data):
    sample_data_dirty = sample_data.copy()
    sample_data_dirty.loc[0, 'feature_1'] = np.nan

    cleaned = Preprocessor.clean_data(sample_data_dirty, drop_na=False)
    assert cleaned.isnull().sum().sum() == 0


def test_scale_features(sample_data):
    numeric_cols = sample_data.select_dtypes(include=[np.number]).columns.tolist()[:3]
    scaled, scaler = Preprocessor.scale_features(sample_data, columns=numeric_cols)

    assert scaled[numeric_cols].mean().abs().sum() < 0.1
    assert abs(scaled[numeric_cols].std().mean() - 1.0) < 0.1


def test_load_csv_from_file(tmp_path):
    df_original = pd.DataFrame({
        'a': [1, 2, 3],
        'b': [4.0, 5.0, 6.0],
        'c': ['x', 'y', 'z'],
    })
    filepath = tmp_path / "test.csv"
    df_original.to_csv(filepath, index=False)

    loaded = pd.read_csv(filepath)
    assert loaded.shape == (3, 3)
    assert list(loaded.columns) == ['a', 'b', 'c']


def test_chunked_loading_matches_normal(tmp_path):
    df = pd.DataFrame({
        'col1': np.random.randn(1000),
        'col2': np.random.randn(1000),
    })
    filepath = tmp_path / "chunk_test.csv"
    df.to_csv(filepath, index=False)

    normal = pd.read_csv(filepath)
    chunks = []
    for chunk in pd.read_csv(filepath, chunksize=200):
        chunks.append(chunk)
    chunked = pd.concat(chunks)

    pd.testing.assert_frame_equal(normal, chunked)


def test_optimize_dtypes_reduces_memory():
    df = pd.DataFrame({
        'big_int': np.random.randint(0, 100, 1000),
        'big_float': np.random.randn(1000),
    })
    df['big_int'] = df['big_int'].astype(np.int64)
    df['big_float'] = df['big_float'].astype(np.float64)

    original_mem = df.memory_usage(deep=True).sum()
    optimized = Preprocessor.optimize_dtypes(df)
    optimized_mem = optimized.memory_usage(deep=True).sum()

    assert optimized_mem <= original_mem
