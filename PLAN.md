# Aimran Data Science Lab — Performance & Cleanup Plan

> Grounded in a 9-subsystem code audit (Aug 2026), not in the v1.0 research doc.
> The doc is **stale**: it lists work already shipped and prescribes heavy
> libraries for capabilities nothing in the platform consumes yet.
>
> **Operating rule:** make the existing code correct and fast first; use what is
> already installed before adding anything; add only small, justified deps; and
> explicitly defer the big ones until a real consumer exists.

---

## 0. Reframe — doc vs. reality

The audit contradicts the doc's "baseline reality check" on the load-bearing points:

| Doc v1.0 claim | Reality (verified) |
| --- | --- |
| `src/observability/` is empty | 4 working modules; HTTP tracing middleware wired into `main.py:209` |
| C#/Rust bridges raise `NotImplementedError` | Real HTTP+subprocess clients; **zero** `NotImplementedError` in the bridges |
| gRPC "defined but unused" | Full 3-service server, generated stubs, wired to `cli.py:258-262` (port 50051) |
| No RBAC / auth | Full JWT+API-key+bcrypt RBAC in `src/security/auth.py` |
| "KS + Chi2 + PSI" drift | Active detector does **KS + Chi2 only**; PSI/JS exist but are dead code |
| AutoML "30+ estimators" | 6 sklearn estimators per task, untuned |

The 14 `NotImplementedError` hits are protoc-generated abstract base classes (normal codegen), all overridden. So the doc's premise is wrong; the plan below is built from the code.

**Free leverage the audit found — deps already in `requirements.txt` but never imported by project code:** `optuna`, `xgboost`, `lightgbm`, `polars`, `torch`, `prometheus-client` (for exposition), `redis` (for the rate limiter). Most "improvements" cost no new dependency.

---

## 1. Real bugs — fix now, zero new deps  (P0/P1)

| # | Bug | File:line | Fix | Effort |
| --- | --- | --- | --- | --- |
| 1.1 | **gRPC import is broken**: gencode needs protobuf runtime 7.35.1, but `requirements.txt` pins `protobuf<7.0`. `import services_pb2` raises → `grpc-serve` and the `ExecuteFunction` path die. | `requirements.txt:93`; `src/multi_language/grpc/services_pb2.py:12-19` | Bump the pin to match the gencode, **or** regen stubs against installed protobuf (command already in `grpc_server.py:44-48`). | 0.25d |
| 1.2 | **`/metrics` unscrapeable**: endpoint returns a JSON dict, but `monitoring/prometheus.yml` scrapes it as Prometheus exposition text → every scrape fails to parse. `prometheus-client` is installed but unused. | `src/api/main.py:384-386`; `monitoring/prometheus.yml:6-9` | Serve `prometheus_client.generate_latest()` with `CONTENT_TYPE_LATEST` (or mount `make_asgi_app`). | 0.5d |
| 1.3 | **Forgeable JWTs / default creds** *(SECURITY — not optional)*: `SECRET_KEY` defaults to `'aimran-dev-secret-change-in-production'`, `ADMIN_PASSWORD` to `'admin'`, DB password to `'airflow'`. Any deploy that forgets an env var silently ships signable tokens. | `src/security/auth.py:53,170`; `src/config.py:21`; `docker-compose.yml:98,138` | Fail-fast (raise on startup) if `SECRET_KEY`/admin creds are unset outside dev; delete the plaintext fallbacks. | 1d |
| 1.4 | **v2 advertises missing routes**: `/v2/info` lists `POST /v2/explain/global` and `/local`, but neither route exists → documented endpoints 404. | `src/api/v2.py:48-49` | Implement the two routes (reuse existing `ExplainabilityEngine`) or drop them from `/v2/info`. | 0.5–1d |
| 1.5 | **Stale API in validation harness**: calls `ModelPerformanceDriftDetector(degradation_threshold=…)`; current signature is `(model_id, window, threshold)` → `TypeError`. | `validate_new_modules.py:98` vs `src/monitoring/drift_detector.py:343` | Fix the call site. | 0.25d |

---

## 2. Performance wins — using deps already installed (no new deps)

| # | Win | File:line | Why | Effort |
| --- | --- | --- | --- | --- |
| 2.1 | **Split the thread pool.** One `ThreadPoolExecutor(max_workers=4)` multiplexes `/predict` **and** training, AutoML, upload, explain. A long train call starves inference latency (head-of-line blocking). | `src/api/main.py:110,459,508,711,802` | Dedicated small inference pool vs. a separate job pool. Pure latency win, zero deps. | 2d |
| 2.2 | **Put XGBoost + LightGBM in the AutoML leaderboard** (and add one model-layer wrapper). Both are installed and never imported; they are the strongest tabular learners and are currently excluded. | `src/automl/engine.py:143-159`; `src/models/gradient_boosting.py:32-38` | Biggest accuracy/throughput gain, dependency already paid for. | 1d |
| 2.3 | **Wire Optuna into the candidate loop** for real HPO within the time budget, replacing the single-shot untuned CV. Adds a natural per-trial timeout, which also fixes the coarse advisory budget check (§ below). | `src/automl/engine.py:256-289` | Optuna is installed but unused. Real AutoML, no FLAML needed. | 3d |
| 2.4 | *(conditional)* **Polars lazy scan** for large CSV/Parquet in the loader, with a `.to_pandas()` bridge at the boundary. | `src/data/loaders.py:31-65` | Only worth it if large-file ingestion is an actual pain point — downstream is all pandas. Otherwise **skip**. | 2d |

> Note on ONNX: **deferred** (see §6). The real inference-latency problem is the shared pool (2.1), not the sklearn estimator. Add `onnxruntime` only if profiling shows the estimator is the bottleneck after 2.1.

---

## 3. Observability — wire the free half, defer the backend

The tracing code exists but exports nowhere and auto-instrumentation is never called.

| # | Action | File:line | Effort |
| --- | --- | --- | --- |
| 3.1 | Call `instrument_all()` at API startup and add a Celery `worker_process_init` hook (worker is fully untraced today). | `worker.py`; `src/observability/instrumentors.py:83` | 0.5d |
| 3.2 | Consolidate onto the richer `middleware.py`; delete the inline duplicate middleware + orphaned `tracer.py` path (imported by nothing). | `src/observability/__init__.py:26-82`, `middleware.py`, `tracer.py` | 0.5d |
| 3.3 | Structured JSON logging + `trace_id` injection at API/worker startup (`structlog` — small dep). | `src/api/main.py`, `worker.py` | 1d |
| 3.4 | **DEFER** otel-collector + Tempo/Loki/Mimir services. Spans currently go nowhere because `OTEL_EXPORTER_OTLP_ENDPOINT` is unset — that's fine until someone actually queries traces. Standing up a collector + `.env` endpoint is ~1d **when that day comes**. | `docker-compose.yml` | — |

Small deps this section needs: `structlog`, `opentelemetry-instrumentation-{fastapi,sqlalchemy,redis,celery}`.

---

## 4. Delete / consolidate — bloat removal

| # | Target | File | Action | Effort |
| --- | --- | --- | --- | --- |
| 4.1 | Orphaned near-duplicate of `engine.py` (already drifted: 5 vs 6 candidates, 300s vs 60s). Imported nowhere. | `src/automl/pipeline_engine.py` | Delete. | 0.25d |
| 4.2 | Two overlapping explainability engines; permutation importance lives only in the **non-exported** one. Stale third copy in `build/lib/`. | `src/explainability/shap_engine.py`, `shap_explainer.py`, `build/lib/**` | Consolidate to one; export the permutation path; delete `build/lib` copies. | 0.5d |
| 4.3 | Dead `compute_psi` / `compute_js_divergence` with zero call sites; meanwhile docstrings advertise "KS+PSI+JS" the active detector doesn't do. | `src/data/drift_detector.py:226-304`; `src/monitoring/__init__.py:30`; `ARCHITECTURE.md:248` | Wire PSI/JS into the detection path **or** delete + fix the false docstrings. | 1d |
| 4.4 | `schema_check` dtype enforcement exists but is unreachable via the public rule path. | `src/data/quality.py:25,260-271,399-424` | Expose as a `"schema"` rule type — **cheaper than adding Great Expectations**. | 0.5d |
| 4.5 | Dead `import grpc` in a pure in-memory registry. | `src/multi_language/gateway.py:7-8` | Remove. | trivial |

---

## 5. Test coverage — critical paths only, security first

Enterprise modules have **zero** tests, so reported coverage overstates real coverage. Prioritise by blast radius.

| # | Target | Why | Effort |
| --- | --- | --- | --- |
| 5.1 | Auth / RBAC / JWT / rate-limit | Security-critical, entirely untested — auth bypass could ship green. | 2d |
| 5.2 | v2 endpoints, AutoML, drift, explainability | Live enterprise surface with no tests; add smoke + one negative each. | 3d |
| 5.3 | gRPC import + servicer dispatch | Catches regressions like the protobuf break (1.1). | 1.5d |
| 5.4 | *(optional)* `hypothesis` property tests for loaders/preprocess | Small dep; add after 5.1–5.3, not before. | — |

> Skip Great Expectations for data contracts — the existing rule engine plus a light Pandera schema (or 4.4) covers it without a heavy datasource/context framework.

---

## 6. Deliberately NOT doing — the "without extra"

Each of these is a multi-day addition for a capability that currently has **no consumer**. Named trigger = when to revisit.

| Deferred item | Why skip now | Add when… |
| --- | --- | --- |
| **Feast** feature store | Home-grown parquet store works. | You actually serve time-series/entity features and observe train-serve skew. First reach: add `event_timestamp` + an as-of join in `store.py` (~2d custom) before pulling in Feast + Redis. |
| **Ray Serve** | Single FastAPI + split pools (2.1) handles current scale. | You need multi-replica, canary, or live A/B in production. |
| **Delta Lake** | Parquet+JSON versioning with rollback/lineage already works. | You need ACID multi-writer guarantees. |
| **HashiCorp Vault / `hvac`** | Env + fail-fast + no plaintext defaults (1.3) is enough. | Multi-env secret rotation becomes a requirement. |
| **AIF360** | 5 fairness metrics + a reweighting mitigator already exist. | If post-processing mitigation is mandated, add only Fairlearn `ThresholdOptimizer`; still skip AIF360's 70-metric surface. |
| **DiCE / LIME** | SHAP + native/permutation cover explanations. | Counterfactual recourse becomes a product/legal requirement (DiCE); LIME is redundant — likely never. |
| **TabPFN / pytorch-tabular / FTTransformer** | sklearn + XGBoost/LightGBM (2.2) cover tabular. | A benchmark on real data shows deep-tabular wins materially. `torch` stays dead weight until then. |
| **DuckDB, Evidently, NannyML, TPOT2, FLAML** | No consumer; Optuna (2.3) covers HPO; batch drift + reports exist. | A specific, measured need appears. |

**One exception — DO add:** `shap` is imported under `try/except` in two engines but is **not declared**, so SHAP silently no-ops in every clean install. Add `shap>=0.44` to activate paid-for-but-dead functionality. (0.1d)

---

## 7. Dependency delta (minimal)

- **Add (small, justified):** `shap` (activate dead code), `structlog` + `opentelemetry-instrumentation-*` (§3), `hypothesis` (§5.4), `pip-audit`/`safety` (CI-only supply-chain scan). `hvac` **only if** a secrets manager is mandated.
- **Use what's already installed and idle:** `optuna`, `xgboost`, `lightgbm`, `polars`, `prometheus-client`, `redis` (the `RateLimiter` accepts a `storage_uri` but ignores it → limits are per-process only; wire it to Redis so they survive restart/scale — `src/security/auth.py:336`).
- **Do NOT add now:** `feast`, `ray[serve]`, `deltalake`, `duckdb`, `evidently`, `nannyml`, `dice-ml`, `aif360`, `tabpfn`, `pytorch-tabular`, `great_expectations`, `flaml`.

---

## 8. Suggested order (fastest ROI first)

- **M0 (unblock + de-risk):** §1 real bugs + declare `shap` + §4 deletes. Low risk; fixes broken gRPC/metrics and closes the JWT hole.
- **M1 (perf):** §2.1 pool split, §2.2 XGBoost/LightGBM, §2.3 Optuna HPO, then §3 observability wiring.
- **M2 (confidence):** §5 tests, security first.
- **Everything in §6 stays deferred** until its named trigger fires.

CI supply-chain hardening (pip-audit/safety; make bandit + mypy gating instead of `|| true`) folds into M0/M1 as small, isolated changes.
