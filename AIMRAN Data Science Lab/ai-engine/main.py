"""
AIMRAN Data Science Lab — Python AI Engine
FastAPI application that serves as the ML computation backend.
"""

from fastapi import FastAPI
from contextlib import asynccontextmanager
from routers import experiments, models, profiling, cleaning, code_execution, visualization


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Startup / shutdown lifecycle."""
    print("🐍 AIMRAN Python AI Engine starting...")
    yield
    print("🐍 AIMRAN Python AI Engine shutting down.")


app = FastAPI(
    title="AIMRAN AI Engine",
    version="0.1.0",
    lifespan=lifespan,
)

# ── Health ────────────────────────────────────────────────────────────────────
@app.get("/health")
async def health():
    return {"status": "healthy", "engine": "python-ai", "version": "0.1.0"}


# ── Routers ───────────────────────────────────────────────────────────────────
app.include_router(experiments.router, prefix="/api/experiments", tags=["experiments"])
app.include_router(models.router, prefix="/api/models", tags=["models"])
app.include_router(profiling.router, prefix="/api/profiling", tags=["profiling"])
app.include_router(cleaning.router, prefix="/api/cleaning", tags=["cleaning"])
app.include_router(code_execution.router, prefix="/api/code", tags=["code-execution"])
app.include_router(visualization.router, prefix="/api/visualization", tags=["visualization"])


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8100, reload=True)
