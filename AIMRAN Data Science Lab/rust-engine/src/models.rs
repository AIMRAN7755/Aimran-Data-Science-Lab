//! Serde models for request/response payloads.

use serde::{Deserialize, Serialize};

// ── Health ──────────────────────────────────────────────────────────────────

#[derive(Serialize)]
pub struct HealthResponse {
    pub status: String,
    pub engine: String,
    pub version: String,
}

// ── Resources ───────────────────────────────────────────────────────────────

#[derive(Serialize)]
pub struct ResourceSnapshot {
    pub cpu_usage_percent: f64,
    pub cpu_core_count: usize,
    pub cpu_frequency_mhz: f64,
    pub memory_total_bytes: u64,
    pub memory_used_bytes: u64,
    pub memory_available_bytes: u64,
    pub disk_total_bytes: u64,
    pub disk_used_bytes: u64,
    pub disk_free_bytes: u64,
    pub gpu: Option<GpuInfo>,
    pub timestamp: String,
}

#[derive(Serialize)]
pub struct GpuInfo {
    pub name: String,
    pub usage_percent: f64,
    pub memory_total_bytes: u64,
    pub memory_used_bytes: u64,
    pub temperature_celsius: f64,
    pub cuda_available: bool,
}

// ── Delta ───────────────────────────────────────────────────────────────────

#[derive(Deserialize)]
pub struct DeltaComputeRequest {
    pub base_path: String,
    pub target_path: String,
}

#[derive(Serialize)]
pub struct DeltaComputeResponse {
    pub delta_data: String, // base64
    pub original_size_bytes: u64,
    pub delta_size_bytes: u64,
    pub compression_ratio: f64,
    pub base_hash: String,
    pub target_hash: String,
    pub compute_duration_ms: u64,
}

#[derive(Deserialize)]
pub struct DeltaApplyRequest {
    pub base_path: String,
    pub delta_data: String, // base64
    pub output_path: String,
}

#[derive(Serialize)]
pub struct DeltaApplyResponse {
    pub output_path: String,
    pub result_hash: String,
    pub output_size_bytes: u64,
    pub apply_duration_ms: u64,
}

// ── Hash ────────────────────────────────────────────────────────────────────

#[derive(Deserialize)]
pub struct HashRequest {
    pub file_path: String,
}

#[derive(Serialize)]
pub struct HashResponse {
    pub hash: String,
}

// ── File I/O ────────────────────────────────────────────────────────────────

#[derive(Deserialize)]
pub struct CsvParseRequest {
    pub file_path: String,
    pub max_rows: Option<usize>,
}

#[derive(Serialize)]
pub struct CsvParseResponse {
    pub columns: Vec<String>,
    pub rows: Vec<Vec<String>>,
    pub total_row_count: usize,
    pub parse_duration_ms: u64,
}

#[derive(Deserialize)]
pub struct ConvertRequest {
    pub source_path: String,
    pub target_format: String,
    pub output_path: String,
}

#[derive(Serialize)]
pub struct ConvertResponse {
    pub output_path: String,
}
