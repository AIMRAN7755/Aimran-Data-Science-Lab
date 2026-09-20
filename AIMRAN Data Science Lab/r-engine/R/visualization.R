# Visualization functions for the AIMRAN R Engine.

library(ggplot2)
library(base64enc)

#' Generate a ggplot2 chart and return it as a Base64-encoded PNG.
#'
#' @param dataset_path Path to the CSV file.
#' @param chart_type One of: scatter, line, bar, histogram, boxplot, heatmap, density.
#' @param x_column Column for the X axis (optional for some chart types).
#' @param y_column Column for the Y axis (optional for some chart types).
#' @param title Chart title.
#' @return A list with chart_type, figure_base64, and r_code.
generate_chart <- function(dataset_path, chart_type, x_column = NULL, y_column = NULL, title = NULL) {
  df <- read.csv(dataset_path, stringsAsFactors = FALSE)

  p <- switch(chart_type,
    "scatter" = {
      ggplot(df, aes(x = .data[[x_column]], y = .data[[y_column]])) +
        geom_point() +
        geom_smooth(method = "lm", se = FALSE, color = "blue")
    },
    "line" = {
      ggplot(df, aes(x = .data[[x_column]], y = .data[[y_column]])) +
        geom_line()
    },
    "bar" = {
      ggplot(df, aes(x = .data[[x_column]])) +
        geom_bar()
    },
    "histogram" = {
      ggplot(df, aes(x = .data[[x_column]])) +
        geom_histogram(bins = 30, fill = "steelblue", color = "white")
    },
    "boxplot" = {
      ggplot(df, aes(x = .data[[x_column]], y = .data[[y_column]])) +
        geom_boxplot()
    },
    "density" = {
      ggplot(df, aes(x = .data[[x_column]])) +
        geom_density(fill = "steelblue", alpha = 0.5)
    },
    {
      ggplot(df, aes(x = .data[[x_column]], y = .data[[y_column]])) +
        geom_point()
    }
  )

  if (!is.null(title)) {
    p <- p + ggtitle(title)
  }

  p <- p + theme_minimal()

  # Render to a temporary PNG and encode as Base64.
  tmp <- tempfile(fileext = ".png")
  ggsave(tmp, plot = p, width = 8, height = 6, dpi = 150)
  encoded <- base64encode(tmp)
  unlink(tmp)

  # Reconstruct the R code that produced this chart for display in the IDE.
  r_code <- paste0(
    "library(ggplot2)\n",
    "df <- read.csv(\"", dataset_path, "\")\n",
    "ggplot(df, aes(x = ", x_column, ", y = ", y_column, ")) +\n",
    "  geom_", chart_type, "() +\n",
    "  theme_minimal()"
  )

  list(
    chart_type = chart_type,
    figure_base64 = encoded,
    r_code = r_code
  )
}
