# Statistical analysis functions for the AIMRAN R Engine.

library(jsonlite)

#' Compute a summary profile for a CSV dataset.
#'
#' Returns row/column counts, per-column stats (type, nulls, unique, mean, sd, min, max),
#' and an overall quality score.
profile_dataset <- function(dataset_path) {
  df <- read.csv(dataset_path, stringsAsFactors = FALSE)
  row_count <- nrow(df)
  col_count <- ncol(df)

  columns <- lapply(names(df), function(col_name) {
    col <- df[[col_name]]
    null_count <- sum(is.na(col))
    null_pct <- if (row_count > 0) null_count / row_count * 100 else 0
    unique_count <- length(unique(col[!is.na(col)]))
    dtype <- if (is.numeric(col)) "numeric" else if (is.logical(col)) "boolean" else "string"

    stats <- list(
      name = col_name,
      data_type = dtype,
      null_count = null_count,
      null_percentage = round(null_pct, 2),
      unique_count = unique_count,
      mean = NULL,
      std_dev = NULL,
      min = NULL,
      max = NULL
    )

    if (is.numeric(col)) {
      vals <- col[!is.na(col)]
      if (length(vals) > 0) {
        stats$mean <- round(mean(vals), 4)
        stats$std_dev <- round(sd(vals), 4)
        stats$min <- min(vals)
        stats$max <- max(vals)
      }
    }

    stats
  })

  total_cells <- row_count * col_count
  total_nulls <- sum(sapply(columns, function(c) c$null_count))
  quality_score <- if (total_cells > 0) round(1 - total_nulls / total_cells, 4) else 1

  list(
    row_count = row_count,
    column_count = col_count,
    columns = columns,
    quality_score = quality_score
  )
}

#' Run a correlation matrix on numeric columns.
compute_correlation <- function(dataset_path, method = "pearson") {
  df <- read.csv(dataset_path, stringsAsFactors = FALSE)
  numeric_cols <- df[, sapply(df, is.numeric), drop = FALSE]

  if (ncol(numeric_cols) < 2) {
    return(list(error = "Need at least 2 numeric columns for correlation."))
  }

  cor_matrix <- round(cor(numeric_cols, use = "complete.obs", method = method), 4)
  list(
    method = method,
    columns = colnames(cor_matrix),
    matrix = as.list(as.data.frame(cor_matrix))
  )
}

#' Run a basic linear regression.
run_regression <- function(dataset_path, target_column, feature_columns) {
  df <- read.csv(dataset_path, stringsAsFactors = FALSE)
  formula_str <- paste(target_column, "~", paste(feature_columns, collapse = " + "))
  model <- lm(as.formula(formula_str), data = df)
  s <- summary(model)

  list(
    r_squared = round(s$r.squared, 4),
    adj_r_squared = round(s$adj.r.squared, 4),
    f_statistic = round(s$fstatistic[1], 4),
    p_value = round(pf(s$fstatistic[1], s$fstatistic[2], s$fstatistic[3], lower.tail = FALSE), 6),
    coefficients = lapply(seq_len(nrow(s$coefficients)), function(i) {
      list(
        name = rownames(s$coefficients)[i],
        estimate = round(s$coefficients[i, 1], 4),
        std_error = round(s$coefficients[i, 2], 4),
        t_value = round(s$coefficients[i, 3], 4),
        p_value = round(s$coefficients[i, 4], 6)
      )
    })
  )
}

#' Perform a t-test between two columns.
run_ttest <- function(dataset_path, column_a, column_b, paired = FALSE) {
  df <- read.csv(dataset_path, stringsAsFactors = FALSE)
  result <- t.test(df[[column_a]], df[[column_b]], paired = paired)
  list(
    statistic = round(result$statistic, 4),
    p_value = round(result$p.value, 6),
    confidence_interval = round(result$conf.int, 4),
    method = result$method
  )
}
