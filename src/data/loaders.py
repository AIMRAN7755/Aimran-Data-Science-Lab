"""Data loading module for Aimran Data Science Lab"""

import pandas as pd
import numpy as np
from typing import Optional, List, Dict, Tuple, Iterator, Callable
import logging
from pathlib import Path
from joblib import Parallel, delayed
import psutil

logger = logging.getLogger(__name__)


class DataLoader:
    """Advanced data loading and preprocessing with optimization"""

    def __init__(self, data_path: str = "./data", use_parallel: bool = True):
        self.data_path = data_path
        self.use_parallel = use_parallel
        self.n_jobs = psutil.cpu_count(logical=False) or 1

    def load_dataset(self, filename: str, use_chunking: bool = False, 
                    chunksize: int = 10000, **kwargs) -> pd.DataFrame:
        """Load dataset from file with optional chunking"""
        filepath = f"{self.data_path}/{filename}"

        if filename.endswith('.csv'):
            if use_chunking:
                return self._load_csv_chunked(filepath, chunksize, **kwargs)
            else:
                return pd.read_csv(filepath, **kwargs)
        elif filename.endswith('.parquet'):
            return pd.read_parquet(filepath, **kwargs)
        elif filename.endswith('.xlsx'):
            return pd.read_excel(filepath, **kwargs)
        else:
            raise ValueError(f"Unsupported file format: {filename}")

    def _load_csv_chunked(self, filepath: str, chunksize: int, **kwargs) -> pd.DataFrame:
        """Load CSV in chunks for memory efficiency"""
        import time
        start = time.perf_counter()

        chunks = []
        chunk_count = 0

        for chunk in pd.read_csv(filepath, chunksize=chunksize, **kwargs):
            chunks.append(chunk)
            chunk_count += 1

        df = pd.concat(chunks, ignore_index=True)
        elapsed = time.perf_counter() - start

        logger.info(f"Loaded {len(df)} rows in {elapsed:.2f}s ({chunk_count} chunks)")
        return df

    def load_generator(self, filename: str, chunksize: int = 10000, 
                      **kwargs) -> Iterator[pd.DataFrame]:
        """Load dataset as generator for streaming"""
        filepath = f"{self.data_path}/{filename}"

        if filename.endswith('.csv'):
            return pd.read_csv(filepath, chunksize=chunksize, **kwargs)
        else:
            raise ValueError("Generator loading only supports CSV files")

    def load_parallel(self, filenames: List[str], **kwargs) -> List[pd.DataFrame]:
        """Load multiple files in parallel"""
        if not self.use_parallel:
            return [self.load_dataset(f, **kwargs) for f in filenames]

        results = Parallel(n_jobs=self.n_jobs)(
            delayed(self.load_dataset)(f, **kwargs) for f in filenames
        )
        logger.info(f"Loaded {len(filenames)} files in parallel")
        return results

    def split_data(self, df: pd.DataFrame, test_size: float = 0.2, 
                   random_state: int = 42) -> Tuple[pd.DataFrame, pd.DataFrame]:
        """Split data into train and test sets"""
        from sklearn.model_selection import train_test_split

        train, test = train_test_split(df, test_size=test_size, random_state=random_state)
        logger.info(f"Split data: {len(train)} train, {len(test)} test")
        return train, test

    def get_data_info(self, df: pd.DataFrame) -> Dict:
        """Get comprehensive data information"""
        return {
            'shape': df.shape,
            'columns': df.columns.tolist(),
            'dtypes': df.dtypes.to_dict(),
            'missing': df.isnull().sum().to_dict(),
            'memory_usage': df.memory_usage(deep=True).sum() / 1024**2,  # MB
            'memory_optimizable': self._estimate_optimization_potential(df),
        }

    @staticmethod
    def _estimate_optimization_potential(df: pd.DataFrame) -> Dict:
        """Estimate memory optimization opportunities"""
        original_memory = df.memory_usage(deep=True).sum()

        # Estimate savings from downcast
        optimizations = {}
        for col in df.select_dtypes(include=[np.float64]).columns:
            if df[col].min() >= np.float32(-np.inf) and df[col].max() <= np.float32(np.inf):
                optimizations[col] = 'float32'

        for col in df.select_dtypes(include=[np.int64]).columns:
            if df[col].min() >= np.int32(np.iinfo(np.int32).min) and df[col].max() <= np.int32(np.iinfo(np.int32).max):
                optimizations[col] = 'int32'

        potential_savings = len(optimizations) * original_memory / (len(df.columns) * 2)

        return {
            'optimizable_columns': optimizations,
            'potential_savings_mb': potential_savings / 1024**2,
            'recommendation': 'Apply downcast' if potential_savings > 0 else 'Already optimized'
        }


class Preprocessor:
    """Data preprocessing operations with performance optimization"""

    @staticmethod
    def clean_data(df: pd.DataFrame, drop_na: bool = False, use_parallel: bool = False) -> pd.DataFrame:
        """Clean data"""
        import time
        start = time.perf_counter()

        df_clean = df.copy()

        # Remove duplicates
        initial_rows = len(df_clean)
        df_clean = df_clean.drop_duplicates()
        duplicates_removed = initial_rows - len(df_clean)

        # Handle missing values
        if drop_na:
            df_clean = df_clean.dropna()
        else:
            numeric_cols = df_clean.select_dtypes(include=[np.number]).columns.tolist()
            if numeric_cols:
                df_clean[numeric_cols] = df_clean[numeric_cols].fillna(df_clean[numeric_cols].mean())

        elapsed = time.perf_counter() - start
        logger.info(f"Data cleaned: {len(df_clean)} rows remaining (removed {duplicates_removed} duplicates) in {elapsed:.3f}s")
        return df_clean

    @staticmethod
    def scale_features(df: pd.DataFrame, columns: Optional[List[str]] = None, 
                      use_parallel: bool = False) -> Tuple[pd.DataFrame, 'StandardScaler']:
        """Scale numeric features"""
        from sklearn.preprocessing import StandardScaler
        import time

        start = time.perf_counter()
        df_scaled = df.copy()

        if columns is None:
            columns = df_scaled.select_dtypes(include=[np.number]).columns.tolist()

        scaler = StandardScaler()
        df_scaled[columns] = scaler.fit_transform(df_scaled[columns])

        elapsed = time.perf_counter() - start
        logger.info(f"Scaled {len(columns)} features in {elapsed:.3f}s")
        return df_scaled, scaler

    @staticmethod
    def encode_categorical(df: pd.DataFrame, columns: Optional[List[str]] = None,
                          use_parallel: bool = False) -> Tuple[pd.DataFrame, Dict]:
        """Encode categorical variables"""
        from sklearn.preprocessing import LabelEncoder
        import time

        start = time.perf_counter()
        df_encoded = df.copy()
        encoders = {}

        if columns is None:
            columns = df_encoded.select_dtypes(include=['object']).columns.tolist()

        if use_parallel and len(columns) > 1:
            # Parallel encoding
            def encode_col(col):
                le = LabelEncoder()
                return col, le, le.fit_transform(df_encoded[col].astype(str))

            n_jobs = min(psutil.cpu_count(logical=False) or 1, len(columns))
            results = Parallel(n_jobs=n_jobs)(
                delayed(encode_col)(col) for col in columns
            )

            for col, le, encoded_values in results:
                df_encoded[col] = encoded_values
                encoders[col] = le
        else:
            # Sequential encoding
            for col in columns:
                le = LabelEncoder()
                df_encoded[col] = le.fit_transform(df_encoded[col].astype(str))
                encoders[col] = le

        elapsed = time.perf_counter() - start
        logger.info(f"Encoded {len(columns)} categorical features in {elapsed:.3f}s")
        return df_encoded, encoders

    @staticmethod
    def optimize_dtypes(df: pd.DataFrame, aggressive: bool = False) -> pd.DataFrame:
        """Optimize DataFrame dtypes to reduce memory"""
        import time
        start = time.perf_counter()

        original_memory = df.memory_usage(deep=True).sum()
        df_optimized = df.copy()

        # Downcast integers
        int_cols = df_optimized.select_dtypes(include=['int64']).columns
        for col in int_cols:
            min_val = df_optimized[col].min()
            max_val = df_optimized[col].max()

            if min_val > np.iinfo(np.int32).min and max_val < np.iinfo(np.int32).max:
                df_optimized[col] = df_optimized[col].astype(np.int32)
            elif min_val > np.iinfo(np.int16).min and max_val < np.iinfo(np.int16).max:
                df_optimized[col] = df_optimized[col].astype(np.int16)

        # Downcast floats
        float_cols = df_optimized.select_dtypes(include=['float64']).columns
        for col in float_cols:
            if aggressive or df_optimized[col].min() > np.float32(np.finfo(np.float32).min):
                df_optimized[col] = df_optimized[col].astype(np.float32)

        # Convert to category for low-cardinality strings
        object_cols = df_optimized.select_dtypes(include=['object']).columns
        for col in object_cols:
            if df_optimized[col].nunique() / len(df_optimized) < 0.05:  # Less than 5% unique
                df_optimized[col] = df_optimized[col].astype('category')

        optimized_memory = df_optimized.memory_usage(deep=True).sum()
        reduction = (original_memory - optimized_memory) / original_memory * 100
        elapsed = time.perf_counter() - start

        logger.info(f"Optimized dtypes: {reduction:.1f}% memory reduction in {elapsed:.3f}s")
        return df_optimized
