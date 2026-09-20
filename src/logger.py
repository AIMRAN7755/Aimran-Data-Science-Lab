"""Main logger setup for Aimran Data Science Lab"""

import logging
import logging.handlers
from pathlib import Path
from src.config import config

# Create logs directory if it doesn't exist
Path("logs").mkdir(exist_ok=True)


def setup_logger(name: str, log_file: str = "logs/app.log") -> logging.Logger:
    """Setup application logger"""

    logger = logging.getLogger(name)
    logger.setLevel(getattr(logging, config.LOG_LEVEL))

    # File handler
    file_handler = logging.handlers.RotatingFileHandler(
        log_file,
        maxBytes=10485760,  # 10MB
        backupCount=10
    )
    file_handler.setLevel(getattr(logging, config.LOG_LEVEL))

    # Console handler
    console_handler = logging.StreamHandler()
    console_handler.setLevel(getattr(logging, config.LOG_LEVEL))

    # Formatter
    formatter = logging.Formatter(
        '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S'
    )

    file_handler.setFormatter(formatter)
    console_handler.setFormatter(formatter)

    logger.addHandler(file_handler)
    logger.addHandler(console_handler)

    return logger


# Create main logger
logger = setup_logger(__name__)
