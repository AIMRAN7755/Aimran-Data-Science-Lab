# R package management functions for the AIMRAN R Engine.

#' List all installed R packages with name and version.
list_packages <- function() {
  pkgs <- installed.packages()[, c("Package", "Version")]
  pkg_list <- lapply(seq_len(nrow(pkgs)), function(i) {
    list(
      name = unname(pkgs[i, "Package"]),
      version = unname(pkgs[i, "Version"])
    )
  })
  list(packages = pkg_list)
}

#' Install an R package from CRAN.
install_package <- function(package_name, version = NULL) {
  tryCatch({
    if (!is.null(version) && nzchar(version)) {
      if (requireNamespace("remotes", quietly = TRUE)) {
        remotes::install_version(package_name, version = version, repos = "https://cloud.r-project.org", quiet = TRUE)
      } else {
        install.packages(package_name, repos = "https://cloud.r-project.org", quiet = TRUE)
      }
    } else {
      install.packages(package_name, repos = "https://cloud.r-project.org", quiet = TRUE)
    }
    list(success = TRUE, package_name = package_name, message = paste(package_name, "installed successfully."))
  }, error = function(e) {
    list(success = FALSE, package_name = package_name, message = conditionMessage(e))
  })
}

#' Remove an installed R package.
uninstall_package <- function(package_name) {
  tryCatch({
    remove.packages(package_name)
    list(success = TRUE, package_name = package_name, message = paste(package_name, "removed successfully."))
  }, error = function(e) {
    list(success = FALSE, package_name = package_name, message = conditionMessage(e))
  })
}
