"""
Bias & Fairness Detection
==========================
Detects and measures algorithmic bias and fairness violations in ML model
predictions across sensitive demographic groups.

All metrics follow the AIF360 / Fairlearn conceptual definitions but are
implemented from scratch using only stdlib + numpy + pandas.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Data Structures
# ---------------------------------------------------------------------------


@dataclass
class BiasMetrics:
    """
    Container for all computed fairness metrics for a single sensitive attribute.

    Attributes
    ----------
    group_col : name of the sensitive column (e.g. "gender", "race")
    privileged_group : value of group_col treated as the privileged group
    unprivileged_group : value treated as the unprivileged group
    demographic_parity : difference in positive prediction rates (ideal: 0.0)
    equal_opportunity : difference in true positive rates (ideal: 0.0)
    equalized_odds : average of |TPR diff| and |FPR diff| (ideal: 0.0)
    disparate_impact : ratio of positive rates unpriv/priv (ideal: 1.0)
    average_odds_difference : average of FPR diff and TPR diff (ideal: 0.0)
    """

    group_col: str
    privileged_group: Any
    unprivileged_group: Any
    demographic_parity: float = 0.0
    equal_opportunity: float = 0.0
    equalized_odds: float = 0.0
    disparate_impact: float = 1.0
    average_odds_difference: float = 0.0


# ---------------------------------------------------------------------------
# Helper utilities
# ---------------------------------------------------------------------------


def _group_mask(df: pd.DataFrame, col: str, value: Any) -> np.ndarray:
    return (df[col] == value).to_numpy()


def _positive_rate(y_pred: np.ndarray, mask: np.ndarray) -> float:
    group_preds = y_pred[mask]
    if len(group_preds) == 0:
        return 0.0
    return float(np.mean(group_preds == 1))


def _true_positive_rate(
    y_true: np.ndarray, y_pred: np.ndarray, mask: np.ndarray
) -> float:
    pos_mask = mask & (y_true == 1)
    if pos_mask.sum() == 0:
        return 0.0
    return float(np.mean(y_pred[pos_mask] == 1))


def _false_positive_rate(
    y_true: np.ndarray, y_pred: np.ndarray, mask: np.ndarray
) -> float:
    neg_mask = mask & (y_true == 0)
    if neg_mask.sum() == 0:
        return 0.0
    return float(np.mean(y_pred[neg_mask] == 1))


# ---------------------------------------------------------------------------
# Main Detector
# ---------------------------------------------------------------------------


class BiasDetector:
    """
    Comprehensive bias and fairness detector for binary classification models.

    Usage
    -----
    >>> detector = BiasDetector()
    >>> metrics = detector.detect_bias(y_true, y_pred, "gender", df)
    >>> report  = detector.fairness_report(y_true, y_pred, "gender", df)
    """

    def detect_bias(
        self,
        y_true: Any,
        y_pred: Any,
        sensitive_col: str,
        df: pd.DataFrame,
    ) -> BiasMetrics:
        """
        Detect bias in model predictions across a sensitive attribute.

        Automatically identifies the two most frequent groups in
        sensitive_col, designating the majority as privileged and the
        minority as unprivileged.

        Parameters
        ----------
        y_true : ground-truth binary labels (0/1)
        y_pred : model predicted binary labels (0/1)
        sensitive_col : column in df containing the sensitive attribute
        df : full DataFrame including the sensitive column

        Returns
        -------
        BiasMetrics
        """
        y_true = np.array(y_true)
        y_pred = np.array(y_pred)

        if sensitive_col not in df.columns:
            raise ValueError(f"Column '{sensitive_col}' not found in DataFrame.")

        value_counts = df[sensitive_col].value_counts()
        if len(value_counts) < 2:
            raise ValueError(
                f"Column '{sensitive_col}' must have at least 2 distinct groups."
            )

        privileged_group = value_counts.index[0]
        unprivileged_group = value_counts.index[1]

        logger.info(
            "Detecting bias on '%s': privileged=%s, unprivileged=%s",
            sensitive_col, privileged_group, unprivileged_group,
        )

        priv_mask = _group_mask(df, sensitive_col, privileged_group)
        unpriv_mask = _group_mask(df, sensitive_col, unprivileged_group)

        dp = self.demographic_parity_difference(y_pred, sensitive_col, df)
        eo = self.equal_opportunity_difference(y_true, y_pred, sensitive_col, df)
        di = self.disparate_impact_ratio(y_pred, sensitive_col, df)

        tpr_priv = _true_positive_rate(y_true, y_pred, priv_mask)
        tpr_unpriv = _true_positive_rate(y_true, y_pred, unpriv_mask)
        fpr_priv = _false_positive_rate(y_true, y_pred, priv_mask)
        fpr_unpriv = _false_positive_rate(y_true, y_pred, unpriv_mask)

        equalized_odds = float(
            (abs(tpr_unpriv - tpr_priv) + abs(fpr_unpriv - fpr_priv)) / 2
        )
        aod = float(((fpr_unpriv - fpr_priv) + (tpr_unpriv - tpr_priv)) / 2)

        return BiasMetrics(
            group_col=sensitive_col,
            privileged_group=privileged_group,
            unprivileged_group=unprivileged_group,
            demographic_parity=dp,
            equal_opportunity=eo,
            equalized_odds=equalized_odds,
            disparate_impact=di,
            average_odds_difference=aod,
        )

    def demographic_parity_difference(
        self,
        y_pred: Any,
        sensitive_col: str,
        df: pd.DataFrame,
    ) -> float:
        """
        DPD = P(Y_hat=1 | privileged) - P(Y_hat=1 | unprivileged).  Ideal: 0.0.

        Parameters
        ----------
        y_pred : predicted binary labels
        sensitive_col : sensitive attribute column in df
        df : DataFrame containing sensitive_col

        Returns
        -------
        float
        """
        y_pred = np.array(y_pred)
        value_counts = df[sensitive_col].value_counts()
        privileged = value_counts.index[0]
        unprivileged = value_counts.index[1]

        priv_mask = _group_mask(df, sensitive_col, privileged)
        unpriv_mask = _group_mask(df, sensitive_col, unprivileged)

        dpd = _positive_rate(y_pred, priv_mask) - _positive_rate(y_pred, unpriv_mask)
        logger.debug("Demographic parity difference: %.4f", dpd)
        return float(dpd)

    def equal_opportunity_difference(
        self,
        y_true: Any,
        y_pred: Any,
        sensitive_col: str,
        df: pd.DataFrame,
    ) -> float:
        """
        EOD = TPR(privileged) - TPR(unprivileged).  Ideal: 0.0.

        Parameters
        ----------
        y_true : ground-truth binary labels
        y_pred : predicted binary labels
        sensitive_col : sensitive column name
        df : DataFrame containing sensitive_col

        Returns
        -------
        float
        """
        y_true = np.array(y_true)
        y_pred = np.array(y_pred)
        value_counts = df[sensitive_col].value_counts()
        privileged = value_counts.index[0]
        unprivileged = value_counts.index[1]

        priv_mask = _group_mask(df, sensitive_col, privileged)
        unpriv_mask = _group_mask(df, sensitive_col, unprivileged)

        eod = (
            _true_positive_rate(y_true, y_pred, priv_mask)
            - _true_positive_rate(y_true, y_pred, unpriv_mask)
        )
        logger.debug("Equal opportunity difference: %.4f", eod)
        return float(eod)

    def disparate_impact_ratio(
        self,
        y_pred: Any,
        sensitive_col: str,
        df: pd.DataFrame,
    ) -> float:
        """
        DIR = P(Y_hat=1 | unprivileged) / P(Y_hat=1 | privileged).
        Ideal: 1.0.  Values < 0.8 flag potential adverse impact.

        Parameters
        ----------
        y_pred : predicted binary labels
        sensitive_col : sensitive column name
        df : DataFrame containing sensitive_col

        Returns
        -------
        float
        """
        y_pred = np.array(y_pred)
        value_counts = df[sensitive_col].value_counts()
        privileged = value_counts.index[0]
        unprivileged = value_counts.index[1]

        priv_mask = _group_mask(df, sensitive_col, privileged)
        unpriv_mask = _group_mask(df, sensitive_col, unprivileged)

        rate_priv = _positive_rate(y_pred, priv_mask)
        rate_unpriv = _positive_rate(y_pred, unpriv_mask)

        if rate_priv == 0:
            logger.warning("Privileged group has 0 positive prediction rate.")
            return 0.0

        dir_val = rate_unpriv / rate_priv
        logger.debug("Disparate impact ratio: %.4f", dir_val)
        return float(dir_val)

    def fairness_report(
        self,
        y_true: Any,
        y_pred: Any,
        sensitive_col: str,
        df: pd.DataFrame,
    ) -> Dict[str, Any]:
        """
        Generate a comprehensive fairness report with metrics and interpretations.

        Parameters
        ----------
        y_true : ground-truth binary labels
        y_pred : predicted binary labels
        sensitive_col : sensitive attribute column name
        df : DataFrame containing sensitive_col

        Returns
        -------
        dict  with all metrics, per-group breakdown, and plain-English
              interpretation strings for each metric.
        """
        logger.info("Generating fairness report for column '%s'", sensitive_col)
        m = self.detect_bias(y_true, y_pred, sensitive_col, df)

        def _interp_dp(val: float) -> str:
            if abs(val) < 0.05:
                return "PASS -- Demographic parity is within acceptable range (|diff| < 0.05)."
            direction = "privileged" if val > 0 else "unprivileged"
            return (
                f"FAIL -- Demographic parity difference is {val:+.3f}. "
                f"The {direction} group receives favourable outcomes more often."
            )

        def _interp_eo(val: float) -> str:
            if abs(val) < 0.05:
                return "PASS -- Equal opportunity difference is within acceptable range."
            return (
                f"FAIL -- Equal opportunity difference is {val:+.3f}. "
                "True positive rates differ substantially between groups."
            )

        def _interp_di(val: float) -> str:
            if val >= 0.8:
                return f"PASS -- Disparate impact ratio {val:.3f} >= 0.8 (80% rule satisfied)."
            return (
                f"FAIL -- Disparate impact ratio {val:.3f} < 0.8. "
                "Potential adverse impact on the unprivileged group."
            )

        def _interp_aod(val: float) -> str:
            if abs(val) < 0.05:
                return "PASS -- Average odds difference is within acceptable range."
            return (
                f"FAIL -- Average odds difference is {val:+.3f}. "
                "Both TPR and FPR differ between groups."
            )

        overall = (
            "FAIR"
            if (
                abs(m.demographic_parity) < 0.05
                and abs(m.equal_opportunity) < 0.05
                and m.disparate_impact >= 0.8
            )
            else "UNFAIR"
        )

        return {
            "sensitive_column": sensitive_col,
            "privileged_group": m.privileged_group,
            "unprivileged_group": m.unprivileged_group,
            "metrics": {
                "demographic_parity_difference": m.demographic_parity,
                "equal_opportunity_difference": m.equal_opportunity,
                "equalized_odds": m.equalized_odds,
                "disparate_impact_ratio": m.disparate_impact,
                "average_odds_difference": m.average_odds_difference,
            },
            "interpretations": {
                "demographic_parity": _interp_dp(m.demographic_parity),
                "equal_opportunity": _interp_eo(m.equal_opportunity),
                "disparate_impact": _interp_di(m.disparate_impact),
                "average_odds_difference": _interp_aod(m.average_odds_difference),
            },
            "overall_fairness": overall,
        }

    def mitigate_bias_reweighting(
        self,
        X: pd.DataFrame,
        y: Any,
        sensitive_col: str,
    ) -> np.ndarray:
        """
        Compute sample weights to mitigate bias via re-weighting (pre-processing).

        Each sample is assigned a weight equal to the inverse of the joint
        probability of its (group, label) combination, normalised so that
        weights sum to n.

        Parameters
        ----------
        X : feature DataFrame containing sensitive_col
        y : binary target array
        sensitive_col : sensitive attribute column in X

        Returns
        -------
        np.ndarray  1-D array of sample weights aligned with X
        """
        if sensitive_col not in X.columns:
            raise ValueError(f"Column '{sensitive_col}' not found in X.")

        y_arr = np.array(y)
        n = len(y_arr)
        groups = X[sensitive_col].values

        unique_groups = np.unique(groups)
        unique_labels = np.unique(y_arr)

        weights = np.ones(n, dtype=float)

        for grp in unique_groups:
            grp_mask = groups == grp
            p_group = grp_mask.sum() / n
            for lbl in unique_labels:
                lbl_mask = y_arr == lbl
                p_label = lbl_mask.sum() / n
                joint_mask = grp_mask & lbl_mask
                p_joint = joint_mask.sum() / n
                if p_joint > 0:
                    weight = (p_group * p_label) / p_joint
                    weights[joint_mask] = weight
                    logger.debug(
                        "Group=%s, Label=%s -> weight=%.4f (n=%d)",
                        grp, lbl, weight, joint_mask.sum(),
                    )

        weights = weights * (n / weights.sum())
        logger.info(
            "Re-weighting done. Weight range: [%.4f, %.4f]",
            weights.min(), weights.max(),
        )
        return weights
