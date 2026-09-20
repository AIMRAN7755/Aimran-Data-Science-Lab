"""Python code execution and package management endpoints."""

import io
import sys
import time
import base64
import subprocess
import traceback
from contextlib import redirect_stdout, redirect_stderr

from fastapi import APIRouter, HTTPException
from schemas import (
    CodeExecuteRequest,
    CodeExecuteResult,
    PackageInfo,
    PackageListResult,
    PackageInstallRequest,
    PackageActionResult,
)

router = APIRouter()


@router.post("/execute", response_model=CodeExecuteResult)
async def execute_code(request: CodeExecuteRequest):
    """Execute a Python code snippet and capture output."""
    stdout_buf = io.StringIO()
    stderr_buf = io.StringIO()
    figure_b64: str | None = None
    has_figure = False
    start = time.monotonic()

    try:
        # Patch matplotlib to render to buffer instead of screen
        _patch_code = (
            "import matplotlib\n"
            "matplotlib.use('Agg')\n"
            "import matplotlib.pyplot as plt\n"
        )
        full_code = _patch_code + request.code

        exec_globals: dict = {"__builtins__": __builtins__}

        with redirect_stdout(stdout_buf), redirect_stderr(stderr_buf):
            exec(compile(full_code, "<aimran-cell>", "exec"), exec_globals)

        # Check if a matplotlib figure was created
        try:
            import matplotlib.pyplot as _plt

            figs = [
                m.canvas.figure
                for m in _plt._pylab_helpers.Gcf.get_all_fig_managers()
            ]
            if figs:
                buf = io.BytesIO()
                figs[-1].savefig(buf, format="png", bbox_inches="tight", dpi=100)
                buf.seek(0)
                figure_b64 = base64.b64encode(buf.read()).decode()
                has_figure = True
                _plt.close("all")
        except Exception:
            pass

        elapsed = time.monotonic() - start
        return CodeExecuteResult(
            stdout=stdout_buf.getvalue(),
            stderr=stderr_buf.getvalue(),
            exit_code=0,
            execution_time_seconds=round(elapsed, 4),
            has_figure=has_figure,
            figure_base64=figure_b64,
        )
    except Exception:
        elapsed = time.monotonic() - start
        return CodeExecuteResult(
            stdout=stdout_buf.getvalue(),
            stderr=traceback.format_exc(),
            exit_code=1,
            execution_time_seconds=round(elapsed, 4),
        )


@router.get("/packages", response_model=PackageListResult)
async def list_packages():
    """List all installed pip packages."""
    try:
        result = subprocess.run(
            [sys.executable, "-m", "pip", "list", "--format=json"],
            capture_output=True,
            text=True,
            timeout=30,
        )
        if result.returncode != 0:
            raise HTTPException(status_code=500, detail=result.stderr)

        import json

        raw = json.loads(result.stdout)
        packages = [
            PackageInfo(name=p["name"], version=p["version"]) for p in raw
        ]
        return PackageListResult(packages=packages)
    except subprocess.TimeoutExpired:
        raise HTTPException(status_code=504, detail="pip list timed out")


@router.post("/packages/install", response_model=PackageActionResult)
async def install_package(request: PackageInstallRequest):
    """Install a pip package."""
    pkg = (
        f"{request.package_name}=={request.version}"
        if request.version
        else request.package_name
    )
    try:
        result = subprocess.run(
            [sys.executable, "-m", "pip", "install", pkg],
            capture_output=True,
            text=True,
            timeout=120,
        )
        return PackageActionResult(
            success=result.returncode == 0,
            package_name=request.package_name,
            message=result.stdout if result.returncode == 0 else result.stderr,
        )
    except subprocess.TimeoutExpired:
        return PackageActionResult(
            success=False,
            package_name=request.package_name,
            message="Installation timed out.",
        )


@router.post("/packages/uninstall", response_model=PackageActionResult)
async def uninstall_package(request: PackageInstallRequest):
    """Uninstall a pip package."""
    try:
        result = subprocess.run(
            [sys.executable, "-m", "pip", "uninstall", "-y", request.package_name],
            capture_output=True,
            text=True,
            timeout=60,
        )
        return PackageActionResult(
            success=result.returncode == 0,
            package_name=request.package_name,
            message=result.stdout if result.returncode == 0 else result.stderr,
        )
    except subprocess.TimeoutExpired:
        return PackageActionResult(
            success=False,
            package_name=request.package_name,
            message="Uninstall timed out.",
        )
