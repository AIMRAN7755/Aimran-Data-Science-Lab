# AIMRAN Data Science Lab — R Engine (Plumber API)
# Start with:  Rscript -e "plumber::pr_run(plumber::pr('plumber_api.R'), host='0.0.0.0', port=9000)"

library(plumber)
library(jsonlite)

source("R/statistics.R")
source("R/visualization.R")
source("R/packages.R")

# ── Health ────────────────────────────────────────────────────────────────────

#* Health check
#* @get /health
function(res) {
  res$setHeader("X-Engine-Version", "0.1.0")
  list(status = "healthy", engine = "R Plumber Engine", version = "0.1.0")
}

# ── Code Execution ───────────────────────────────────────────────────────────

#* Execute an R code snippet and return stdout/stderr.
#* @post /api/code/execute
function(req, res) {
  body <- fromJSON(req$postBody)
  code <- body$code
  timeout <- if (!is.null(body$timeout_seconds)) body$timeout_seconds else 30

  stdout_capture <- ""
  stderr_capture <- ""
  exit_code <- 0
  has_figure <- FALSE
  figure_base64 <- NULL
  start_time <- proc.time()

  tryCatch({
    # Capture a plot if one is produced.
    tmp_plot <- tempfile(fileext = ".png")
    png(tmp_plot, width = 800, height = 600)

    output <- tryCatch({
      capture.output({
        result <- eval(parse(text = code), envir = new.env(parent = globalenv()))
        if (!is.null(result)) print(result)
      })
    }, error = function(e) {
      exit_code <<- 1
      stderr_capture <<- conditionMessage(e)
      character(0)
    })

    dev.off()

    stdout_capture <- paste(output, collapse = "\n")

    if (file.exists(tmp_plot) && file.info(tmp_plot)$size > 0) {
      has_figure <- TRUE
      figure_base64 <- base64enc::base64encode(tmp_plot)
    }
    unlink(tmp_plot)

  }, error = function(e) {
    try(dev.off(), silent = TRUE)
    exit_code <<- 1
    stderr_capture <<- conditionMessage(e)
  })

  elapsed <- (proc.time() - start_time)["elapsed"]

  list(
    stdout = stdout_capture,
    stderr = stderr_capture,
    exit_code = exit_code,
    execution_time_seconds = round(as.numeric(elapsed), 3),
    has_figure = has_figure,
    figure_base64 = figure_base64
  )
}

# ── Package Management ───────────────────────────────────────────────────────

#* List installed R packages.
#* @get /api/code/packages
function() {
  list_packages()
}

#* Install an R package.
#* @post /api/code/packages/install
function(req) {
  body <- fromJSON(req$postBody)
  install_package(body$package_name, body$version)
}

#* Uninstall an R package.
#* @post /api/code/packages/uninstall
function(req) {
  body <- fromJSON(req$postBody)
  uninstall_package(body$package_name)
}

# ── Visualization ────────────────────────────────────────────────────────────

#* Generate a ggplot2 chart and return Base64 PNG.
#* @post /api/visualization/generate
function(req) {
  body <- fromJSON(req$postBody)
  generate_chart(
    dataset_path = body$dataset_path,
    chart_type   = body$chart_type,
    x_column     = body$x_column,
    y_column     = body$y_column,
    title        = body$title
  )
}

# ── Statistical Analysis ─────────────────────────────────────────────────────

#* Profile a dataset (row/column counts, per-column statistics).
#* @post /api/profiling/profile
function(req) {
  body <- fromJSON(req$postBody)
  profile_dataset(body$dataset_path)
}

#* Compute a correlation matrix.
#* @post /api/statistics/correlation
function(req) {
  body <- fromJSON(req$postBody)
  method <- if (!is.null(body$method)) body$method else "pearson"
  compute_correlation(body$dataset_path, method)
}

#* Run linear regression.
#* @post /api/statistics/regression
function(req) {
  body <- fromJSON(req$postBody)
  run_regression(body$dataset_path, body$target_column, body$feature_columns)
}

#* Run a t-test between two columns.
#* @post /api/statistics/ttest
function(req) {
  body <- fromJSON(req$postBody)
  paired <- if (!is.null(body$paired)) body$paired else FALSE
  run_ttest(body$dataset_path, body$column_a, body$column_b, paired)
}
