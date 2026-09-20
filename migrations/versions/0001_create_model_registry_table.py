"""create model_registry table

Revision ID: 0001
Revises:
Create Date: 2026-07-26 17:00:00.000000
"""

from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


revision: str = "0001"
down_revision: Union[str, None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.create_table(
        "model_registry",
        sa.Column("model_id", sa.String(255), primary_key=True),
        sa.Column("model_type", sa.String(255), nullable=False),
        sa.Column("version", sa.Integer(), nullable=False, server_default=sa.text("1")),
        sa.Column("task", sa.String(64), nullable=False, server_default=sa.text("'classification'")),
        sa.Column("metrics_json", sa.Text(), nullable=True),
        sa.Column("tags_json", sa.Text(), nullable=True),
        sa.Column("model_blob", sa.LargeBinary(), nullable=True),
        sa.Column("is_champion", sa.Boolean(), nullable=False, server_default=sa.text("false")),
        sa.Column("created_at", sa.Float(), nullable=False),
    )
    op.create_index("ix_model_registry_champion", "model_registry", ["model_id", "is_champion"])
    op.create_index("ix_model_registry_created", "model_registry", ["created_at"])


def downgrade() -> None:
    op.drop_index("ix_model_registry_created")
    op.drop_index("ix_model_registry_champion")
    op.drop_table("model_registry")
