"""AI-powered cleaning recommendation endpoints."""

import pandas as pd
import numpy as np
from fastapi import APIRouter, HTTPException
from schemas import CleaningRecommendRequest, CleaningRecommendation, RecommendedOperation

router = APIRouter()


@router.post("/recommend", response_model=CleaningRecommendation)
async def get_cleaning_recommendations(request: CleaningRecommendRequest):
    try:
        df = pd.read_csv(request.dataset_path) if request.dataset_path.endswith(".csv") else pd.read_parquet(request.dataset_path)
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

    operations: list[RecommendedOperation] = []
    total_cells = df.shape[0] * df.shape[1]
    issues_found = 0

    for col in df.columns:
        series = df[col]
        null_pct = series.isnull().mean() * 100

        # Recommend imputation for high-null columns
        if null_pct > 5:
            issues_found += series.isnull().sum()
            if np.issubdtype(series.dtype, np.number):
                operations.append(RecommendedOperation(
                    operation_type="impute_mean",
                    target_column=col,
                    reason=f"{null_pct:.1f}% missing values in numeric column",
                    confidence=min(0.95, null_pct / 100 + 0.5),
                    parameters={"strategy": "mean"},
                ))
            else:
                operations.append(RecommendedOperation(
                    operation_type="impute_mode",
                    target_column=col,
                    reason=f"{null_pct:.1f}% missing values in categorical column",
                    confidence=min(0.90, null_pct / 100 + 0.4),
                    parameters={"strategy": "mode"},
                ))

        # Detect potential duplicates in string columns
        if series.dtype == "object":
            stripped = series.dropna().str.strip().str.lower()
            if stripped.nunique() < series.dropna().nunique():
                operations.append(RecommendedOperation(
                    operation_type="normalize_text",
                    target_column=col,
                    reason="Inconsistent casing/whitespace detected",
                    confidence=0.80,
                    parameters={"trim": True, "lowercase": True},
                ))

    estimated_improvement = (issues_found / total_cells * 100) if total_cells > 0 else 0

    return CleaningRecommendation(
        operations=operations,
        estimated_quality_improvement=round(min(estimated_improvement, 30.0), 2),
    )
