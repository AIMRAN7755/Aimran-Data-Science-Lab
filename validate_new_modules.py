"""Validation script for the new enterprise modules."""
import sys
sys.path.insert(0, '.')

print("=== 1. ExplainabilityEngine + BiasDetector ===")
import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier

from src.explainability import ExplainabilityEngine, BiasDetector, BiasMetrics

rng = np.random.default_rng(0)
X = pd.DataFrame(rng.random((200, 4)), columns=["a", "b", "c", "d"])
y = (X["a"] + X["b"] > 1.0).astype(int).values
clf = RandomForestClassifier(n_estimators=10, random_state=0).fit(X, y)

eng = ExplainabilityEngine()
feat_names = list(X.columns)

glob = eng.global_explanation(clf, "val_clf", feature_names=feat_names, top_n=3)
assert len(glob["top_features"]) == 3, "global_explanation: expected top_n=3"
assert glob["method"] == "native_importances"
print("  global_explanation:", [f["feature"] for f in glob["top_features"]])

local = eng.local_explanation(clf, "val_clf", X.iloc[0].tolist(), feature_names=feat_names)
assert local["feature_contributions"], "local_explanation: empty contributions"
print("  local_explanation: top =", local["top_features"][0])

disp = eng.explain(clf, "val_clf_g", X.iloc[0].tolist(), explain_type="global", feature_names=feat_names)
assert disp["type"] == "global"
print("  explain(global): method =", disp["method"])

df_bias = pd.DataFrame({"gender": ["M"] * 100 + ["F"] * 100})
y_true = rng.integers(0, 2, 200)
y_pred = rng.integers(0, 2, 200)
bd = BiasDetector()

metrics = bd.detect_bias(y_true, y_pred, "gender", df_bias)
assert isinstance(metrics, BiasMetrics)
print("  detect_bias: DPD =", round(metrics.demographic_parity, 4))

fr = bd.fairness_report(y_true, y_pred, "gender", df_bias)
assert "overall_fairness" in fr
print("  fairness_report:", fr["overall_fairness"])

w = bd.mitigate_bias_reweighting(df_bias, y_true, "gender")
assert len(w) == 200
print("  mitigate_bias_reweighting: range [%.3f, %.3f]" % (w.min(), w.max()))

print()
print("=== 2. DataDriftDetector + ModelPerformanceDriftDetector ===")
from src.monitoring import DataDriftDetector, ModelPerformanceDriftDetector, DriftReport

rng2 = np.random.default_rng(42)
df_ref = pd.DataFrame({
    "num1": rng2.normal(0, 1, 500),
    "num2": rng2.uniform(0, 10, 500),
    "cat1": rng2.choice(["A", "B", "C"], 500),
})
df_cur = pd.DataFrame({
    "num1": rng2.normal(3, 1, 200),   # deliberately drifted
    "num2": rng2.uniform(0, 10, 200),
    "cat1": rng2.choice(["A", "B", "C"], 200),
})

ddd = DataDriftDetector("val_drift")
ddd.fit_reference(df_ref)
report = ddd.detect_drift(df_cur)
assert report.columns_analyzed == 3, "expected 3 common columns"
assert report.drift_detected, "num1 was shifted 0->3; drift should fire"
print("  detect_drift: %d/%d cols drifted, score=%.3f" % (
    report.columns_drifted, report.columns_analyzed, report.overall_drift_score))
for r in report.column_results:
    print("    %s | %s | score=%.4f | drift=%s" % (
        r.column, r.test_used, r.drift_score, r.drift_detected))

d = report.to_dict()
assert "overall_drift_score" in d and "column_details" in d
print("  to_dict: OK (%d column_details)" % len(d["column_details"]))

mpd = ModelPerformanceDriftDetector(model_id="val_model", window=50, threshold=0.05)
for _ in range(60):
    mpd.record(0.92)          # stable historical baseline
for _ in range(50):
    mpd.record(0.80)          # degraded recent window
rep = mpd.report()
assert "degraded" in rep and "baseline" in rep
print("  performance drift: degraded=%s baseline=%s recent=%s" % (
    rep["degraded"], rep["baseline"], rep["recent_mean"]))
assert mpd.is_degraded(), "expected degradation after 0.92 -> 0.80 drop"

print()
print("=== 3. AuthManager + RateLimiter ===")
from src.security import AuthManager, RateLimiter, UserRole, User

mgr = AuthManager()
user = mgr.create_user("alice_v", "alicevx@test.com", UserRole.DATA_SCIENTIST)
assert isinstance(user, User)
raw_key = user._raw_api_key          # raw key is exposed only immediately after creation
assert len(raw_key) > 20
print("  create_user: username=%s role=%s" % (user.username, user.role.value))

auth_user = mgr.authenticate_api_key(raw_key)
assert auth_user is not None and auth_user.username == "alice_v"
print("  authenticate_api_key: OK")

# RBAC hierarchy: a data scientist clears data_scientist + viewer, but not admin
assert mgr.require_role(auth_user, UserRole.DATA_SCIENTIST) is True
assert mgr.require_role(auth_user, UserRole.VIEWER) is True
assert mgr.require_role(auth_user, UserRole.ADMIN) is False
print("  require_role: DS>=DS True, DS>=VIEWER True, DS>=ADMIN False  OK")

users = mgr.list_users()
assert any(u["username"] == "alice_v" for u in users)
print("  list_users: %d users  OK" % len(users))

# Deactivation revokes API-key authentication
assert mgr.deactivate_user(user.user_id) is True
assert mgr.authenticate_api_key(raw_key) is None
print("  deactivate_user: OK (auth after deactivation=None)")

limiter = RateLimiter(max_requests=5, window_seconds=60)
client = "rate-limit-test-client"
results = [limiter.is_allowed(client) for _ in range(7)]
assert results[:5] == [True] * 5
assert results[5] is False
print("  RateLimiter: first 5 allowed, 6th blocked  OK")

print()
print("=== ALL VALIDATIONS PASSED ===")
