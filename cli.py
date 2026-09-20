"""
Aimran Data Science Lab — CLI
Run with: python cli.py <command> [options]
"""

import json
import logging
import subprocess
import sys
import threading
import time
from pathlib import Path

import click
from tabulate import tabulate

from src.performance import PerformanceProfiler, PerformanceOptimizer
from src.health_check import health_manager

# Windows consoles default to cp1252 and crash on emoji output; force UTF-8.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

logging.basicConfig(level=logging.WARNING)
logger = logging.getLogger(__name__)

API_BASE = "http://localhost:8000"
PLATFORM_VERSION = "1.0.0"


# ── helpers ──────────────────────────────────────────────────────────────────

def _api_get(path: str, timeout: int = 8):
    import requests as _req
    return _req.get(f"{API_BASE}{path}", timeout=timeout)


def _api_post(path: str, payload: dict, timeout: int = 60):
    import requests as _req
    return _req.post(f"{API_BASE}{path}", json=payload, timeout=timeout)


def _connection_error(cmd_name: str):
    click.secho(
        f"❌ Cannot reach API at {API_BASE}. "
        "Start the server with:  python cli.py serve",
        fg="red",
    )


# ════════════════════════════════════════════════════════════════════════════
#  CLI GROUP
# ════════════════════════════════════════════════════════════════════════════

@click.group()
def cli():
    """🔬 Aimran Data Science Lab — Command Line Interface"""
    pass


# ── version ──────────────────────────────────────────────────────────────────

@cli.command()
def version():
    """Show platform version information."""
    import platform as _plat
    rows = [
        ["Platform", f"Aimran Data Science Lab v{PLATFORM_VERSION}"],
        ["Python", sys.version.split()[0]],
        ["OS", f"{_plat.system()} {_plat.release()}"],
        ["Architecture", _plat.machine()],
    ]
    click.secho("\n🔬 Aimran Data Science Lab", fg="cyan", bold=True)
    click.echo(tabulate(rows, tablefmt="rounded_outline"))


# ── health ───────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--quick", is_flag=True, help="Quick single-endpoint check only")
def health(quick):
    """Comprehensive (or quick) system health check."""
    if quick:
        click.secho("🏥 Quick Health Check…", fg="cyan")
        try:
            r = _api_get("/health")
            if r.status_code == 200:
                click.secho("✅ API is healthy", fg="green")
            else:
                click.secho(f"⚠️  API returned HTTP {r.status_code}", fg="yellow")
        except Exception as exc:
            _connection_error("health")
        return

    click.secho("🔍 Running comprehensive diagnostics…\n", fg="cyan")
    report = health_manager.full_report()
    checks = report["checks"]

    click.secho("📊 System Resources:", bold=True)
    rows = []
    for label, key in [("CPU", None), ("Memory", None), ("Disk", "disk")]:
        if key:
            c = checks[key]
            status_icon = "✅" if c["status"] == "ok" else "⚠️"
            usage = c.get("detail", c["status"])
            rows.append([label, usage, status_icon])

    click.echo(tabulate(rows, headers=["Resource", "Usage", "Status"], tablefmt="rounded_outline"))

    click.secho("\n🔌 Service Connectivity:", bold=True)
    svc_rows = []
    for label, key in [("Redis", "redis"), ("Database", "database"), ("API", "api")]:
        c = checks.get(key, {})
        status_icon = "✅" if c.get("status") == "ok" else "❌"
        detail = c.get("detail", c.get("status", "unknown"))
        latency = c.get("latency_ms")
        detail_str = f"{detail}  ({latency}ms)" if latency is not None else detail
        svc_rows.append([label, detail_str, status_icon])
    click.echo(tabulate(svc_rows, headers=["Service", "Detail", "Status"], tablefmt="rounded_outline"))

    click.secho("\n📦 Model Registry:", bold=True)
    mc = checks.get("model_registry", {})
    models = mc.get("models", [])
    if models:
        for m in models:
            click.secho(f"  • {m}", fg="cyan")
    else:
        click.secho("  (no models registered)", fg="yellow")

    overall = report["overall"]
    colour = "green" if overall == "healthy" else "yellow"
    click.secho(f"\n{'✅' if overall == 'healthy' else '⚠️'} Overall: {overall.upper()}", fg=colour, bold=True)


# ── status ───────────────────────────────────────────────────────────────────

@cli.command()
@click.option(
    "--format",
    "fmt",
    type=click.Choice(["json", "table"]),
    default="table",
    help="Output format",
)
def status(fmt):
    """Show system status."""
    click.secho("📡 Fetching system status…\n", fg="cyan")
    try:
        r = _api_get("/status")
        data = r.json()
        if fmt == "json":
            click.echo(json.dumps(data, indent=2))
        else:
            sys_d = data.get("system", {})
            rows = [
                ["CPU",       f"{sys_d.get('cpu_percent', 'N/A')}%"],
                ["Memory",    f"{sys_d.get('memory_mb', 'N/A')} MB"],
                ["Processes", sys_d.get("processes", "N/A")],
            ]
            click.echo(tabulate(rows, headers=["Metric", "Value"], tablefmt="rounded_outline"))
            async_flag = "Yes" if sys_d.get("async_capable") else "No"
            click.secho(f"\nAsync capable: {async_flag}", fg="green")
    except Exception:
        _connection_error("status")


# ── explore ──────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--file", "filepath", required=True, help="Path to CSV/Parquet file")
@click.option("--rows", type=int, default=5, help="Number of rows to preview")
def explore(filepath, rows):
    """Explore a data file (CSV or Parquet)."""
    import pandas as pd

    path = Path(filepath)
    if not path.exists():
        click.secho(f"❌ File not found: {filepath}", fg="red")
        sys.exit(1)

    click.secho(f"\n📊 Exploring: {filepath}\n", fg="cyan", bold=True)

    try:
        df = pd.read_parquet(path) if path.suffix == ".parquet" else pd.read_csv(path)
    except Exception as exc:
        click.secho(f"❌ Failed to load file: {exc}", fg="red")
        sys.exit(1)

    summary = [
        ["Rows",    f"{len(df):,}"],
        ["Columns", len(df.columns)],
        ["Memory",  f"{df.memory_usage(deep=True).sum() / 1024**2:.2f} MB"],
        ["Dtypes",  ", ".join(df.dtypes.astype(str).unique())],
    ]
    click.echo(tabulate(summary, headers=["Metric", "Value"], tablefmt="rounded_outline"))

    click.secho(f"\n📋 First {rows} rows:", bold=True)
    click.echo(df.head(rows).to_string())

    # Column types
    click.secho("\n🗂️  Column types:", bold=True)
    type_rows = [(c, str(df[c].dtype), df[c].nunique()) for c in df.columns]
    click.echo(tabulate(type_rows, headers=["Column", "Type", "Unique"], tablefmt="rounded_outline"))

    # Missing values
    missing = df.isnull().sum()
    if missing.sum() > 0:
        click.secho("\n⚠️  Missing values:", fg="yellow", bold=True)
        miss_rows = [(c, int(cnt), f"{cnt/len(df)*100:.1f}%") for c, cnt in missing[missing > 0].items()]
        click.echo(tabulate(miss_rows, headers=["Column", "Count", "%"], tablefmt="rounded_outline"))
    else:
        click.secho("\n✅ No missing values", fg="green")

    # Basic stats for numeric cols
    import numpy as np
    num_cols = df.select_dtypes(include=np.number).columns
    if len(num_cols):
        click.secho("\n📈 Numeric summary:", bold=True)
        click.echo(df[num_cols].describe().to_string())


# ── serve ────────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--host", default="0.0.0.0", show_default=True, help="Bind host")
@click.option("--port", default=8000, show_default=True, type=int, help="Bind port")
@click.option("--workers", default=4, show_default=True, type=int, help="Worker count")
@click.option("--reload", "hot_reload", is_flag=True, help="Enable hot reload (dev mode)")
def serve(host, port, workers, hot_reload):
    """Start the FastAPI server with Uvicorn."""
    click.secho(f"🚀 Starting API server on http://{host}:{port}  (workers={workers})\n", fg="cyan", bold=True)
    cmd = [
        "uvicorn", "src.api.main:app",
        "--host", host,
        "--port", str(port),
        "--workers", str(workers),
    ]
    if hot_reload:
        cmd.append("--reload")
        click.secho("♻️  Hot-reload enabled", fg="yellow")
    try:
        subprocess.run(cmd)
    except KeyboardInterrupt:
        click.secho("\n⏹️  Server stopped", fg="yellow")
    except FileNotFoundError:
        click.secho("❌ uvicorn not found. Install with: pip install uvicorn", fg="red")


# ── serve-grpc ────────────────────────────────────────────────────────────────

@cli.command("serve-grpc")
@click.option("--port", default=50051, show_default=True, type=int, help="gRPC listen port")
@click.option("--workers", default=10, show_default=True, type=int, help="Thread pool size")
def serve_grpc(port, workers):
    """Start the gRPC server (ServiceRegistry, ServiceBroker, ServiceHeartbeat)."""
    try:
        from src.multi_language.grpc_server import GrpcServer
    except ImportError as exc:
        click.secho(f"❌ {exc}", fg="red")
        sys.exit(1)
    server = GrpcServer(port=port, max_workers=workers)
    server.start()
    click.secho(f"🛰️  gRPC server listening on port {port}", fg="green", bold=True)
    click.echo("   Services: ServiceRegistry, ServiceBroker, ServiceHeartbeat\n")
    try:
        server.serve()
    except KeyboardInterrupt:
        server.stop()
        click.secho("\n⏹️  gRPC server stopped", fg="yellow")


# ── dashboard ────────────────────────────────────────────────────────────────

@cli.command()
def dashboard():
    """Launch the Streamlit dashboard."""
    click.secho("📊 Starting Streamlit dashboard…\n", fg="cyan")
    try:
        subprocess.run(["streamlit", "run", "ui/dashboard.py"])
    except FileNotFoundError:
        click.secho("❌ Streamlit not found. Install with: pip install streamlit", fg="red")
    except KeyboardInterrupt:
        click.secho("\n⏹️  Dashboard stopped", fg="yellow")


# ── test ─────────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--test", "test_path", default=None, help="Specific test file or directory")
@click.option("--verbose", "-v", is_flag=True, help="Verbose pytest output")
@click.option("--coverage", is_flag=True, help="Measure code coverage (requires pytest-cov)")
def test(test_path, verbose, coverage):
    """Run the test suite with pytest."""
    click.secho("🧪 Running tests…\n", fg="cyan")

    cmd = ["pytest"]
    cmd.append(test_path or "tests/")
    cmd.append("--tb=short")

    if verbose:
        cmd.append("-v")
    if coverage:
        cmd.extend(["--cov=src", "--cov-report=term-missing", "--cov-report=html:htmlcov"])
        click.secho("📊 Coverage report will be written to htmlcov/", fg="yellow")

    try:
        result = subprocess.run(cmd)
        if result.returncode == 0:
            click.secho("\n✅ All tests passed!", fg="green", bold=True)
        else:
            click.secho(f"\n❌ Tests failed (exit code {result.returncode})", fg="red", bold=True)
        sys.exit(result.returncode)
    except KeyboardInterrupt:
        click.secho("\n⏹️  Tests interrupted", fg="yellow")
    except FileNotFoundError:
        click.secho("❌ pytest not found. Install with: pip install pytest", fg="red")


# ── benchmark ────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--duration", type=int, default=10, show_default=True,
              help="Target benchmark duration (informational)")
def benchmark(duration):
    """Run CPU and memory performance benchmarks."""
    import numpy as np

    click.secho(f"⚡ Running benchmarks (~{duration}s)…\n", fg="cyan", bold=True)
    results = []

    # CPU
    click.echo("1️⃣  CPU — matrix multiplications…", nl=False)
    t0 = time.perf_counter()
    iters = max(1, duration * 50)
    for _ in range(iters):
        np.dot(np.random.randn(100, 100), np.random.randn(100, 100))
    cpu_time = time.perf_counter() - t0
    click.secho(f"  done ({cpu_time:.3f}s)", fg="green")
    results.append(["CPU matmul ×{:,}".format(iters), f"{cpu_time:.3f}s",
                    f"{iters / cpu_time:.0f} ops/s"])

    # Memory
    click.echo("2️⃣  Memory — large array operations…", nl=False)
    t1 = time.perf_counter()
    arr = np.random.randn(5_000, 500)
    _ = np.dot(arr, arr.T)
    mem_time = time.perf_counter() - t1
    click.secho(f"  done ({mem_time:.3f}s)", fg="green")
    results.append(["Memory (5k×500 matmul)", f"{mem_time:.3f}s", "—"])
    del arr

    click.secho("\n📊 Benchmark Results:", bold=True)
    click.echo(tabulate(results, headers=["Test", "Time", "Throughput"],
                        tablefmt="rounded_outline"))


# ── setup ────────────────────────────────────────────────────────────────────

@cli.command()
def setup():
    """Bootstrap the lab environment (create dirs, validate config)."""
    click.secho("🔧 Setting up Aimran Data Science Lab…\n", fg="cyan", bold=True)

    steps = [
        ("data/raw",         lambda: Path("data/raw").mkdir(parents=True, exist_ok=True)),
        ("data/processed",   lambda: Path("data/processed").mkdir(parents=True, exist_ok=True)),
        ("models",           lambda: Path("models").mkdir(parents=True, exist_ok=True)),
        ("logs",             lambda: Path("logs").mkdir(parents=True, exist_ok=True)),
        ("notebooks",        lambda: Path("notebooks").mkdir(parents=True, exist_ok=True)),
    ]

    for label, fn in steps:
        click.echo(f"  📁 Creating {label}…", nl=False)
        try:
            fn()
            click.secho("  ✅", fg="green")
        except Exception as exc:
            click.secho(f"  ❌  {exc}", fg="red")

    click.echo("\n  🔍 Checking key dependencies…")
    pkgs = ["streamlit", "fastapi", "pandas", "sklearn", "plotly", "psutil", "click"]
    missing = []
    for pkg in pkgs:
        try:
            __import__(pkg)
            click.secho(f"    ✅ {pkg}", fg="green")
        except ImportError:
            click.secho(f"    ❌ {pkg}  (not installed)", fg="red")
            missing.append(pkg)

    if missing:
        click.secho(
            f"\n⚠️  Missing packages: {', '.join(missing)}\n"
            "Run: pip install -r requirements.txt",
            fg="yellow",
        )
    else:
        click.secho("\n✅ Setup complete! All dependencies present.", fg="green", bold=True)


# ── logs ─────────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--tail", type=int, default=20, show_default=True, help="Number of last lines to show")
@click.option("--follow", "-f", is_flag=True, help="Follow log file in real time")
def logs(tail, follow):
    """View application logs."""
    log_file = Path("logs/app.log")

    if not log_file.exists():
        click.secho("❌ No log file found at logs/app.log", fg="red")
        sys.exit(1)

    click.secho(f"📜 Log file: {log_file}\n", fg="cyan")

    with open(log_file, "r", encoding="utf-8", errors="replace") as f:
        lines = f.readlines()

    for line in lines[-tail:]:
        _coloured_log_line(line.rstrip())

    if not follow:
        return

    # Follow mode
    click.secho("\n👁️  Following (Ctrl+C to stop)…\n", fg="yellow")
    try:
        with open(log_file, "r", encoding="utf-8", errors="replace") as f:
            f.seek(0, 2)  # seek to end
            while True:
                line = f.readline()
                if line:
                    _coloured_log_line(line.rstrip())
                else:
                    time.sleep(0.25)
    except KeyboardInterrupt:
        click.secho("\n⏹️  Stopped following logs.", fg="yellow")


def _coloured_log_line(line: str):
    lo = line.lower()
    if "error" in lo or "critical" in lo:
        click.secho(line, fg="red")
    elif "warning" in lo or "warn" in lo:
        click.secho(line, fg="yellow")
    elif "info" in lo:
        click.secho(line, fg="white")
    else:
        click.echo(line)


# ── automl ───────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--file", "filepath", required=True, help="Path to training CSV")
@click.option("--target", required=True, help="Target column name")
@click.option("--task", type=click.Choice(["classification", "regression"]),
              default="classification", show_default=True)
@click.option("--budget", type=int, default=60, show_default=True, help="Time budget (seconds)")
@click.option("--trials", type=int, default=20, show_default=True, help="Max trials")
def automl(filepath, target, task, budget, trials):
    """Run AutoML via the REST API and show the leaderboard."""
    import pandas as pd

    path = Path(filepath)
    if not path.exists():
        click.secho(f"❌ File not found: {filepath}", fg="red")
        sys.exit(1)

    click.secho(f"\n🤖 AutoML — {task} on column '{target}'", fg="cyan", bold=True)
    click.echo(f"   File: {filepath}")
    click.echo(f"   Budget: {budget}s  |  Trials: {trials}\n")

    try:
        df = pd.read_csv(path)
    except Exception as exc:
        click.secho(f"❌ Failed to load CSV: {exc}", fg="red")
        sys.exit(1)

    if target not in df.columns:
        click.secho(
            f"❌ Target column '{target}' not found in CSV. "
            f"Available columns: {', '.join(map(str, df.columns))}",
            fg="red",
        )
        sys.exit(1)

    payload = {
        "data": df.to_dict(orient="list"),
        "target": target,
        "task": task,
        "time_budget": budget,
        "n_trials": trials,
    }

    click.echo("📤 Sending request to API…")

    request = {"response": None, "error": None, "done": threading.Event()}

    def _call():
        try:
            request["response"] = _api_post("/automl/train", payload, timeout=budget + 30)
        except Exception as exc:
            request["error"] = str(exc)
        finally:
            request["done"].set()

    t = threading.Thread(target=_call, daemon=True)
    t.start()

    with click.progressbar(length=budget, label="AutoML running") as bar:
        elapsed = 0
        while not request["done"].is_set() and elapsed < budget:
            time.sleep(1)
            bar.update(1)
            elapsed += 1
        if not request["done"].is_set():
            bar.update(budget - elapsed)
            click.echo("\n⏳ Request still in progress — waiting for the API to finish…")

    if not request["done"].wait(timeout=budget + 30):
        request["error"] = f"API request timed out after {budget + 30}s"
    t.join(timeout=1)

    if request["error"]:
        click.secho(f"❌ API error: {request['error']}", fg="red")
        _connection_error("automl")
        sys.exit(1)

    r = request["response"]
    if r.status_code != 200:
        click.secho(f"❌ API returned HTTP {r.status_code}: {r.text}", fg="red")
        sys.exit(1)

    try:
        data = r.json()
    except Exception as exc:
        click.secho(f"❌ API returned an invalid response: {exc}", fg="red")
        sys.exit(1)

    lb = data.get("leaderboard", [])
    if lb:
        click.secho("\n📊 Leaderboard:", bold=True)
        click.echo(tabulate(lb, headers="keys", tablefmt="rounded_outline"))
    else:
        click.secho("\nℹ️  No leaderboard entries in the response.", fg="yellow")

    best = data.get("best_model", {})
    if best:
        click.secho(f"\n🥇 Best Model: {best.get('name', '—')}  (score={best.get('score', '—')})",
                    fg="green", bold=True)
        click.echo(json.dumps(best.get("params", {}), indent=2))
    else:
        click.secho("\nℹ️  No best-model summary in the response.", fg="yellow")


# ── train ────────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--file", "filepath", required=True, help="Path to training CSV")
@click.option("--model", "model_type", required=True,
              type=click.Choice(["random_forest", "gradient_boosting", "neural_network", "logistic_regression"]),
              help="Model type")
@click.option("--model-id", default=None, help="Custom model ID (auto-generated if omitted)")
@click.option("--task", type=click.Choice(["classification", "regression"]),
              default="classification", show_default=True)
def train(filepath, model_type, model_id, task):
    """Quickly train a model via the REST API."""
    import pandas as pd
    from datetime import datetime

    path = Path(filepath)
    if not path.exists():
        click.secho(f"❌ File not found: {filepath}", fg="red")
        sys.exit(1)

    if not model_id:
        model_id = f"{model_type}-{datetime.now():%Y%m%d%H%M%S}"

    click.secho(f"\n🏋️  Training {model_type} ({task})", fg="cyan", bold=True)
    click.echo(f"   Model ID : {model_id}")
    click.echo(f"   File     : {filepath}\n")

    try:
        df = pd.read_csv(path)
    except Exception as exc:
        click.secho(f"❌ {exc}", fg="red")
        sys.exit(1)

    payload = {
        "data": df.to_dict(orient="list"),
        "model_type": model_type,
        "model_id": model_id,
        "task": task,
    }

    try:
        with click.progressbar(length=10, label="Training") as bar:
            r = _api_post("/train", payload, timeout=120)
            bar.update(10)
    except Exception:
        _connection_error("train")
        sys.exit(1)

    if r.status_code == 200:
        data = r.json()
        click.secho(f"\n✅ Model trained!  ID: {data.get('model_id', model_id)}", fg="green", bold=True)
        metrics = data.get("metrics", {})
        if metrics:
            rows = [(k, v) for k, v in metrics.items()]
            click.echo(tabulate(rows, headers=["Metric", "Value"], tablefmt="rounded_outline"))
    else:
        click.secho(f"❌ API returned HTTP {r.status_code}: {r.text}", fg="red")
        sys.exit(1)


# ── predict ──────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--model-id", required=True, help="Model ID to use for prediction")
@click.option("--features", required=True,
              help='Comma-separated feature values, e.g. "1.0,2.5,0.3"')
def predict(model_id, features):
    """Run a quick prediction via the REST API."""
    click.secho(f"\n🔮 Predicting with model: {model_id}", fg="cyan", bold=True)

    try:
        values = [float(v.strip()) for v in features.split(",")]
    except ValueError:
        click.secho("❌ Features must be comma-separated numbers (e.g. 1.0,2.5,0.3)", fg="red")
        sys.exit(1)

    payload = {"model_id": model_id, "features": values}

    try:
        r = _api_post("/predict", payload, timeout=15)
    except Exception:
        _connection_error("predict")
        sys.exit(1)

    if r.status_code == 200:
        data = r.json()
        pred = data.get("prediction")
        prob = data.get("probability")
        click.secho(f"\n✅ Prediction : {pred}", fg="green", bold=True)
        if prob is not None:
            click.secho(f"   Probability: {prob:.4f}", fg="green")
    else:
        click.secho(f"❌ HTTP {r.status_code}: {r.text}", fg="red")
        sys.exit(1)


# ── drift ────────────────────────────────────────────────────────────────────

@cli.command()
@click.option("--reference", "ref_path", required=True, help="Path to reference CSV")
@click.option("--current", "cur_path", required=True, help="Path to current CSV")
def drift(ref_path, cur_path):
    """Detect data drift between a reference and current dataset."""
    import pandas as pd

    for p in [ref_path, cur_path]:
        if not Path(p).exists():
            click.secho(f"❌ File not found: {p}", fg="red")
            sys.exit(1)

    click.secho("\n📡 Drift Detection", fg="cyan", bold=True)
    click.echo(f"   Reference : {ref_path}")
    click.echo(f"   Current   : {cur_path}\n")

    ref_df = pd.read_csv(ref_path)
    cur_df = pd.read_csv(cur_path)

    payload = {
        "reference": ref_df.to_dict(orient="list"),
        "current": cur_df.to_dict(orient="list"),
    }

    click.echo("📤 Sending to drift detection API…")
    try:
        r = _api_post("/drift/detect", payload, timeout=60)
    except Exception:
        _connection_error("drift")
        sys.exit(1)

    if r.status_code != 200:
        click.secho(f"❌ HTTP {r.status_code}: {r.text}", fg="red")
        sys.exit(1)

    data = r.json()
    overall = data.get("drift_detected", False)

    if overall:
        click.secho("🚨 DRIFT DETECTED", fg="red", bold=True)
    else:
        click.secho("✅ No significant drift detected", fg="green", bold=True)

    features = data.get("features", {})
    if features:
        rows = []
        for feat, info in sorted(features.items(), key=lambda x: x[1].get("score", 0), reverse=True):
            score = info.get("score", 0)
            drifted = info.get("drifted", score > 0.5)
            flag = "🔴 YES" if drifted else "🟢 NO"
            rows.append([feat, f"{score:.4f}", flag])
        click.echo(tabulate(rows, headers=["Feature", "Drift Score", "Drifted"],
                            tablefmt="rounded_outline"))


# ── admin create-user ────────────────────────────────────────────────────────

@cli.group()
def admin():
    """Admin utilities."""
    pass


@admin.command("create-user")
@click.option("--username", required=True, help="Username")
@click.option("--email", required=True, help="Email address")
@click.option("--role", default="analyst", show_default=True,
              type=click.Choice(["admin", "analyst", "viewer"]),
              help="User role")
def create_user(username, email, role):
    """Create a new platform user and print the generated API key."""
    click.secho(f"\n👤 Creating user: {username} ({role})", fg="cyan", bold=True)

    payload = {"username": username, "email": email, "role": role}

    try:
        r = _api_post("/admin/users", payload, timeout=10)
    except Exception:
        _connection_error("admin create-user")
        sys.exit(1)

    if r.status_code in (200, 201):
        data = r.json()
        api_key = data.get("api_key", "—")
        click.secho("\n✅ User created successfully!", fg="green", bold=True)
        rows = [
            ["Username", data.get("username", username)],
            ["Email",    data.get("email", email)],
            ["Role",     data.get("role", role)],
            ["User ID",  data.get("user_id", "—")],
        ]
        click.echo(tabulate(rows, tablefmt="rounded_outline"))
        click.secho(f"\n🔑 API Key: {api_key}", fg="yellow", bold=True)
        click.secho("   ⚠️  Save this key — it will NOT be shown again.", fg="yellow")
    else:
        click.secho(f"❌ HTTP {r.status_code}: {r.text}", fg="red")
        sys.exit(1)


if __name__ == "__main__":
    cli()
