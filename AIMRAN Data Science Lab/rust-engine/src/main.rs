//! AIMRAN Data Science Lab — Rust Resource Engine
//! High-performance backend for resource monitoring, delta computation, and file I/O.

mod handlers;
mod models;
mod resource_monitor;

use axum::{routing::{get, post}, Router};
use tower_http::cors::CorsLayer;
use tracing_subscriber::EnvFilter;

#[tokio::main]
async fn main() {
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::try_from_default_env().unwrap_or_else(|_| "info".into()))
        .init();

    tracing::info!("🦀 AIMRAN Rust Resource Engine starting on :8200");

    let app = Router::new()
        // Health
        .route("/health", get(handlers::health))
        // Resource monitoring
        .route("/api/resources/snapshot", get(handlers::resource_snapshot))
        .route("/api/resources/gpu", get(handlers::detect_gpu))
        // Delta computation
        .route("/api/delta/compute", post(handlers::compute_delta))
        .route("/api/delta/apply", post(handlers::apply_delta))
        // Utilities
        .route("/api/util/hash", post(handlers::compute_hash))
        // File I/O
        .route("/api/io/parse-csv", post(handlers::parse_csv))
        .route("/api/io/convert", post(handlers::convert_file))
        .layer(CorsLayer::permissive());

    let listener = tokio::net::TcpListener::bind("0.0.0.0:8200").await.unwrap();
    axum::serve(listener, app).await.unwrap();
}
