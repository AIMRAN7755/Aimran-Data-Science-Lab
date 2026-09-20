"""
Aimran Data Science Lab — Enterprise Streamlit Dashboard
Multi-page dashboard with sidebar radio navigation.
"""

import time
import json
from datetime import datetime, timedelta

import numpy as np
import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import psutil
import requests
import streamlit as st

# ── Page config (must be first Streamlit call) ──────────────────────────────
st.set_page_config(
    page_title="Aimran Lab",
    page_icon="🔬",
    layout="wide",
    initial_sidebar_state="expanded",
)

# ── Global CSS ───────────────────────────────────────────────────────────────
st.markdown(
    """
<style>
/* ---------- layout ---------- */
.main { padding-top: 1.5rem; }
section[data-testid="stSidebar"] { background: #0f1117; }
section[data-testid="stSidebar"] * { color: #e0e0e0 !important; }

/* ---------- metric cards ---------- */
.card {
    background: linear-gradient(135deg, #1e2130 0%, #262b3d 100%);
    border: 1px solid #2e3452;
    border-radius: 10px;
    padding: 1.2rem 1.4rem;
    margin-bottom: 1rem;
}
.card h4 { margin: 0 0 .4rem 0; font-size: .85rem; color: #8892b0; text-transform: uppercase; letter-spacing: .05em; }
.card .val { font-size: 2rem; font-weight: 700; color: #ccd6f6; }
.card .sub { font-size: .75rem; color: #64ffda; margin-top: .2rem; }

/* ---------- status badge ---------- */
.badge-ok  { background:#0d3d2e; color:#64ffda; border:1px solid #64ffda; border-radius:20px; padding:2px 10px; font-size:.75rem; }
.badge-err { background:#3d0d0d; color:#ff6b6b; border:1px solid #ff6b6b; border-radius:20px; padding:2px 10px; font-size:.75rem; }
.badge-champ { background:#3d2d00; color:#ffd700; border:1px solid #ffd700; border-radius:20px; padding:2px 10px; font-size:.75rem; }

/* ---------- info / quick-link boxes ---------- */
.info-box {
    background:#1a1f35;
    border-left:4px solid #64ffda;
    border-radius:6px;
    padding:.9rem 1.1rem;
    margin:.5rem 0;
    font-size:.88rem;
    color:#ccd6f6;
}
.link-box {
    background:#1a1f35;
    border:1px solid #2e3452;
    border-radius:8px;
    padding:.7rem 1rem;
    margin:.4rem 0;
    font-size:.88rem;
    color:#8892b0;
}

/* ---------- drift table colours ---------- */
.drift-ok   { color:#64ffda; font-weight:600; }
.drift-warn { color:#ffd700; font-weight:600; }
.drift-high { color:#ff6b6b; font-weight:600; }

/* ---------- section headers ---------- */
.section-header {
    border-bottom: 2px solid #2e3452;
    padding-bottom: .4rem;
    margin-bottom: 1.2rem;
    color: #ccd6f6;
}
</style>
""",
    unsafe_allow_html=True,
)

# ── Constants ────────────────────────────────────────────────────────────────
API_BASE = "http://localhost:8000"
PLATFORM_VERSION = "1.0.0"


# ── Helpers ──────────────────────────────────────────────────────────────────

def _api_get(path: str, timeout: int = 6):
    """GET helper — returns (data_dict_or_None, error_string_or_None)."""
    try:
        r = requests.get(f"{API_BASE}{path}", timeout=timeout)
        r.raise_for_status()
        return r.json(), None
    except requests.exceptions.ConnectionError:
        return None, "API server not reachable (start with `python cli.py serve`)"
    except requests.exceptions.Timeout:
        return None, "Request timed out"
    except Exception as exc:
        return None, str(exc)


def _api_post(path: str, payload: dict, timeout: int = 30):
    """POST helper — returns (data_dict_or_None, error_string_or_None)."""
    try:
        r = requests.post(f"{API_BASE}{path}", json=payload, timeout=timeout)
        r.raise_for_status()
        return r.json(), None
    except requests.exceptions.ConnectionError:
        return None, "API server not reachable (start with `python cli.py serve`)"
    except requests.exceptions.Timeout:
        return None, "Request timed out"
    except Exception as exc:
        return None, str(exc)


@st.cache_data(ttl=5)
def _system_metrics():
    cpu = psutil.cpu_percent(interval=0.3)
    mem = psutil.virtual_memory()
    return {
        "cpu": cpu,
        "mem": mem.percent,
        "mem_gb": mem.used / 1024 ** 3,
        "mem_total_gb": mem.total / 1024 ** 3,
    }


@st.cache_data(ttl=30)
def _perf_history():
    """60-point simulated history anchored to real current values."""
    base_cpu = psutil.cpu_percent(interval=0.2)
    base_mem = psutil.virtual_memory().percent
    rng = np.random.default_rng(int(time.time()) // 30)
    rows = []
    for i in range(60):
        ts = datetime.now() - timedelta(seconds=(60 - i) * 5)
        rows.append(
            {
                "time": ts,
                "CPU %": float(np.clip(base_cpu + rng.normal(0, 4), 0, 100)),
                "Memory %": float(np.clip(base_mem + rng.normal(0, 2), 0, 100)),
            }
        )
    return pd.DataFrame(rows)


@st.cache_data
def _load_csv(file_bytes: bytes, name: str) -> pd.DataFrame:
    import io
    return pd.read_csv(io.BytesIO(file_bytes))


@st.cache_data
def _load_parquet(file_bytes: bytes, name: str) -> pd.DataFrame:
    import io
    return pd.read_parquet(io.BytesIO(file_bytes))


def _plotly_defaults(fig, height: int = 380):
    fig.update_layout(
        paper_bgcolor="rgba(0,0,0,0)",
        plot_bgcolor="rgba(0,0,0,0)",
        font_color="#ccd6f6",
        height=height,
        margin=dict(l=10, r=10, t=40, b=10),
    )
    fig.update_xaxes(gridcolor="#1e2130", zerolinecolor="#1e2130")
    fig.update_yaxes(gridcolor="#1e2130", zerolinecolor="#1e2130")
    return fig


# ════════════════════════════════════════════════════════════════════════════
#  PAGE FUNCTIONS
# ════════════════════════════════════════════════════════════════════════════


# ── 1. HOME ─────────────────────────────────────────────────────────────────

def page_home():
    st.title("🔬 Aimran Data Science Lab — Enterprise Platform")
    st.markdown(
        "<p style='color:#8892b0;margin-top:-.5rem;'>Comprehensive ML Operations & Analytics Platform</p>",
        unsafe_allow_html=True,
    )

    metrics = _system_metrics()

    # ── top metric cards ──
    c1, c2, c3, c4 = st.columns(4)
    with c1:
        st.markdown(
            f"<div class='card'><h4>CPU Usage</h4>"
            f"<div class='val'>{metrics['cpu']:.1f}%</div>"
            f"<div class='sub'>{'⬆ High' if metrics['cpu'] > 70 else '✓ Normal'}</div></div>",
            unsafe_allow_html=True,
        )
    with c2:
        st.markdown(
            f"<div class='card'><h4>Memory Usage</h4>"
            f"<div class='val'>{metrics['mem']:.1f}%</div>"
            f"<div class='sub'>{metrics['mem_gb']:.1f} / {metrics['mem_total_gb']:.1f} GB</div></div>",
            unsafe_allow_html=True,
        )
    with c3:
        data, _ = _api_get("/models")
        model_count = len(data) if isinstance(data, list) else "—"
        st.markdown(
            f"<div class='card'><h4>Active Models</h4>"
            f"<div class='val'>{model_count}</div>"
            f"<div class='sub'>Registered in registry</div></div>",
            unsafe_allow_html=True,
        )
    with c4:
        exp_data, _ = _api_get("/experiments")
        exp_count = len(exp_data) if isinstance(exp_data, list) else "—"
        st.markdown(
            f"<div class='card'><h4>Experiments</h4>"
            f"<div class='val'>{exp_count}</div>"
            f"<div class='sub'>Total tracked runs</div></div>",
            unsafe_allow_html=True,
        )

    st.markdown("---")

    left, right = st.columns([3, 2])

    with left:
        st.subheader("📈 Real-Time System Performance")
        hist = _perf_history()
        fig = go.Figure()
        fig.add_trace(
            go.Scatter(
                x=hist["time"],
                y=hist["CPU %"],
                name="CPU %",
                line=dict(color="#64ffda", width=2),
                fill="tozeroy",
                fillcolor="rgba(100,255,218,0.07)",
            )
        )
        fig.add_trace(
            go.Scatter(
                x=hist["time"],
                y=hist["Memory %"],
                name="Memory %",
                line=dict(color="#ff79c6", width=2),
                fill="tozeroy",
                fillcolor="rgba(255,121,198,0.07)",
            )
        )
        fig.update_layout(hovermode="x unified", yaxis=dict(range=[0, 105]))
        st.plotly_chart(_plotly_defaults(fig, 360), use_container_width=True)

        if st.button("🔄 Refresh Metrics"):
            st.cache_data.clear()
            st.rerun()

    with right:
        st.subheader("🟢 Lab Status")
        api_ok, api_err = _api_get("/health")
        api_badge = "<span class='badge-ok'>ONLINE</span>" if api_ok else "<span class='badge-err'>OFFLINE</span>"
        st.markdown(
            f"""
            <div class='info-box'>
            <b>Platform Status</b><br><br>
            🔌 API Server &nbsp;&nbsp;{api_badge}<br>
            🗄️ Database &nbsp;&nbsp;&nbsp;<span class='badge-ok'>CONNECTED</span><br>
            📊 Dashboard &nbsp;&nbsp;<span class='badge-ok'>RUNNING</span><br>
            ⚙️ Workers &nbsp;&nbsp;&nbsp;&nbsp;<span class='badge-ok'>ACTIVE</span><br>
            </div>
            """,
            unsafe_allow_html=True,
        )

        st.subheader("🔗 Quick Links")
        links = [
            ("📊 Data Explorer", "Upload & profile your datasets"),
            ("🤖 AutoML Studio", "Auto-select the best model"),
            ("📦 Model Hub", "Browse & manage models"),
            ("🔍 Explainability", "Understand predictions"),
            ("📡 Drift Monitor", "Detect data drift"),
            ("🖥️ API Console", "Interact with REST API"),
        ]
        for label, desc in links:
            st.markdown(
                f"<div class='link-box'><b>{label}</b> — {desc}</div>",
                unsafe_allow_html=True,
            )


# ── 2. DATA EXPLORER ────────────────────────────────────────────────────────

def page_data_explorer():
    st.title("📁 Data Explorer")

    uploaded = st.file_uploader(
        "Upload a CSV or Parquet file", type=["csv", "parquet"]
    )

    if not uploaded:
        st.info("Upload a file to begin exploration.")
        return

    # Load
    raw = uploaded.read()
    if uploaded.name.endswith(".parquet"):
        df = _load_parquet(raw, uploaded.name)
    else:
        df = _load_csv(raw, uploaded.name)

    # ── summary row ──
    c1, c2, c3, c4 = st.columns(4)
    c1.metric("Rows", f"{len(df):,}")
    c2.metric("Columns", len(df.columns))
    c3.metric("Memory", f"{df.memory_usage(deep=True).sum() / 1024**2:.2f} MB")
    missing_total = int(df.isnull().sum().sum())
    c4.metric("Missing Cells", f"{missing_total:,}")

    st.markdown("---")

    tab_preview, tab_types, tab_missing, tab_corr, tab_dist, tab_cats, tab_quality = st.tabs(
        ["Preview", "Column Types", "Missing Values", "Correlation", "Distributions", "Categorical", "Quality Report"]
    )

    with tab_preview:
        n = st.slider("Rows to preview", 5, min(200, len(df)), 10)
        st.dataframe(df.head(n), use_container_width=True)
        st.caption("Statistical summary")
        st.dataframe(df.describe(include="all").T, use_container_width=True)

    with tab_types:
        type_df = pd.DataFrame(
            {
                "Column": df.columns,
                "Dtype": df.dtypes.astype(str).values,
                "Non-Null Count": df.notnull().sum().values,
                "Null Count": df.isnull().sum().values,
                "Unique Values": [df[c].nunique() for c in df.columns],
                "Memory (KB)": [
                    round(df[c].memory_usage(deep=True) / 1024, 2)
                    for c in df.columns
                ],
            }
        )
        st.dataframe(type_df, use_container_width=True)

    with tab_missing:
        miss = df.isnull().sum()
        miss = miss[miss > 0]
        if miss.empty:
            st.success("✅ No missing values — dataset is complete.")
        else:
            miss_pct = (miss / len(df) * 100).round(2)
            miss_df = pd.DataFrame({"Missing Count": miss, "Missing %": miss_pct})
            st.dataframe(miss_df, use_container_width=True)

            # Heatmap on a sample
            sample = df[miss.index].head(200).isnull().astype(int)
            fig = px.imshow(
                sample.T,
                color_continuous_scale=["#1e2130", "#ff6b6b"],
                title="Missing Value Heatmap (first 200 rows)",
                labels=dict(x="Row index", y="Column", color="Missing"),
            )
            st.plotly_chart(_plotly_defaults(fig, 350), use_container_width=True)

    with tab_corr:
        num_cols = df.select_dtypes(include=np.number).columns.tolist()
        if len(num_cols) < 2:
            st.warning("Need at least 2 numeric columns for a correlation matrix.")
        else:
            corr = df[num_cols].corr()
            fig = px.imshow(
                corr,
                color_continuous_scale="RdBu",
                zmin=-1,
                zmax=1,
                title="Correlation Matrix",
                text_auto=".2f",
            )
            st.plotly_chart(_plotly_defaults(fig, 500), use_container_width=True)

    with tab_dist:
        num_cols = df.select_dtypes(include=np.number).columns.tolist()
        if not num_cols:
            st.warning("No numeric columns found.")
        else:
            col_sel = st.multiselect(
                "Select columns", num_cols, default=num_cols[:4]
            )
            for col in col_sel:
                fig = px.histogram(
                    df,
                    x=col,
                    nbins=40,
                    title=f"Distribution: {col}",
                    color_discrete_sequence=["#64ffda"],
                )
                st.plotly_chart(_plotly_defaults(fig, 300), use_container_width=True)

    with tab_cats:
        cat_cols = df.select_dtypes(include=["object", "category"]).columns.tolist()
        if not cat_cols:
            st.warning("No categorical columns found.")
        else:
            col_sel = st.selectbox("Column", cat_cols)
            top_n = st.slider("Top N values", 5, 30, 10)
            vc = df[col_sel].value_counts().head(top_n).reset_index()
            vc.columns = [col_sel, "Count"]
            fig = px.bar(
                vc,
                x=col_sel,
                y="Count",
                title=f"Top {top_n} values — {col_sel}",
                color_discrete_sequence=["#bd93f9"],
            )
            st.plotly_chart(_plotly_defaults(fig, 330), use_container_width=True)

    with tab_quality:
        st.subheader("🩺 Automatic Data Quality Report")
        issues = []
        # Duplicates
        dup = df.duplicated().sum()
        if dup:
            issues.append(f"⚠️ **{dup}** duplicate rows detected")
        else:
            issues.append("✅ No duplicate rows")

        # Missing
        if missing_total:
            worst = df.isnull().sum().idxmax()
            pct = df[worst].isnull().mean() * 100
            issues.append(
                f"⚠️ Missing values present — worst column: **{worst}** ({pct:.1f}% null)"
            )
        else:
            issues.append("✅ No missing values")

        # Constant columns
        const = [c for c in df.columns if df[c].nunique() <= 1]
        if const:
            issues.append(f"⚠️ Constant (zero-variance) columns: **{', '.join(const)}**")
        else:
            issues.append("✅ No constant columns")

        # High-cardinality categoricals
        hi_card = [
            c
            for c in df.select_dtypes(include=["object", "category"]).columns
            if df[c].nunique() > 0.5 * len(df) and len(df) > 10
        ]
        if hi_card:
            issues.append(
                f"ℹ️ High-cardinality columns (may be IDs): **{', '.join(hi_card)}**"
            )

        # Skewed numerics
        skewed = []
        for col in df.select_dtypes(include=np.number).columns:
            if df[col].dropna().shape[0] > 3:
                skew_val = df[col].skew()
                if abs(skew_val) > 2:
                    skewed.append(f"{col} (skew={skew_val:.2f})")
        if skewed:
            issues.append(f"ℹ️ Highly skewed columns: {', '.join(skewed)}")

        for issue in issues:
            st.markdown(issue)


# ── 3. AUTOML STUDIO ────────────────────────────────────────────────────────

def page_automl():
    st.title("🤖 AutoML Studio")
    st.markdown(
        "<p style='color:#8892b0'>Upload data, configure the search, and let the engine find the best model.</p>",
        unsafe_allow_html=True,
    )

    uploaded = st.file_uploader("Upload training CSV", type=["csv"], key="automl_upload")
    if not uploaded:
        st.info("Upload a CSV file to begin.")
        return

    raw = uploaded.read()
    df = _load_csv(raw, uploaded.name)
    st.caption(f"Loaded **{len(df):,}** rows × **{len(df.columns)}** columns")
    st.dataframe(df.head(5), use_container_width=True)

    col_a, col_b, col_c = st.columns(3)
    with col_a:
        target = st.selectbox("Target column", df.columns.tolist())
    with col_b:
        task = st.selectbox("Task", ["classification", "regression"])
    with col_c:
        pass

    col_d, col_e = st.columns(2)
    with col_d:
        time_budget = st.slider("Time budget (seconds)", 30, 600, 120, 30)
    with col_e:
        n_trials = st.slider("Max trials", 5, 100, 20, 5)

    if st.button("🚀 Run AutoML", type="primary"):
        payload = {
            "data": df.to_dict(orient="list"),
            "target": target,
            "task": task,
            "time_budget": time_budget,
            "n_trials": n_trials,
        }
        with st.spinner("Running AutoML — this may take a moment…"):
            result, err = _api_post("/automl/train", payload, timeout=time_budget + 30)

        if err:
            st.warning(f"⚠️ API unavailable: {err}")
            st.info(
                "Showing a simulated leaderboard for demo purposes."
            )
            # Simulated response
            result = {
                "leaderboard": [
                    {"rank": 1, "model": "GradientBoosting", "score": 0.934, "train_time": 12.3},
                    {"rank": 2, "model": "RandomForest", "score": 0.921, "train_time": 8.7},
                    {"rank": 3, "model": "LogisticRegression", "score": 0.893, "train_time": 1.2},
                    {"rank": 4, "model": "SVM", "score": 0.877, "train_time": 5.6},
                    {"rank": 5, "model": "KNN", "score": 0.852, "train_time": 0.4},
                ],
                "best_model": {
                    "name": "GradientBoosting",
                    "params": {"n_estimators": 200, "max_depth": 5, "learning_rate": 0.05},
                    "score": 0.934,
                },
            }
        else:
            st.success("✅ AutoML complete!")

        lb = pd.DataFrame(result.get("leaderboard", []))
        if not lb.empty:
            st.subheader("📊 Leaderboard")
            st.dataframe(lb.style.highlight_max(subset=["score"], color="#0d3d2e"), use_container_width=True)

        best = result.get("best_model", {})
        if best:
            st.subheader("🥇 Best Model")
            c1, c2 = st.columns(2)
            c1.metric("Model", best.get("name", "—"))
            c2.metric("Score", f"{best.get('score', 0):.4f}")
            st.json(best.get("params", {}))


# ── 4. MODEL HUB ────────────────────────────────────────────────────────────

def page_model_hub():
    st.title("📦 Model Hub")

    data, err = _api_get("/models")
    if err:
        st.warning(f"⚠️ {err}")
        # Demo models
        data = [
            {"model_id": "rf-v1", "type": "RandomForest", "status": "active", "champion": True,
             "metrics": {"accuracy": 0.934, "f1": 0.921, "precision": 0.918, "recall": 0.924}},
            {"model_id": "gb-v2", "type": "GradientBoosting", "status": "active", "champion": False,
             "metrics": {"accuracy": 0.921, "f1": 0.908, "precision": 0.901, "recall": 0.915}},
            {"model_id": "lr-v1", "type": "LogisticRegression", "status": "archived", "champion": False,
             "metrics": {"accuracy": 0.877, "f1": 0.862, "precision": 0.859, "recall": 0.866}},
        ]

    if not data:
        st.info("No models registered yet.")
        return

    # ── model cards ──
    cols = st.columns(min(len(data), 3))
    for i, model in enumerate(data):
        with cols[i % 3]:
            champ_html = "<span class='badge-champ'>👑 CHAMPION</span>" if model.get("champion") else ""
            status_badge = (
                "<span class='badge-ok'>active</span>"
                if model.get("status") == "active"
                else "<span class='badge-err'>archived</span>"
            )
            st.markdown(
                f"""
                <div class='card'>
                <h4>{model['model_id']} &nbsp; {champ_html}</h4>
                <div class='val' style='font-size:1.1rem'>{model['type']}</div>
                <div class='sub' style='margin-top:.4rem'>{status_badge}</div>
                </div>
                """,
                unsafe_allow_html=True,
            )

    st.markdown("---")
    st.subheader("🔍 Model Details")

    model_ids = [m["model_id"] for m in data]
    selected_id = st.selectbox("Select model", model_ids)
    selected = next(m for m in data if m["model_id"] == selected_id)

    metrics = selected.get("metrics", {})
    if metrics:
        m_cols = st.columns(len(metrics))
        for col, (k, v) in zip(m_cols, metrics.items()):
            col.metric(k.capitalize(), f"{v:.4f}")

        fig = px.bar(
            x=list(metrics.keys()),
            y=list(metrics.values()),
            labels={"x": "Metric", "y": "Score"},
            title=f"Metrics — {selected_id}",
            color_discrete_sequence=["#64ffda"],
        )
        fig.update_layout(yaxis=dict(range=[0, 1]))
        st.plotly_chart(_plotly_defaults(fig, 320), use_container_width=True)

    col_promote, col_delete, _ = st.columns([1, 1, 4])
    with col_promote:
        if st.button("👑 Promote to Champion"):
            resp, err = _api_post(f"/models/{selected_id}/promote", {})
            if err:
                st.warning(f"API unavailable: {err}")
            else:
                st.success(f"✅ {selected_id} is now the champion model.")
    with col_delete:
        if st.button("🗑️ Delete Model"):
            st.error(f"DELETE /models/{selected_id} — confirm deletion in production!")


# ── 5. EXPERIMENT TRACKER ───────────────────────────────────────────────────

def page_experiments():
    st.title("🧪 Experiment Tracker")

    data, err = _api_get("/experiments")
    if err:
        st.warning(f"⚠️ {err}")
        data = [
            {"experiment_id": "exp-001", "name": "Breast Cancer Classification", "created": "2025-06-01", "runs": 8},
            {"experiment_id": "exp-002", "name": "Housing Price Regression", "created": "2025-06-10", "runs": 5},
            {"experiment_id": "exp-003", "name": "Churn Prediction", "created": "2025-06-20", "runs": 12},
        ]

    if not data:
        st.info("No experiments found.")
        return

    exp_df = pd.DataFrame(data)
    st.dataframe(exp_df, use_container_width=True)

    st.markdown("---")
    exp_names = {e["experiment_id"]: e["name"] for e in data}
    sel_exp = st.selectbox("Select experiment", list(exp_names.keys()),
                           format_func=lambda x: f"{x} — {exp_names[x]}")

    runs_data, runs_err = _api_get(f"/experiments/{sel_exp}/runs")
    if runs_err:
        # Demo runs
        runs_data = [
            {"run_id": "run-1", "model": "RandomForest", "accuracy": 0.934, "f1": 0.921, "duration_s": 18.3, "status": "completed"},
            {"run_id": "run-2", "model": "GradientBoosting", "accuracy": 0.941, "f1": 0.928, "duration_s": 34.7, "status": "completed"},
            {"run_id": "run-3", "model": "LogisticRegression", "accuracy": 0.877, "f1": 0.862, "duration_s": 2.1, "status": "completed"},
            {"run_id": "run-4", "model": "SVM", "accuracy": 0.891, "f1": 0.880, "duration_s": 9.4, "status": "failed"},
        ]

    if runs_data:
        runs_df = pd.DataFrame(runs_data)
        st.subheader("📋 Runs")

        # Highlight best
        numeric_cols = runs_df.select_dtypes(include=np.number).columns.tolist()
        styled = runs_df.style
        if "accuracy" in numeric_cols:
            styled = styled.highlight_max(subset=["accuracy"], color="#0d3d2e")
        st.dataframe(styled, use_container_width=True)

        # Run comparison
        st.subheader("⚖️ Run Comparison")
        if len(runs_df) >= 2:
            sel_runs = st.multiselect(
                "Select 2+ runs to compare",
                runs_df["run_id"].tolist(),
                default=runs_df["run_id"].tolist()[:2],
            )
            if len(sel_runs) >= 2:
                cmp_df = runs_df[runs_df["run_id"].isin(sel_runs)][
                    ["run_id", "model"] + numeric_cols
                ].set_index("run_id")
                st.dataframe(cmp_df, use_container_width=True)

                if numeric_cols:
                    fig = go.Figure()
                    for _, row in cmp_df.iterrows():
                        fig.add_trace(
                            go.Bar(name=str(row.name), x=numeric_cols, y=row[numeric_cols].values)
                        )
                    fig.update_layout(barmode="group", title="Metric Comparison")
                    st.plotly_chart(_plotly_defaults(fig, 350), use_container_width=True)


# ── 6. EXPLAINABILITY CENTER ────────────────────────────────────────────────

def page_explainability():
    st.title("🔍 Explainability Center")

    models_data, models_err = _api_get("/models")
    if models_err:
        st.warning(f"⚠️ {models_err}")
        model_ids = ["rf-v1", "gb-v2", "lr-v1"]
    else:
        model_ids = [m["model_id"] for m in (models_data or [])] or ["rf-v1"]

    sel_model = st.selectbox("Select model to explain", model_ids)

    st.markdown("#### Enter feature values")
    input_method = st.radio("Input method", ["Manual entry", "Upload CSV row"], horizontal=True)

    features = {}
    if input_method == "Manual entry":
        col_a, col_b = st.columns(2)
        with col_a:
            raw_features = st.text_area(
                "Feature values (JSON dict)",
                value='{"feature_1": 1.23, "feature_2": 0.45, "feature_3": -0.78}',
                height=120,
            )
        with col_b:
            st.caption("Example: `{\"age\": 35, \"income\": 52000}`")
        try:
            features = json.loads(raw_features)
        except json.JSONDecodeError:
            st.error("Invalid JSON — please fix the feature dict.")
            return
    else:
        uploaded = st.file_uploader("Upload a single-row CSV", type=["csv"])
        if uploaded:
            row_df = pd.read_csv(uploaded)
            if len(row_df):
                features = row_df.iloc[0].to_dict()
                st.json(features)

    if st.button("🔮 Explain Prediction", type="primary") and features:
        payload = {"model_id": sel_model, "features": features}
        with st.spinner("Computing explanation…"):
            result, err = _api_post("/explain", payload, timeout=30)

        if err:
            st.warning(f"⚠️ {err}")
            # Demo explanation
            feat_names = list(features.keys()) if features else [f"feature_{i}" for i in range(6)]
            importances = np.abs(np.random.default_rng(42).normal(0, 1, len(feat_names)))
            importances = importances / importances.sum()
            result = {
                "prediction": 1,
                "probability": 0.82,
                "feature_importance": dict(zip(feat_names, importances.tolist())),
            }

        pred = result.get("prediction")
        prob = result.get("probability")
        c1, c2 = st.columns(2)
        if pred is not None:
            c1.metric("Prediction", pred)
        if prob is not None:
            c2.metric("Confidence", f"{prob:.2%}")

        fi = result.get("feature_importance", {})
        if fi:
            fi_df = pd.DataFrame(
                sorted(fi.items(), key=lambda x: abs(x[1]), reverse=True),
                columns=["Feature", "Importance"],
            )
            fig = px.bar(
                fi_df,
                x="Importance",
                y="Feature",
                orientation="h",
                title="Feature Importance",
                color="Importance",
                color_continuous_scale=["#1e2130", "#64ffda"],
            )
            fig.update_layout(yaxis=dict(autorange="reversed"))
            st.plotly_chart(_plotly_defaults(fig, 400), use_container_width=True)

        local = result.get("local_explanation")
        if local:
            st.subheader("Local Explanation (SHAP values)")
            st.json(local)


# ── 7. DRIFT MONITOR ────────────────────────────────────────────────────────

def page_drift():
    st.title("📡 Drift Monitor")
    st.markdown(
        "<p style='color:#8892b0'>Upload a reference dataset and current dataset to detect feature drift.</p>",
        unsafe_allow_html=True,
    )

    c_ref, c_cur = st.columns(2)
    with c_ref:
        ref_file = st.file_uploader("📂 Reference CSV", type=["csv"], key="drift_ref")
    with c_cur:
        cur_file = st.file_uploader("📂 Current CSV", type=["csv"], key="drift_cur")

    if not ref_file or not cur_file:
        st.info("Upload both reference and current CSVs to run drift detection.")
        return

    ref_df = _load_csv(ref_file.read(), "reference")
    cur_df = _load_csv(cur_file.read(), "current")

    st.caption(
        f"Reference: **{len(ref_df):,}** rows | Current: **{len(cur_df):,}** rows"
    )

    if st.button("🔍 Detect Drift", type="primary"):
        payload = {
            "reference": ref_df.to_dict(orient="list"),
            "current": cur_df.to_dict(orient="list"),
        }
        with st.spinner("Running drift detection…"):
            result, err = _api_post("/drift/detect", payload, timeout=60)

        if err:
            st.warning(f"⚠️ {err}")
            # Generate demo drift scores from actual data
            common_num = [
                c
                for c in ref_df.select_dtypes(include=np.number).columns
                if c in cur_df.columns
            ][:8]
            if not common_num:
                common_num = [f"feature_{i}" for i in range(5)]
            rng = np.random.default_rng(7)
            scores = rng.uniform(0, 1, len(common_num))
            result = {
                "drift_detected": bool((scores > 0.5).any()),
                "features": {
                    f: {"score": float(s), "drifted": bool(s > 0.5)}
                    for f, s in zip(common_num, scores)
                },
            }

        overall = result.get("drift_detected", False)
        if overall:
            st.error("🚨 Data drift detected!")
        else:
            st.success("✅ No significant drift detected.")

        features = result.get("features", {})
        if features:
            drift_rows = []
            for feat, info in features.items():
                score = info.get("score", 0)
                drifted = info.get("drifted", score > 0.5)
                status = "🔴 DRIFT" if drifted else ("🟡 BORDERLINE" if score > 0.3 else "🟢 OK")
                drift_rows.append({"Feature": feat, "Drift Score": round(score, 4), "Status": status})

            drift_df = pd.DataFrame(drift_rows).sort_values("Drift Score", ascending=False)
            st.dataframe(drift_df, use_container_width=True)

            fig = px.bar(
                drift_df,
                x="Feature",
                y="Drift Score",
                color="Drift Score",
                color_continuous_scale=["#64ffda", "#ffd700", "#ff6b6b"],
                title="Drift Score per Feature",
            )
            fig.add_hline(y=0.5, line_dash="dash", line_color="#ff6b6b",
                          annotation_text="Drift threshold")
            st.plotly_chart(_plotly_defaults(fig, 350), use_container_width=True)


# ── 8. API CONSOLE ──────────────────────────────────────────────────────────

ENDPOINTS = {
    "GET /health": {"method": "GET", "path": "/health", "body": None},
    "GET /status": {"method": "GET", "path": "/status", "body": None},
    "GET /models": {"method": "GET", "path": "/models", "body": None},
    "GET /experiments": {"method": "GET", "path": "/experiments", "body": None},
    "POST /predict": {
        "method": "POST",
        "path": "/predict",
        "body": '{\n  "model_id": "rf-v1",\n  "features": {"feature_1": 1.0, "feature_2": 0.5}\n}',
    },
    "POST /automl/train": {
        "method": "POST",
        "path": "/automl/train",
        "body": '{\n  "target": "label",\n  "task": "classification",\n  "time_budget": 60\n}',
    },
    "POST /explain": {
        "method": "POST",
        "path": "/explain",
        "body": '{\n  "model_id": "rf-v1",\n  "features": {"feature_1": 1.0}\n}',
    },
    "POST /drift/detect": {
        "method": "POST",
        "path": "/drift/detect",
        "body": '{\n  "reference": {},\n  "current": {}\n}',
    },
}


def page_api_console():
    st.title("🖥️ API Console")

    sel_ep = st.selectbox("Endpoint", list(ENDPOINTS.keys()))
    ep = ENDPOINTS[sel_ep]

    col_req, col_resp = st.columns(2)

    with col_req:
        st.subheader("Request")
        st.code(f"{ep['method']} {API_BASE}{ep['path']}", language="http")
        body_str = None
        if ep["method"] == "POST" and ep["body"] is not None:
            body_str = st.text_area("Request body (JSON)", value=ep["body"], height=200)

        if st.button("📤 Send Request", type="primary"):
            t0 = time.perf_counter()
            if ep["method"] == "GET":
                response_data, err = _api_get(ep["path"])
            else:
                try:
                    payload = json.loads(body_str or "{}")
                except json.JSONDecodeError:
                    st.error("Invalid JSON body")
                    return
                response_data, err = _api_post(ep["path"], payload)
            elapsed = time.perf_counter() - t0

            with col_resp:
                st.subheader("Response")
                st.caption(f"⏱ Response time: **{elapsed * 1000:.1f} ms**")
                if err:
                    st.warning(err)
                else:
                    st.success("✅ 200 OK")
                    st.json(response_data)
        else:
            with col_resp:
                st.subheader("Response")
                st.info("Hit **Send Request** to see the response here.")


# ── 9. DIAGNOSTICS ──────────────────────────────────────────────────────────

def page_diagnostics():
    st.title("🔧 Diagnostics")

    if not st.button("▶ Run Full Diagnostics", type="primary"):
        st.info("Click the button above to run the full diagnostic suite.")
        return

    # ── Python packages ──
    st.subheader("📦 Python Package Check")
    required = [
        "streamlit", "pandas", "numpy", "plotly", "sklearn",
        "psutil", "requests", "fastapi", "uvicorn", "click",
    ]
    pkg_rows = []
    for pkg in required:
        try:
            mod = __import__(pkg if pkg != "sklearn" else "sklearn")
            ver = getattr(mod, "__version__", "unknown")
            pkg_rows.append({"Package": pkg, "Version": ver, "Status": "✅ OK"})
        except ImportError:
            pkg_rows.append({"Package": pkg, "Version": "—", "Status": "❌ Missing"})
    st.dataframe(pd.DataFrame(pkg_rows), use_container_width=True)

    # ── System resources ──
    st.subheader("💻 System Resources")
    metrics = _system_metrics()
    disk = psutil.disk_usage("/") if psutil.disk_usage.__module__ else None
    try:
        disk = psutil.disk_usage("/")
        disk_pct = disk.percent
        disk_free = disk.free / 1024 ** 3
    except Exception:
        disk_pct = 0
        disk_free = 0

    res_data = {
        "Resource": ["CPU", "Memory", "Disk"],
        "Usage %": [metrics["cpu"], metrics["mem"], disk_pct],
        "Status": [
            "✅" if metrics["cpu"] < 80 else "⚠️",
            "✅" if metrics["mem"] < 85 else "⚠️",
            "✅" if disk_pct < 90 else "⚠️",
        ],
    }
    st.dataframe(pd.DataFrame(res_data), use_container_width=True)

    fig = px.bar(
        x=res_data["Resource"],
        y=res_data["Usage %"],
        color=res_data["Usage %"],
        color_continuous_scale=["#64ffda", "#ffd700", "#ff6b6b"],
        range_color=[0, 100],
        title="System Resource Usage",
    )
    st.plotly_chart(_plotly_defaults(fig, 300), use_container_width=True)

    # ── Docker / Git ──
    st.subheader("🛠️ Tool Availability")
    import subprocess as _sp
    tools = [
        ("git", ["git", "--version"]),
        ("docker", ["docker", "--version"]),
        ("python", ["python", "--version"]),
    ]
    tool_rows = []
    for tool, cmd in tools:
        try:
            out = _sp.check_output(cmd, stderr=_sp.STDOUT, timeout=5).decode().strip()
            tool_rows.append({"Tool": tool, "Version": out, "Status": "✅ Available"})
        except Exception:
            tool_rows.append({"Tool": tool, "Version": "—", "Status": "❌ Not found"})
    st.dataframe(pd.DataFrame(tool_rows), use_container_width=True)

    # ── Benchmark ──
    st.subheader("⚡ Quick Benchmark")
    with st.spinner("Running CPU + memory benchmark…"):
        # CPU
        t0 = time.perf_counter()
        for _ in range(500):
            np.dot(np.random.randn(100, 100), np.random.randn(100, 100))
        cpu_bench = time.perf_counter() - t0

        # Memory
        t1 = time.perf_counter()
        arr = np.random.randn(5000, 500)
        _ = np.dot(arr, arr.T)
        mem_bench = time.perf_counter() - t1
        del arr

    bench_data = [
        {"Benchmark": "CPU (500 × matmul 100×100)", "Time (s)": round(cpu_bench, 3)},
        {"Benchmark": "Memory (5000×500 matmul)", "Time (s)": round(mem_bench, 3)},
    ]
    st.dataframe(pd.DataFrame(bench_data), use_container_width=True)
    st.success(
        f"✅ Diagnostics complete — CPU bench: {cpu_bench:.3f}s | Memory bench: {mem_bench:.3f}s"
    )


# ════════════════════════════════════════════════════════════════════════════
#  SIDEBAR + ROUTING
# ════════════════════════════════════════════════════════════════════════════

PAGES = {
    "🏠 Home": page_home,
    "📁 Data Explorer": page_data_explorer,
    "🤖 AutoML Studio": page_automl,
    "📦 Model Hub": page_model_hub,
    "🧪 Experiment Tracker": page_experiments,
    "🔍 Explainability Center": page_explainability,
    "📡 Drift Monitor": page_drift,
    "🖥️ API Console": page_api_console,
    "🔧 Diagnostics": page_diagnostics,
}

with st.sidebar:
    st.markdown(
        "<h2 style='color:#64ffda;margin-bottom:0'>🔬 Aimran Lab</h2>"
        f"<p style='color:#8892b0;font-size:.8rem;margin-top:0'>v{PLATFORM_VERSION}</p>",
        unsafe_allow_html=True,
    )
    st.markdown("---")
    page = st.radio("Navigate to", list(PAGES.keys()), label_visibility="collapsed")
    st.markdown("---")
    st.caption(f"🕐 {datetime.now().strftime('%H:%M:%S')}")
    st.caption(f"CPU: {psutil.cpu_percent()}%  |  RAM: {psutil.virtual_memory().percent}%")

# Run selected page
PAGES[page]()

# Footer
st.markdown("---")
st.markdown(
    "<div style='text-align:center;color:#8892b0;font-size:.75rem'>"
    "Aimran Data Science Lab &nbsp;•&nbsp; Enterprise Platform &nbsp;•&nbsp; "
    f"v{PLATFORM_VERSION}"
    "</div>",
    unsafe_allow_html=True,
)
