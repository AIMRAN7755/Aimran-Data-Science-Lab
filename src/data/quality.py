"""
Enterprise Data Quality module for Aimran Data Science Lab.

Provides rule-based quality checks, automatic profiling, and Markdown
report generation.  No external dependencies beyond pandas and numpy.
"""

from __future__ import annotations

import logging
import re
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Callable, Dict, List, Literal, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Type aliases
# ---------------------------------------------------------------------------

RuleType = Literal["not_null", "unique", "range", "regex", "custom"]
Severity = Literal["error", "warning"]


# ---------------------------------------------------------------------------
# Dataclasses
# ---------------------------------------------------------------------------

@dataclass
class QualityRule:
    """
    Specification for a single data-quality check.

    Attributes
    ----------
    name:
        Human-readable identifier for this rule.
    column:
        Target column (empty string means the whole DataFrame).
    rule_type:
        One of ``not_null``, ``unique``, ``range``, ``regex``, ``custom``.
    params:
        Rule-specific parameters (e.g. ``{"min": 0, "max": 100}`` for range).
    severity:
        ``"error"`` failures decrement the quality score;
        ``"warning"`` failures are surfaced but do not block.
    custom_fn:
        Required when *rule_type* is ``"custom"``.  Receives the DataFrame
        and returns ``(passed: bool, message: str)``.
    """

    name: str
    column: str
    rule_type: RuleType
    params: Dict[str, Any] = field(default_factory=dict)
    severity: Severity = "error"
    custom_fn: Optional[Callable[[pd.DataFrame], tuple[bool, str]]] = None


@dataclass
class QualityReport:
    """Outcome of running quality checks against a dataset."""

    dataset_name: str
    timestamp: str
    passed: int
    failed: int
    warnings: int
    total_checks: int
    rules_results: List[Dict[str, Any]]
    score: float          # 0–100; 100 means all error-severity checks passed


# ---------------------------------------------------------------------------
# Engine
# ---------------------------------------------------------------------------

class DataQualityEngine:
    """
    Execute a configurable set of quality rules against a DataFrame.

    Usage::

        engine = DataQualityEngine()
        engine.add_rule(QualityRule("no_nulls_age", "age", "not_null"))
        engine.add_rule(QualityRule("age_range", "age", "range", {"min": 0, "max": 120}))
        report = engine.run_checks(df, "customers")
        print(engine.generate_report_markdown(report))
    """

    def __init__(self) -> None:
        self._rules: List[QualityRule] = []

    # ------------------------------------------------------------------
    # Rule management
    # ------------------------------------------------------------------

    def add_rule(self, rule: QualityRule) -> None:
        """Register a quality rule."""
        self._rules.append(rule)
        logger.debug("Added rule '%s' (%s) on column '%s'.", rule.name, rule.rule_type, rule.column)

    # ------------------------------------------------------------------
    # Execution
    # ------------------------------------------------------------------

    def run_checks(self, df: pd.DataFrame, dataset_name: str) -> QualityReport:
        """
        Run all registered rules against *df*.

        Returns
        -------
        QualityReport
            Aggregated results with per-rule detail.
        """
        results: List[Dict[str, Any]] = []
        error_total = 0
        error_failed = 0

        for rule in self._rules:
            passed, message, detail = self._evaluate_rule(rule, df)

            if rule.severity == "error":
                error_total += 1
                if not passed:
                    error_failed += 1

            results.append({
                "rule": rule.name,
                "column": rule.column,
                "rule_type": rule.rule_type,
                "severity": rule.severity,
                "passed": passed,
                "message": message,
                "detail": detail,
            })

        passed_count = sum(1 for r in results if r["passed"])
        failed_count = len(results) - passed_count
        warning_count = sum(
            1 for r in results if not r["passed"] and r["severity"] == "warning"
        )

        score = (
            100.0 * (error_total - error_failed) / error_total
            if error_total > 0
            else 100.0
        )

        report = QualityReport(
            dataset_name=dataset_name,
            timestamp=datetime.utcnow().isoformat(),
            passed=passed_count,
            failed=failed_count,
            warnings=warning_count,
            total_checks=len(results),
            rules_results=results,
            score=round(score, 2),
        )

        logger.info(
            "Quality check for '%s': score=%.1f, passed=%d, failed=%d, warnings=%d.",
            dataset_name, report.score, report.passed, report.failed, report.warnings,
        )
        return report

    def auto_profile(self, df: pd.DataFrame, dataset_name: str = "auto") -> QualityReport:
        """
        Run a default battery of checks on every column without pre-configured rules.

        Checks applied
        --------------
        * Not-null check for every column
        * Duplicate row check (whole DataFrame)
        * Outlier check (z-score > 3) for every numeric column
        * Cardinality check for object columns
        """
        engine = DataQualityEngine()

        # Not-null for every column
        for col in df.columns:
            engine.add_rule(QualityRule(
                name=f"not_null_{col}",
                column=col,
                rule_type="not_null",
                severity="warning",
            ))

        # Duplicates across the full DataFrame
        engine.add_rule(QualityRule(
            name="no_duplicate_rows",
            column="",
            rule_type="unique",
            severity="warning",
        ))

        # Outlier check for numeric columns
        for col in df.select_dtypes(include=[np.number]).columns:
            engine.add_rule(QualityRule(
                name=f"outlier_{col}",
                column=col,
                rule_type="custom",
                severity="warning",
                custom_fn=self._make_outlier_fn(col),
            ))

        # Cardinality check for object columns
        for col in df.select_dtypes(include=["object", "category"]).columns:
            engine.add_rule(QualityRule(
                name=f"cardinality_{col}",
                column=col,
                rule_type="custom",
                severity="warning",
                custom_fn=self._make_cardinality_fn(col),
            ))

        return engine.run_checks(df, dataset_name)

    # ------------------------------------------------------------------
    # Built-in check implementations
    # ------------------------------------------------------------------

    @staticmethod
    def null_check(df: pd.DataFrame, column: str) -> tuple[bool, str, Dict]:
        """Fail if any value in *column* is null."""
        null_count = int(df[column].isnull().sum())
        passed = null_count == 0
        pct = null_count / len(df) * 100 if len(df) > 0 else 0.0
        return passed, f"{null_count} nulls ({pct:.1f}%)", {"null_count": null_count, "null_pct": pct}

    @staticmethod
    def duplicate_check(df: pd.DataFrame, column: str = "") -> tuple[bool, str, Dict]:
        """Fail if duplicate rows (or values in *column*) exist."""
        if column:
            dup_count = int(df[column].duplicated().sum())
            label = f"column '{column}'"
        else:
            dup_count = int(df.duplicated().sum())
            label = "all columns"
        passed = dup_count == 0
        return passed, f"{dup_count} duplicates across {label}", {"duplicate_count": dup_count}

    @staticmethod
    def range_check(df: pd.DataFrame, column: str, min_val: float, max_val: float) -> tuple[bool, str, Dict]:
        """Fail if any value in *column* is outside [*min_val*, *max_val*]."""
        series = df[column].dropna()
        out_of_range = int(((series < min_val) | (series > max_val)).sum())
        passed = out_of_range == 0
        return (
            passed,
            f"{out_of_range} values outside [{min_val}, {max_val}]",
            {"out_of_range_count": out_of_range, "min": float(series.min()), "max": float(series.max())},
        )

    @staticmethod
    def schema_check(df: pd.DataFrame, expected_dtypes: Dict[str, str]) -> tuple[bool, str, Dict]:
        """Fail if column dtypes do not match *expected_dtypes*."""
        mismatches: Dict[str, Dict] = {}
        for col, expected in expected_dtypes.items():
            if col not in df.columns:
                mismatches[col] = {"expected": expected, "actual": "MISSING"}
            else:
                actual = str(df[col].dtype)
                if not actual.startswith(expected):
                    mismatches[col] = {"expected": expected, "actual": actual}
        passed = len(mismatches) == 0
        return passed, f"{len(mismatches)} dtype mismatches", {"mismatches": mismatches}

    @staticmethod
    def outlier_check(df: pd.DataFrame, column: str, z_threshold: float = 3.0) -> tuple[bool, str, Dict]:
        """Fail if any value's z-score exceeds *z_threshold*."""
        series = df[column].dropna()
        if series.std() == 0:
            return True, "std=0, no outliers possible", {"outlier_count": 0}
        z_scores = np.abs((series - series.mean()) / series.std())
        outlier_count = int((z_scores > z_threshold).sum())
        passed = outlier_count == 0
        return (
            passed,
            f"{outlier_count} outliers (|z| > {z_threshold})",
            {"outlier_count": outlier_count, "max_z": float(z_scores.max())},
        )

    @staticmethod
    def cardinality_check(
        df: pd.DataFrame,
        column: str,
        max_unique_ratio: float = 0.95,
    ) -> tuple[bool, str, Dict]:
        """
        Warn if a categorical column has suspiciously high cardinality
        (ratio of unique values to total rows > *max_unique_ratio*).
        """
        if len(df) == 0:
            return True, "empty DataFrame", {"unique_count": 0, "unique_ratio": 0.0}
        unique_count = int(df[column].nunique())
        ratio = unique_count / len(df)
        passed = ratio <= max_unique_ratio
        return (
            passed,
            f"{unique_count} unique values ({ratio:.1%} of rows)",
            {"unique_count": unique_count, "unique_ratio": ratio},
        )

    @staticmethod
    def distribution_drift_check(
        reference_df: pd.DataFrame,
        current_df: pd.DataFrame,
        column: str,
        threshold: float = 0.1,
    ) -> tuple[bool, str, Dict]:
        """
        Detect mean drift between *reference_df* and *current_df* for *column*.

        Uses a simple relative-mean-shift metric; returns passed=False when the
        shift exceeds *threshold* (default 10 %).
        """
        ref_mean = float(reference_df[column].mean())
        cur_mean = float(current_df[column].mean())
        if ref_mean == 0:
            drift = abs(cur_mean)
        else:
            drift = abs((cur_mean - ref_mean) / ref_mean)
        passed = drift <= threshold
        return (
            passed,
            f"Mean drift {drift:.1%} (threshold {threshold:.1%})",
            {"reference_mean": ref_mean, "current_mean": cur_mean, "drift": drift},
        )

    # ------------------------------------------------------------------
    # Regex check
    # ------------------------------------------------------------------

    @staticmethod
    def regex_check(df: pd.DataFrame, column: str, pattern: str) -> tuple[bool, str, Dict]:
        """Fail if any non-null value in *column* does not match *pattern*."""
        series = df[column].dropna().astype(str)
        compiled = re.compile(pattern)
        mismatches = int((~series.str.match(compiled)).sum())
        passed = mismatches == 0
        return (
            passed,
            f"{mismatches} values do not match pattern '{pattern}'",
            {"mismatch_count": mismatches, "pattern": pattern},
        )

    # ------------------------------------------------------------------
    # Report generation
    # ------------------------------------------------------------------

    @staticmethod
    def generate_report_markdown(report: QualityReport) -> str:
        """Render *report* as a Markdown string."""
        lines: List[str] = [
            f"# Data Quality Report — {report.dataset_name}",
            "",
            f"**Timestamp:** {report.timestamp}",
            f"**Quality Score:** {report.score:.1f} / 100",
            "",
            "## Summary",
            "",
            f"| Metric         | Count |",
            f"|----------------|-------|",
            f"| Total checks   | {report.total_checks} |",
            f"| Passed         | {report.passed} |",
            f"| Failed (error) | {report.failed - report.warnings} |",
            f"| Warnings       | {report.warnings} |",
            "",
            "## Rule Results",
            "",
            "| Rule | Column | Type | Severity | Status | Message |",
            "|------|--------|------|----------|--------|---------|",
        ]

        for r in report.rules_results:
            status = "✅ PASS" if r["passed"] else ("⚠️ WARN" if r["severity"] == "warning" else "❌ FAIL")
            lines.append(
                f"| {r['rule']} | {r['column'] or '(all)'} | {r['rule_type']} "
                f"| {r['severity']} | {status} | {r['message']} |"
            )

        lines.append("")
        return "\n".join(lines)

    # ------------------------------------------------------------------
    # Internal evaluation dispatcher
    # ------------------------------------------------------------------

    def _evaluate_rule(
        self, rule: QualityRule, df: pd.DataFrame
    ) -> tuple[bool, str, Dict]:
        """Dispatch a single rule to the correct check function."""
        try:
            if rule.rule_type == "not_null":
                return self.null_check(df, rule.column)

            if rule.rule_type == "unique":
                return self.duplicate_check(df, rule.column)

            if rule.rule_type == "range":
                return self.range_check(
                    df, rule.column,
                    rule.params.get("min", float("-inf")),
                    rule.params.get("max", float("inf")),
                )

            if rule.rule_type == "regex":
                return self.regex_check(df, rule.column, rule.params["pattern"])

            if rule.rule_type == "custom":
                if rule.custom_fn is None:
                    raise ValueError(f"Rule '{rule.name}' has rule_type='custom' but no custom_fn.")
                result = rule.custom_fn(df)
                # custom_fn may return (passed, message) or (passed, message, detail)
                if len(result) == 2:
                    return result[0], result[1], {}
                return result[0], result[1], result[2]

            raise ValueError(f"Unknown rule_type: {rule.rule_type}")

        except Exception as exc:  # noqa: BLE001
            logger.warning("Rule '%s' raised an exception: %s", rule.name, exc)
            return False, f"Exception: {exc}", {}

    # ------------------------------------------------------------------
    # Helpers for auto_profile closures
    # ------------------------------------------------------------------

    @staticmethod
    def _make_outlier_fn(column: str) -> Callable[[pd.DataFrame], tuple[bool, str, Dict]]:
        def _fn(df: pd.DataFrame) -> tuple[bool, str, Dict]:
            return DataQualityEngine.outlier_check(df, column)
        return _fn

    @staticmethod
    def _make_cardinality_fn(column: str) -> Callable[[pd.DataFrame], tuple[bool, str, Dict]]:
        def _fn(df: pd.DataFrame) -> tuple[bool, str, Dict]:
            return DataQualityEngine.cardinality_check(df, column)
        return _fn


# ---------------------------------------------------------------------------
# Profiler
# ---------------------------------------------------------------------------

class DataProfiler:
    """
    Compute per-column summary statistics and detect drift between profiles.

    Usage::

        profiler = DataProfiler()
        profile = profiler.profile(df)
        diff = profiler.compare_profiles(profile_v1, profile_v2)
    """

    def profile(self, df: pd.DataFrame) -> Dict[str, Any]:
        """
        Return a profile dict with per-column statistics.

        Numeric columns get: dtype, null_pct, unique_count, mean, std, min,
        max, p25, p50, p75, skewness, kurtosis.

        Categorical/object columns get: dtype, null_pct, unique_count,
        top_values, sample_values.
        """
        total_rows = len(df)
        col_profiles: Dict[str, Any] = {}

        for col in df.columns:
            series = df[col]
            null_count = int(series.isnull().sum())
            null_pct = round(null_count / total_rows * 100, 2) if total_rows > 0 else 0.0
            unique_count = int(series.nunique(dropna=True))

            base = {
                "dtype": str(series.dtype),
                "null_pct": null_pct,
                "null_count": null_count,
                "unique_count": unique_count,
            }

            if pd.api.types.is_numeric_dtype(series):
                clean = series.dropna()
                base.update({
                    "mean": float(clean.mean()) if len(clean) else None,
                    "std": float(clean.std()) if len(clean) else None,
                    "min": float(clean.min()) if len(clean) else None,
                    "max": float(clean.max()) if len(clean) else None,
                    "p25": float(clean.quantile(0.25)) if len(clean) else None,
                    "p50": float(clean.quantile(0.50)) if len(clean) else None,
                    "p75": float(clean.quantile(0.75)) if len(clean) else None,
                    "skewness": float(clean.skew()) if len(clean) > 2 else None,
                    "kurtosis": float(clean.kurt()) if len(clean) > 3 else None,
                })
            else:
                top_values = (
                    series.value_counts(dropna=True).head(5).to_dict()
                )
                sample_values = series.dropna().head(5).tolist()
                base.update({
                    "top_values": {str(k): int(v) for k, v in top_values.items()},
                    "sample_values": [str(v) for v in sample_values],
                })

            col_profiles[col] = base

        return {
            "row_count": total_rows,
            "column_count": len(df.columns),
            "columns": col_profiles,
            "generated_at": datetime.utcnow().isoformat(),
        }

    def compare_profiles(
        self,
        profile1: Dict[str, Any],
        profile2: Dict[str, Any],
    ) -> Dict[str, Any]:
        """
        Highlight structural and statistical differences between two profiles.

        Returns
        -------
        dict with keys:

        * ``row_delta`` — change in row count
        * ``new_columns`` — columns present in profile2 but not profile1
        * ``removed_columns`` — columns present in profile1 but not profile2
        * ``column_changes`` — per-column stat diffs for shared numeric columns
        * ``null_pct_changes`` — columns where null_pct changed by >= 1 pp
        """
        cols1 = set(profile1.get("columns", {}).keys())
        cols2 = set(profile2.get("columns", {}).keys())

        column_changes: Dict[str, Any] = {}
        null_pct_changes: Dict[str, Any] = {}

        for col in cols1 & cols2:
            c1 = profile1["columns"][col]
            c2 = profile2["columns"][col]

            # Null-percentage drift
            np1 = c1.get("null_pct", 0.0)
            np2 = c2.get("null_pct", 0.0)
            if abs(np2 - np1) >= 1.0:
                null_pct_changes[col] = {"before": np1, "after": np2, "delta": round(np2 - np1, 2)}

            # Numeric stat drift
            if "mean" in c1 and "mean" in c2 and c1["mean"] is not None and c2["mean"] is not None:
                mean_delta = c2["mean"] - c1["mean"]
                std_delta = (c2.get("std") or 0.0) - (c1.get("std") or 0.0)
                column_changes[col] = {
                    "mean_delta": round(mean_delta, 4),
                    "std_delta": round(std_delta, 4),
                    "min_delta": round((c2.get("min") or 0.0) - (c1.get("min") or 0.0), 4),
                    "max_delta": round((c2.get("max") or 0.0) - (c1.get("max") or 0.0), 4),
                }

        return {
            "row_delta": profile2.get("row_count", 0) - profile1.get("row_count", 0),
            "new_columns": sorted(cols2 - cols1),
            "removed_columns": sorted(cols1 - cols2),
            "column_changes": column_changes,
            "null_pct_changes": null_pct_changes,
        }
