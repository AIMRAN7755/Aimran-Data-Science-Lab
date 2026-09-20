"""Data visualization generation endpoints."""

import io
import base64

import pandas as pd
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import seaborn as sns

from fastapi import APIRouter, HTTPException
from schemas import VisualizationRequest, VisualizationResult

router = APIRouter()


@router.post("/generate", response_model=VisualizationResult)
async def generate_chart(request: VisualizationRequest):
    """Generate a chart image and return the Python code to reproduce it."""
    try:
        df = _load_dataframe(request.dataset_path)
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

    fig, ax = plt.subplots(figsize=(10, 6))
    title = request.title or f"{request.chart_type.title()} Chart"
    code_lines: list[str] = [
        "import pandas as pd",
        "import matplotlib.pyplot as plt",
        "import seaborn as sns",
        "",
        f'df = pd.read_csv("{request.dataset_path}")',
        "fig, ax = plt.subplots(figsize=(10, 6))",
    ]

    try:
        chart = request.chart_type.lower()

        if chart == "scatter":
            _require(request.x_column, request.y_column)
            ax.scatter(df[request.x_column], df[request.y_column], alpha=0.6)
            ax.set_xlabel(request.x_column)
            ax.set_ylabel(request.y_column)
            code_lines.append(
                f'ax.scatter(df["{request.x_column}"], df["{request.y_column}"], alpha=0.6)'
            )
            code_lines.append(f'ax.set_xlabel("{request.x_column}")')
            code_lines.append(f'ax.set_ylabel("{request.y_column}")')

        elif chart == "line":
            _require(request.x_column, request.y_column)
            ax.plot(df[request.x_column], df[request.y_column])
            ax.set_xlabel(request.x_column)
            ax.set_ylabel(request.y_column)
            code_lines.append(
                f'ax.plot(df["{request.x_column}"], df["{request.y_column}"])'
            )

        elif chart == "bar":
            _require(request.x_column, request.y_column)
            ax.bar(df[request.x_column].astype(str), df[request.y_column])
            ax.set_xlabel(request.x_column)
            ax.set_ylabel(request.y_column)
            plt.xticks(rotation=45, ha="right")
            code_lines.append(
                f'ax.bar(df["{request.x_column}"].astype(str), df["{request.y_column}"])'
            )

        elif chart == "histogram":
            col = request.x_column or (df.select_dtypes(include=[np.number]).columns[0] if not df.select_dtypes(include=[np.number]).empty else None)
            if col is None:
                raise ValueError("No numeric column available for histogram.")
            bins = (request.options or {}).get("bins", 30)
            ax.hist(df[col].dropna(), bins=bins, edgecolor="black", alpha=0.7)
            ax.set_xlabel(col)
            ax.set_ylabel("Frequency")
            code_lines.append(f'ax.hist(df["{col}"].dropna(), bins={bins}, edgecolor="black", alpha=0.7)')

        elif chart == "box":
            cols = request.columns or list(df.select_dtypes(include=[np.number]).columns[:6])
            df[cols].boxplot(ax=ax)
            code_lines.append(f'df[{cols}].boxplot(ax=ax)')

        elif chart == "heatmap":
            numeric_df = df.select_dtypes(include=[np.number])
            if numeric_df.empty:
                raise ValueError("No numeric columns for heatmap.")
            sns.heatmap(numeric_df.corr(), annot=True, fmt=".2f", cmap="coolwarm", ax=ax)
            code_lines.append('sns.heatmap(df.select_dtypes(include="number").corr(), annot=True, fmt=".2f", cmap="coolwarm", ax=ax)')

        elif chart == "correlation":
            numeric_df = df.select_dtypes(include=[np.number])
            if numeric_df.empty:
                raise ValueError("No numeric columns for correlation matrix.")
            corr = numeric_df.corr()
            mask = np.triu(np.ones_like(corr, dtype=bool))
            sns.heatmap(corr, mask=mask, annot=True, fmt=".2f", cmap="RdBu_r", center=0, ax=ax)
            code_lines.append('corr = df.select_dtypes(include="number").corr()')
            code_lines.append("mask = np.triu(np.ones_like(corr, dtype=bool))")
            code_lines.append('sns.heatmap(corr, mask=mask, annot=True, fmt=".2f", cmap="RdBu_r", center=0, ax=ax)')

        else:
            raise ValueError(f"Unsupported chart type: {chart}")

        ax.set_title(title)
        code_lines.append(f'ax.set_title("{title}")')
        code_lines.append("plt.tight_layout()")
        code_lines.append("plt.show()")

        fig.tight_layout()
        buf = io.BytesIO()
        fig.savefig(buf, format="png", dpi=100, bbox_inches="tight")
        buf.seek(0)
        b64 = base64.b64encode(buf.read()).decode()
        plt.close(fig)

        return VisualizationResult(
            chart_type=request.chart_type,
            figure_base64=b64,
            python_code="\n".join(code_lines),
        )

    except Exception as e:
        plt.close(fig)
        raise HTTPException(status_code=400, detail=str(e))


def _require(*args: str | None) -> None:
    for a in args:
        if not a:
            raise ValueError("Required column parameter is missing.")


def _load_dataframe(path: str) -> pd.DataFrame:
    if path.endswith(".csv"):
        return pd.read_csv(path)
    elif path.endswith(".parquet"):
        return pd.read_parquet(path)
    elif path.endswith(".json"):
        return pd.read_json(path)
    elif path.endswith((".xlsx", ".xls")):
        return pd.read_excel(path)
    else:
        raise ValueError(f"Unsupported format: {path}")
