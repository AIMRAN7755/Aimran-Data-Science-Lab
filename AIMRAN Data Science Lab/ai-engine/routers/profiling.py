"""Data profiling and outlier detection endpoints."""

import pandas as pd
import numpy as np
from fastapi import APIRouter, HTTPException
from schemas import ProfileRequest, ProfileResult, ColumnProfile, OutlierRequest, OutlierResult

router = APIRouter()


@router.post("/profile", response_model=ProfileResult)
async def profile_dataset(request: ProfileRequest):
    try:
        df = _load_dataframe(request.dataset_path)
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

    columns: list[ColumnProfile] = []
    for col in df.columns:
        series = df[col]
        null_count = int(series.isnull().sum())
        profile = ColumnProfile(
            name=col,
            data_type=str(series.dtype),
            null_count=null_count,
            null_percentage=null_count / len(df) * 100 if len(df) > 0 else 0,
            unique_count=int(series.nunique()),
        )
        if np.issubdtype(series.dtype, np.number):
            profile = profile.model_copy(update={
                "mean": float(series.mean()) if not series.empty else None,
                "std_dev": float(series.std()) if not series.empty else None,
                "min": float(series.min()) if not series.empty else None,
                "max": float(series.max()) if not series.empty else None,
            })
        columns.append(profile)

    # Simple quality score: (1 - avg_null_pct) * 100
    avg_null = np.mean([c.null_percentage for c in columns]) if columns else 0
    quality = max(0.0, (1.0 - avg_null / 100.0)) * 100.0

    return ProfileResult(
        row_count=len(df),
        column_count=len(df.columns),
        columns=columns,
        quality_score=round(quality, 2),
    )


@router.post("/outliers", response_model=OutlierResult)
async def detect_outliers(request: OutlierRequest):
    try:
        df = _load_dataframe(request.dataset_path)
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

    numeric_df = df.select_dtypes(include=[np.number])
    if numeric_df.empty:
        return OutlierResult(method=request.method, outlier_count=0, outlier_indices=[], outlier_scores=[])

    threshold = (request.parameters or {}).get("threshold", 3.0)

    if request.method in ("zscore", "z_score"):
        from scipy import stats
        z_scores = np.abs(stats.zscore(numeric_df.dropna()))
        outlier_mask = (z_scores > threshold).any(axis=1)
        indices = list(np.where(outlier_mask)[0].astype(int))
        scores = [float(z_scores[i].max()) for i in indices]
    elif request.method == "iqr":
        q1 = numeric_df.quantile(0.25)
        q3 = numeric_df.quantile(0.75)
        iqr = q3 - q1
        lower = q1 - 1.5 * iqr
        upper = q3 + 1.5 * iqr
        outlier_mask = ((numeric_df < lower) | (numeric_df > upper)).any(axis=1)
        indices = list(np.where(outlier_mask)[0].astype(int))
        scores = [1.0] * len(indices)
    else:
        indices, scores = [], []

    return OutlierResult(
        method=request.method,
        outlier_count=len(indices),
        outlier_indices=indices,
        outlier_scores=scores,
    )


def _load_dataframe(path: str) -> pd.DataFrame:
    """Load a dataset file into a DataFrame."""
    if path.endswith(".csv"):
        return pd.read_csv(path)
    elif path.endswith(".parquet"):
        return pd.read_parquet(path)
    elif path.endswith(".json"):
        return pd.read_json(path)
    elif path.endswith((".xlsx", ".xls")):
        return pd.read_excel(path)
    else:
        raise ValueError(f"Unsupported format: {path}")
