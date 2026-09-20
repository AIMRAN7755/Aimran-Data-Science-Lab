"""
AutoML Engine — automatically selects and evaluates multiple model families
using cross-validation and returns a ranked leaderboard plus the best fitted model.
"""

from __future__ import annotations

import logging
import time
import warnings
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd

warnings.filterwarnings("ignore")

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

@dataclass
class AutoMLConfig:
    """
    Configuration for an AutoML search.

    Attributes
    ----------
    task : str
        ``"classification"``, ``"regression"``, or ``"auto"`` (detect from target).
    time_budget : float
        Soft wall-clock budget in seconds (default 60 s).
    n_trials : int
        Maximum number of model evaluations (default 20).
    cv_folds : int
        Cross-validation folds (default 5).
    scoring_metric : str or None
        Override CV scoring metric; ``None`` picks a sensible default.
    verbose : bool
        Log each candidate result.
    """

    task: str = "classification"
    time_budget: float = 60.0
    n_trials: int = 20
    cv_folds: int = 5
    scoring_metric: Optional[str] = None
    verbose: bool = True


# ---------------------------------------------------------------------------
# Result types
# ---------------------------------------------------------------------------

@dataclass
class ModelCandidate:
    """Result of evaluating one model family."""

    name: str
    model: Any
    cv_score: float
    cv_std: float
    training_time_ms: float
    params: Dict[str, Any]


@dataclass
class AutoMLResult:
    """Outcome of a complete AutoML search."""

    best_model_name: str
    best_model: Any
    best_score: float
    task: str
    metric: str
    all_candidates: List[ModelCandidate]
    total_time_ms: float
    feature_names: List[str]
    recommendations: List[str]
    # Internal alias used by the API endpoint
    mean_score: float = 0.0
    model_name: str = ""

    def __post_init__(self):
        self.mean_score = self.best_score
        self.model_name = self.best_model_name

    def summary(self) -> Dict[str, Any]:
        sorted_candidates = sorted(self.all_candidates, key=lambda c: c.cv_score, reverse=True)
        return {
            "best_model": self.best_model_name,
            "best_score": round(self.best_score, 4),
            "task": self.task,
            "metric": self.metric,
            "candidates_evaluated": len(self.all_candidates),
            "total_time_ms": round(self.total_time_ms, 2),
            "leaderboard": [
                {
                    "rank": i + 1,
                    "model": c.name,
                    "cv_score": round(c.cv_score, 4),
                    "cv_std": round(c.cv_std, 4),
                    "training_time_ms": round(c.training_time_ms, 2),
                }
                for i, c in enumerate(sorted_candidates)
            ],
            "recommendations": self.recommendations,
        }


# ---------------------------------------------------------------------------
# Engine
# ---------------------------------------------------------------------------

class AutoMLEngine:
    """
    Enterprise AutoML engine.

    Automatically:
    - Detects the task type (classification / regression)
    - Evaluates multiple scikit-learn model families with cross-validation
    - Returns a ranked leaderboard + best fitted model ready for deployment
    - Provides actionable recommendations

    Parameters
    ----------
    config : AutoMLConfig or None
        Full configuration object.  When ``None``, call-time arguments
        (``cv_folds``, ``timeout_seconds``, ``verbose``) are used instead.
    cv_folds : int
        Cross-validation folds (ignored when *config* is provided).
    timeout_seconds : int
        Wall-clock budget (ignored when *config* is provided).
    verbose : bool
        Log per-candidate results.
    """

    # Candidate definitions: (display_name, ModelClass, default_params)
    _CLASSIFICATION_CANDIDATES = [
        ("RandomForest",        "sklearn.ensemble.RandomForestClassifier",       {"n_estimators": 100, "random_state": 42, "n_jobs": -1}),
        ("GradientBoosting",    "sklearn.ensemble.GradientBoostingClassifier",   {"n_estimators": 100, "random_state": 42}),
        ("LogisticRegression",  "sklearn.linear_model.LogisticRegression",       {"max_iter": 1000, "random_state": 42}),
        ("SVM",                 "sklearn.svm.SVC",                               {"probability": True, "random_state": 42}),
        ("KNN",                 "sklearn.neighbors.KNeighborsClassifier",        {"n_neighbors": 5}),
        ("ExtraTrees",          "sklearn.ensemble.ExtraTreesClassifier",         {"n_estimators": 100, "random_state": 42, "n_jobs": -1}),
        ("XGBoost",             "xgboost.XGBClassifier",                         {"n_estimators": 200, "random_state": 42, "n_jobs": -1, "tree_method": "hist", "eval_metric": "logloss", "verbosity": 0}),
        ("LightGBM",            "lightgbm.LGBMClassifier",                       {"n_estimators": 200, "random_state": 42, "n_jobs": -1, "verbose": -1}),
    ]

    _REGRESSION_CANDIDATES = [
        ("RandomForest",     "sklearn.ensemble.RandomForestRegressor",     {"n_estimators": 100, "random_state": 42, "n_jobs": -1}),
        ("GradientBoosting", "sklearn.ensemble.GradientBoostingRegressor", {"n_estimators": 100, "random_state": 42}),
        ("Ridge",            "sklearn.linear_model.Ridge",                 {"alpha": 1.0}),
        ("Lasso",            "sklearn.linear_model.Lasso",                 {"alpha": 1.0, "max_iter": 5000}),
        ("KNN",              "sklearn.neighbors.KNeighborsRegressor",      {"n_neighbors": 5}),
        ("ExtraTrees",       "sklearn.ensemble.ExtraTreesRegressor",       {"n_estimators": 100, "random_state": 42, "n_jobs": -1}),
        ("XGBoost",          "xgboost.XGBRegressor",                       {"n_estimators": 200, "random_state": 42, "n_jobs": -1, "tree_method": "hist", "verbosity": 0}),
        ("LightGBM",         "lightgbm.LGBMRegressor",                     {"n_estimators": 200, "random_state": 42, "n_jobs": -1, "verbose": -1}),
    ]

    def __init__(
        self,
        config: Optional[AutoMLConfig] = None,
        cv_folds: int = 5,
        timeout_seconds: int = 60,
        verbose: bool = True,
    ):
        if config is not None:
            self._config = config
        else:
            self._config = AutoMLConfig(
                cv_folds=cv_folds,
                time_budget=float(timeout_seconds),
                verbose=verbose,
            )

        self._best_result: Optional[AutoMLResult] = None
        self._leaderboard: Optional[pd.DataFrame] = None

    # ------------------------------------------------------------------
    # Public interface
    # ------------------------------------------------------------------

    def fit(
        self,
        X: pd.DataFrame,
        y: pd.Series,
        task: str = "auto",
        time_budget: Optional[float] = None,
        n_trials: Optional[int] = None,
    ) -> AutoMLResult:
        """
        Evaluate all candidates and return the best model.

        Parameters
        ----------
        X, y : pd.DataFrame, pd.Series
            Feature matrix and target vector.
        task : str
            ``"classification"``, ``"regression"``, or ``"auto"``.
        time_budget : float, optional
            Override the config time budget.
        n_trials : int, optional
            Override the config n_trials.

        Returns
        -------
        AutoMLResult
        """
        from sklearn.model_selection import cross_val_score, StratifiedKFold, KFold
        from sklearn.preprocessing import LabelEncoder, StandardScaler

        cfg = self._config
        if time_budget is not None:
            cfg.time_budget = float(time_budget)
        if n_trials is not None:
            cfg.n_trials = int(n_trials)

        # Task detection
        if task == "auto":
            task = self._detect_task(y)
        cfg.task = task

        logger.info(
            "AutoML starting — task=%s, features=%d, samples=%d, budget=%.0fs",
            task, X.shape[1], len(X), cfg.time_budget,
        )

        # Preprocessing
        X_proc = self._preprocess(X)
        scaler = StandardScaler()
        X_scaled = scaler.fit_transform(X_proc)

        # Encode target for classification
        y_enc = y.copy()
        if task == "classification" and y.dtype == object:
            le = LabelEncoder()
            y_enc = pd.Series(le.fit_transform(y))

        # Cross-validation strategy
        candidates_cfg = (
            self._CLASSIFICATION_CANDIDATES if task == "classification"
            else self._REGRESSION_CANDIDATES
        )
        cv_strategy = (
            StratifiedKFold(n_splits=cfg.cv_folds, shuffle=True, random_state=42)
            if task == "classification"
            else KFold(n_splits=cfg.cv_folds, shuffle=True, random_state=42)
        )
        scoring = cfg.scoring_metric or ("f1_weighted" if task == "classification" else "r2")

        evaluated: List[ModelCandidate] = []
        global_start = time.perf_counter()
        trial = 0

        for name, class_path, params in candidates_cfg:
            elapsed = time.perf_counter() - global_start
            if elapsed >= cfg.time_budget or trial >= cfg.n_trials:
                break
            trial += 1

            try:
                ModelClass = self._import_class(class_path)
                t0 = time.perf_counter()
                model = ModelClass(**params)
                scores = cross_val_score(
                    model, X_scaled, y_enc, cv=cv_strategy, scoring=scoring, n_jobs=1
                )
                elapsed_ms = (time.perf_counter() - t0) * 1000

                candidate = ModelCandidate(
                    name=name,
                    model=model,
                    cv_score=float(scores.mean()),
                    cv_std=float(scores.std()),
                    training_time_ms=elapsed_ms,
                    params=params,
                )
                evaluated.append(candidate)

                if cfg.verbose:
                    logger.info(
                        "  [%d] %-20s | %s: %.4f ± %.4f | %.0fms",
                        trial, name, scoring, scores.mean(), scores.std(), elapsed_ms,
                    )

            except Exception as exc:
                logger.warning("  [%d] %-20s | FAILED: %s", trial, name, exc)

        if not evaluated:
            raise RuntimeError("AutoML: no candidates could be evaluated")

        # Best model — refit on full data
        best = max(evaluated, key=lambda c: c.cv_score)
        best_model_class = self._import_class(
            next(cp for n, cp, _ in candidates_cfg if n == best.name)
        )
        final_model = best_model_class(**best.params)
        final_model.fit(X_scaled, y_enc)

        total_ms = (time.perf_counter() - global_start) * 1000
        recommendations = self._recommendations(best, evaluated, task, X)

        result = AutoMLResult(
            best_model_name=best.name,
            best_model=final_model,
            best_score=best.cv_score,
            task=task,
            metric=scoring,
            all_candidates=evaluated,
            total_time_ms=total_ms,
            feature_names=list(X.columns),
            recommendations=recommendations,
        )
        self._best_result = result
        self._leaderboard = self._build_leaderboard(evaluated)

        logger.info(
            "AutoML complete — best: %s | %s=%.4f | %.0fms",
            best.name, scoring, best.cv_score, total_ms,
        )
        return result

    def get_best_model(self) -> Optional[Any]:
        """Return the best fitted model from the last :meth:`fit` call."""
        return self._best_result.best_model if self._best_result else None

    def get_leaderboard(self) -> Optional[pd.DataFrame]:
        """Return the leaderboard DataFrame from the last :meth:`fit` call."""
        return self._leaderboard

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _detect_task(y: pd.Series) -> str:
        n_unique = y.nunique()
        if y.dtype == object or n_unique <= 20:
            return "classification"
        return "regression"

    @staticmethod
    def _preprocess(X: pd.DataFrame) -> pd.DataFrame:
        from sklearn.preprocessing import LabelEncoder
        X = X.copy()
        for col in X.select_dtypes(include=["object", "category"]).columns:
            le = LabelEncoder()
            X[col] = le.fit_transform(X[col].astype(str))
        X = X.fillna(X.median(numeric_only=True))
        return X

    @staticmethod
    def _import_class(class_path: str) -> Any:
        parts = class_path.rsplit(".", 1)
        module = __import__(parts[0], fromlist=[parts[1]])
        return getattr(module, parts[1])

    @staticmethod
    def _build_leaderboard(candidates: List[ModelCandidate]) -> pd.DataFrame:
        rows = sorted(
            [
                {
                    "rank": 0,
                    "model": c.name,
                    "cv_score": round(c.cv_score, 4),
                    "cv_std": round(c.cv_std, 4),
                    "training_time_ms": round(c.training_time_ms, 2),
                }
                for c in candidates
            ],
            key=lambda r: r["cv_score"],
            reverse=True,
        )
        for i, r in enumerate(rows):
            r["rank"] = i + 1
        return pd.DataFrame(rows)

    @staticmethod
    def _recommendations(
        best: ModelCandidate,
        all_candidates: List[ModelCandidate],
        task: str,
        X: pd.DataFrame,
    ) -> List[str]:
        recs = [f"Best model: {best.name} (CV score {best.cv_score:.4f})"]
        if best.cv_std > 0.05:
            recs.append(
                "High variance across folds — consider more data or stronger regularisation."
            )
        if len(X) < 1000:
            recs.append(
                "Small dataset (<1 000 rows) — results may not generalise; consider augmentation."
            )
        if X.shape[1] > 50:
            recs.append(
                "Many features detected — run feature selection to reduce dimensionality."
            )
        sorted_c = sorted(all_candidates, key=lambda c: c.cv_score, reverse=True)
        if len(sorted_c) > 1:
            gap = sorted_c[0].cv_score - sorted_c[1].cv_score
            if gap < 0.01:
                recs.append(
                    f"{sorted_c[1].name} is nearly as good — consider ensembling both models."
                )
        return recs
