# Bootstrap script to install required R packages for the AIMRAN R Engine.
# Run this once before starting the Plumber API:
#   Rscript install_packages.R

required_packages <- c(
  "plumber",
  "jsonlite",
  "dplyr",
  "tidyr",
  "data.table",
  "ggplot2",
  "plotly",
  "stats",
  "forecast",
  "caret",
  "randomForest",
  "xgboost",
  "base64enc"
)

install_if_missing <- function(pkg) {
  if (!requireNamespace(pkg, quietly = TRUE)) {
    message(paste("Installing", pkg, "..."))
    install.packages(pkg, repos = "https://cloud.r-project.org")
  } else {
    message(paste(pkg, "already installed."))
  }
}

invisible(lapply(required_packages, install_if_missing))

message("All required packages installed.")
