"""
Production-grade data drift detector.

Detects statistical differences between a reference (training) distribution
and a current (production) distribution using:
- Kolmogorov-Smirnov test  (numerical features)
- Chi-squared test          (categorical features)

Each detector instance is tied to one model ID and stores its last report
for retrieval via the API.
"""

from __future__ import annotations

import logging
import time
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)

try:
    from scipy import stats as _stats
    _SCIPY_AVAILABLE = True
except ImportError:
    _SCIPY_AVAILABLE = False
    logger.warning("scipy not installed — drift detection will use simplified heuristics")


# ---------------------------------------------------------------------------
# Result types
# ---------------------------------------------------------------------------

@dataclass
class ColumnDriftResult:
    column: str
    drift_detected: bool
    drift_score: float       # 0.0 (none) → 1.0 (severe)
    p_value: Optional[float]
    test_used: str
    reference_stats: Dict[str, Any]
    current_stats: Dict[str, Any]
    severity: str            # "none" | "low" | "medium" | "high"


@dataclass
class DriftReport:
    model_id: str
    timestamp: str
    columns_analyzed: int
    columns_drifted: int
    overall_drift_score: float
    drift_detected: bool
    column_results: List[ColumnDriftResult]
    recommendations: List[str]
    reference_size: int
    current_size: int
    computation_time_ms: float

    def to_dict(self) -> Dict[str, Any]:
        return {
            "model_id": self.model_id,
            "timestamp": self.timestamp,
            "overall_drift_score": round(self.overall_drift_score, 4),
            "drift_detected": self.drift_detected,
            "columns_analyzed": self.columns_analyzed,
            "columns_drifted": self.columns_drifted,
            "drifted_columns": [r.column for r in self.column_results if r.drift_detected],
            "column_details": [
                {
                    "column": r.column,
                    "drift_detected": r.drift_detected,
                    "drift_score": round(r.drift_score, 4),
                    "p_value": round(r.p_value, 4) if r.p_value is not None else None,
                    "test": r.test_used,
                    "severity": r.severity,
                }
                for r in self.column_results
            ],
            "recommendations": self.recommendations,
            "reference_size": self.reference_size,
            "current_size": self.current_size,
            "computation_time_ms": round(self.computation_time_ms, 2),
        }


# ---------------------------------------------------------------------------
# Detector
# ---------------------------------------------------------------------------

class DataDriftDetector:
    """
    Drift detector for a single model.

    Typical usage::

        detector = DataDriftDetector("my_model")
        detector.fit_reference(reference_df)
        report = detector.detect_drift(current_df)
        report.to_dict()

    Or from the API (JSON dicts)::

        report = detector.detect_from_dicts(ref_rows, cur_rows, feature_names)
    """

    def __init__(self, model_id: str = "default", significance_level: float = 0.05):
        self.model_id = model_id
        self.significance_level = significance_level
        self._reference_df: Optional[pd.DataFrame] = None
        self._reference_stats: Dict[str, Dict] = {}
        self._last_report: Optional[DriftReport] = None

    # ------------------------------------------------------------------
    # Setup
    # ------------------------------------------------------------------

    def fit_reference(self, df: pd.DataFrame) -> None:
        """Store the reference distribution."""
        self._reference_df = df.copy()
        self._reference_stats = self._compute_stats(df)
        logger.info(
            "DriftDetector '%s': reference set (%d rows, %d cols)",
            self.model_id, len(df), len(df.columns),
        )

    # ------------------------------------------------------------------
    # Detection
    # ------------------------------------------------------------------

    def detect_drift(self, current_df: pd.DataFrame) -> DriftReport:
        """Compare *current_df* against the stored reference."""
        if self._reference_df is None:
            raise RuntimeError("Call fit_reference() before detect_drift()")

        from datetime import datetime
        t0 = time.perf_counter()

        common_cols = [c for c in self._reference_df.columns if c in current_df.columns]
        results: List[ColumnDriftResult] = []

        for col in common_cols:
            result = self._test_column(col, self._reference_df[col], current_df[col])
            results.append(result)

        drifted = [r for r in results if r.drift_detected]
        overall_score = float(np.mean([r.drift_score for r in results])) if results else 0.0
        recs = self._recommendations(results, overall_score)
        elapsed_ms = (time.perf_counter() - t0) * 1000

        report = DriftReport(
            model_id=self.model_id,
            timestamp=datetime.now().isoformat(),
            columns_analyzed=len(results),
            columns_drifted=len(drifted),
            overall_drift_score=overall_score,
            drift_detected=len(drifted) > 0,
            column_results=results,
            recommendations=recs,
            reference_size=len(self._reference_df),
            current_size=len(current_df),
            computation_time_ms=elapsed_ms,
        )
        self._last_report = report
        logger.info(
            "DriftDetector '%s': %d/%d cols drifted (score=%.3f) in %.1fms",
            self.model_id, len(drifted), len(results), overall_score, elapsed_ms,
        )
        return report

    def detect_from_dicts(
        self,
        reference_rows: List[Dict[str, Any]],
        current_rows: List[Dict[str, Any]],
        feature_names: Optional[List[str]] = None,
    ) -> DriftReport:
        """Convenience wrapper accepting lists-of-dicts (from the API)."""
        ref_df = pd.DataFrame(reference_rows)
        cur_df = pd.DataFrame(current_rows)
        if feature_names:
            ref_df = ref_df[[c for c in feature_names if c in ref_df.columns]]
            cur_df = cur_df[[c for c in feature_names if c in cur_df.columns]]
        self.fit_reference(ref_df)
        return self.detect_drift(cur_df)

    def get_last_report(self) -> Optional[Dict[str, Any]]:
        """Return the last computed drift report as a dict, or ``None``."""
        return self._last_report.to_dict() if self._last_report else None

    # ------------------------------------------------------------------
    # Internals
    # ------------------------------------------------------------------

    @staticmethod
    def _compute_stats(df: pd.DataFrame) -> Dict[str, Dict]:
        stats: Dict[str, Dict] = {}
        for col in df.columns:
            if pd.api.types.is_numeric_dtype(df[col]):
                stats[col] = {
                    "type": "numeric",
                    "mean": float(df[col].mean()),
                    "std": float(df[col].std()),
                    "min": float(df[col].min()),
                    "max": float(df[col].max()),
                }
            else:
                stats[col] = {
                    "type": "categorical",
                    "n_unique": int(df[col].nunique()),
                    "top_values": df[col].value_counts(normalize=True).head(10).to_dict(),
                }
        return stats

    def _test_column(
        self, col: str, ref: pd.Series, cur: pd.Series
    ) -> ColumnDriftResult:
        ref_clean = ref.dropna()
        cur_clean = cur.dropna()
        ref_stats = self._reference_stats.get(col, {})

        if len(ref_clean) == 0 or len(cur_clean) == 0:
            return ColumnDriftResult(
                column=col, drift_detected=False, drift_score=0.0,
                p_value=None, test_used="skipped",
                reference_stats=ref_stats, current_stats={}, severity="none",
            )

        if ref_stats.get("type") == "numeric" or pd.api.types.is_numeric_dtype(ref_clean):
            return self._ks_test(col, ref_clean, cur_clean, ref_stats)
        return self._chi2_test(col, ref_clean, cur_clean, ref_stats)

    def _ks_test(self, col, ref, cur, ref_stats) -> ColumnDriftResult:
        if _SCIPY_AVAILABLE:
            ks_stat, p_value = _stats.ks_2samp(ref.values, cur.values)
        else:
            # Simplified heuristic: normalised mean difference
            ks_stat = abs(float(ref.mean()) - float(cur.mean())) / (float(ref.std()) + 1e-9)
            ks_stat = min(ks_stat, 1.0)
            p_value = 0.01 if ks_stat > 0.3 else 0.5

        drift_detected = p_value < self.significance_level
        severity = self._severity(float(ks_stat))
        cur_stats = {
            "mean": float(cur.mean()), "std": float(cur.std()),
            "min": float(cur.min()), "max": float(cur.max()),
        }
        return ColumnDriftResult(
            column=col, drift_detected=drift_detected,
            drift_score=float(ks_stat), p_value=float(p_value),
            test_used="kolmogorov-smirnov",
            reference_stats=ref_stats, current_stats=cur_stats,
            severity=severity,
        )

    def _chi2_test(self, col, ref, cur, ref_stats) -> ColumnDriftResult:
        all_cats = set(ref.unique()) | set(cur.unique())
        ref_counts = ref.value_counts().reindex(list(all_cats), fill_value=0).astype(float) + 1e-6
        cur_counts = cur.value_counts().reindex(list(all_cats), fill_value=0).astype(float) + 1e-6

        try:
            if _SCIPY_AVAILABLE:
                expected = ref_counts * (len(cur) / len(ref))
                chi2, p_value = _stats.chisquare(cur_counts, f_exp=expected)
            else:
                chi2 = float(
                    ((cur_counts - ref_counts) ** 2 / (ref_counts + 1e-9)).sum()
                )
                p_value = 0.01 if chi2 > len(all_cats) else 0.5

            drift_score = min(float(chi2) / max(len(all_cats) * 10, 1), 1.0)
            drift_detected = float(p_value) < self.significance_level
            severity = self._severity(drift_score)
        except Exception as exc:
            logger.debug("Chi2 test failed for '%s': %s", col, exc)
            drift_detected, drift_score, p_value, severity = False, 0.0, 1.0, "none"

        return ColumnDriftResult(
            column=col, drift_detected=drift_detected,
            drift_score=drift_score, p_value=float(p_value),
            test_used="chi-squared",
            reference_stats=ref_stats,
            current_stats={"n_unique": int(cur.nunique())},
            severity=severity,
        )

    @staticmethod
    def _severity(score: float) -> str:
        if score < 0.1:  return "none"
        if score < 0.2:  return "low"
        if score < 0.4:  return "medium"
        return "high"

    @staticmethod
    def _recommendations(results: List[ColumnDriftResult], overall_score: float) -> List[str]:  # noqa: C901
        drifted = [r for r in results if r.drift_detected]
        if not drifted:
            return ["No significant data drift detected. Model should perform as expected."]

        recs = []
        if overall_score > 0.4:
            recs.append("CRITICAL: Severe drift detected — immediate retraining recommended.")
        elif overall_score > 0.2:
            recs.append("WARNING: Moderate drift — schedule retraining soon.")
        else:
            recs.append("NOTICE: Minor drift — monitor closely.")

        high = [r for r in drifted if r.severity == "high"]
        if high:
            cols = ", ".join(r.column for r in high[:3])
            recs.append(f"High-severity drift in: {cols}. Investigate upstream data pipeline.")

        if len(drifted) > len(results) * 0.5:
            recs.append("More than 50% of features drifted — likely a data source change.")

        return recs


# ---------------------------------------------------------------------------
# Model-level performance drift
# ---------------------------------------------------------------------------

class ModelPerformanceDriftDetector:
    """
    Rolling-window model accuracy / score trend monitor.

    Records a time-series of model performance scores and raises an alert
    when a recent window falls significantly below the historical baseline.

    Parameters
    ----------
    model_id : str
        Identifier of the model being monitored.
    window : int
        Number of most-recent observations in the "current" window.
    threshold : float
        Fraction drop from baseline that triggers a degradation alert.
        e.g. ``0.05`` means a 5-percentage-point drop triggers an alert.
    """

    def __init__(self, model_id: str, window: int = 20, threshold: float = 0.05):
        self.model_id = model_id
        self.window = window
        self.threshold = threshold
        self._scores: list = []   # (timestamp, score)

    def record(self, score: float) -> None:
        """Record a new performance observation."""
        self._scores.append((time.time(), float(score)))

    @property
    def baseline(self) -> Optional[float]:
        """Mean score over all observations older than the current window."""
        baseline_obs = self._scores[: -self.window] if len(self._scores) > self.window else []
        if not baseline_obs:
            return None
        return float(np.mean([s for _, s in baseline_obs]))

    @property
    def recent_mean(self) -> Optional[float]:
        """Mean score over the most recent *window* observations."""
        recent = self._scores[-self.window :]
        if not recent:
            return None
        return float(np.mean([s for _, s in recent]))

    def is_degraded(self) -> bool:
        """Return True if recent performance has dropped beyond *threshold*."""
        b, r = self.baseline, self.recent_mean
        if b is None or r is None:
            return False
        return (b - r) > self.threshold

    def report(self) -> Dict[str, Any]:
        """Return a dict summary of the performance trend."""
        return {
            "model_id":     self.model_id,
            "observations": len(self._scores),
            "baseline":     round(self.baseline, 4) if self.baseline is not None else None,
            "recent_mean":  round(self.recent_mean, 4) if self.recent_mean is not None else None,
            "degraded":     self.is_degraded(),
            "threshold":    self.threshold,
        }
