//! Axum request handlers for all engine endpoints.

use axum::http::StatusCode;
use axum::Json;
use sha2::{Digest, Sha256};
use std::io::Read;
use std::time::Instant;

use crate::models::*;
use crate::resource_monitor;

// ── Health ──────────────────────────────────────────────────────────────────

pub async fn health() -> Json<HealthResponse> {
    Json(HealthResponse {
        status: "healthy".into(),
        engine: "rust-resource".into(),
        version: "0.1.0".into(),
    })
}

// ── Resources ───────────────────────────────────────────────────────────────

pub async fn resource_snapshot() -> Json<ResourceSnapshot> {
    Json(resource_monitor::get_snapshot())
}

pub async fn detect_gpu() -> Result<Json<GpuInfo>, StatusCode> {
    resource_monitor::detect_gpu()
        .map(Json)
        .ok_or(StatusCode::NOT_FOUND)
}

// ── Delta ───────────────────────────────────────────────────────────────────

pub async fn compute_delta(
    Json(req): Json<DeltaComputeRequest>,
) -> Result<Json<DeltaComputeResponse>, (StatusCode, String)> {
    let start = Instant::now();

    let base_data = std::fs::read(&req.base_path).map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;
    let target_data = std::fs::read(&req.target_path).map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let base_hash = sha256_hex(&base_data);
    let target_hash = sha256_hex(&target_data);

    // Simple XOR-based delta (placeholder for production bsdiff)
    let delta: Vec<u8> = base_data
        .iter()
        .zip(target_data.iter())
        .map(|(a, b)| a ^ b)
        .collect();

    let delta_b64 = base64_encode(&delta);
    let elapsed = start.elapsed();

    Ok(Json(DeltaComputeResponse {
        delta_data: delta_b64,
        original_size_bytes: base_data.len() as u64,
        delta_size_bytes: delta.len() as u64,
        compression_ratio: if base_data.is_empty() {
            0.0
        } else {
            delta.len() as f64 / base_data.len() as f64
        },
        base_hash,
        target_hash,
        compute_duration_ms: elapsed.as_millis() as u64,
    }))
}

pub async fn apply_delta(
    Json(req): Json<DeltaApplyRequest>,
) -> Result<Json<DeltaApplyResponse>, (StatusCode, String)> {
    let start = Instant::now();

    let base_data = std::fs::read(&req.base_path).map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;
    let delta_data = base64_decode(&req.delta_data).map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let output: Vec<u8> = base_data
        .iter()
        .zip(delta_data.iter())
        .map(|(a, d)| a ^ d)
        .collect();

    std::fs::write(&req.output_path, &output).map_err(|e| (StatusCode::INTERNAL_SERVER_ERROR, e.to_string()))?;

    let result_hash = sha256_hex(&output);
    let elapsed = start.elapsed();

    Ok(Json(DeltaApplyResponse {
        output_path: req.output_path,
        result_hash,
        output_size_bytes: output.len() as u64,
        apply_duration_ms: elapsed.as_millis() as u64,
    }))
}

// ── Hash ────────────────────────────────────────────────────────────────────

pub async fn compute_hash(
    Json(req): Json<HashRequest>,
) -> Result<Json<HashResponse>, (StatusCode, String)> {
    let mut file =
        std::fs::File::open(&req.file_path).map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;
    let mut hasher = Sha256::new();
    let mut buffer = [0u8; 8192];
    loop {
        let n = file.read(&mut buffer).map_err(|e| (StatusCode::INTERNAL_SERVER_ERROR, e.to_string()))?;
        if n == 0 {
            break;
        }
        hasher.update(&buffer[..n]);
    }
    Ok(Json(HashResponse {
        hash: hex::encode(hasher.finalize()),
    }))
}

// ── File I/O ────────────────────────────────────────────────────────────────

pub async fn parse_csv(
    Json(req): Json<CsvParseRequest>,
) -> Result<Json<CsvParseResponse>, (StatusCode, String)> {
    let start = Instant::now();
    let mut reader = csv::Reader::from_path(&req.file_path)
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let headers: Vec<String> = reader
        .headers()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?
        .iter()
        .map(|h| h.to_string())
        .collect();

    let max_rows = req.max_rows.unwrap_or(0);
    let mut rows: Vec<Vec<String>> = Vec::new();
    let mut total = 0usize;

    for result in reader.records() {
        let record = result.map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;
        total += 1;
        if max_rows == 0 || rows.len() < max_rows {
            rows.push(record.iter().map(|f| f.to_string()).collect());
        }
    }

    let elapsed = start.elapsed();
    Ok(Json(CsvParseResponse {
        columns: headers,
        rows,
        total_row_count: total,
        parse_duration_ms: elapsed.as_millis() as u64,
    }))
}

pub async fn convert_file(
    Json(req): Json<ConvertRequest>,
) -> Result<Json<ConvertResponse>, (StatusCode, String)> {
    // Placeholder: real conversion between CSV/Parquet/JSON
    std::fs::copy(&req.source_path, &req.output_path)
        .map_err(|e| (StatusCode::INTERNAL_SERVER_ERROR, e.to_string()))?;
    Ok(Json(ConvertResponse {
        output_path: req.output_path,
    }))
}

// ── Helpers ─────────────────────────────────────────────────────────────────

fn sha256_hex(data: &[u8]) -> String {
    let mut hasher = Sha256::new();
    hasher.update(data);
    hex::encode(hasher.finalize())
}

fn base64_encode(data: &[u8]) -> String {
    use std::io::Write;
    let mut buf = Vec::new();
    {
        let mut enc = base64_writer(&mut buf);
        enc.write_all(data).unwrap();
    }
    String::from_utf8(buf).unwrap()
}

fn base64_decode(s: &str) -> Result<Vec<u8>, String> {
    // Simple manual base64 decode (or use a crate in production)
    base64_manual_decode(s)
}

// Minimal base64 implementation to avoid extra crate dependency
fn base64_writer(out: &mut Vec<u8>) -> Base64Writer<'_> {
    Base64Writer { out }
}

struct Base64Writer<'a> {
    out: &'a mut Vec<u8>,
}

impl<'a> std::io::Write for Base64Writer<'a> {
    fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
        const TABLE: &[u8] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for chunk in buf.chunks(3) {
            let b0 = chunk[0] as u32;
            let b1 = if chunk.len() > 1 { chunk[1] as u32 } else { 0 };
            let b2 = if chunk.len() > 2 { chunk[2] as u32 } else { 0 };
            let n = (b0 << 16) | (b1 << 8) | b2;
            self.out.push(TABLE[((n >> 18) & 0x3F) as usize]);
            self.out.push(TABLE[((n >> 12) & 0x3F) as usize]);
            if chunk.len() > 1 {
                self.out.push(TABLE[((n >> 6) & 0x3F) as usize]);
            } else {
                self.out.push(b'=');
            }
            if chunk.len() > 2 {
                self.out.push(TABLE[(n & 0x3F) as usize]);
            } else {
                self.out.push(b'=');
            }
        }
        Ok(buf.len())
    }
    fn flush(&mut self) -> std::io::Result<()> {
        Ok(())
    }
}

fn base64_manual_decode(s: &str) -> Result<Vec<u8>, String> {
    const TABLE: &[u8] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    let mut out = Vec::new();
    let bytes: Vec<u8> = s.bytes().filter(|b| *b != b'\n' && *b != b'\r').collect();
    for chunk in bytes.chunks(4) {
        if chunk.len() < 2 {
            break;
        }
        let val = |c: u8| -> Result<u32, String> {
            if c == b'=' { return Ok(0); }
            TABLE.iter().position(|&t| t == c).map(|p| p as u32).ok_or_else(|| format!("Invalid base64 char: {c}"))
        };
        let a = val(chunk[0])?;
        let b = val(chunk[1])?;
        let c = if chunk.len() > 2 { val(chunk[2])? } else { 0 };
        let d = if chunk.len() > 3 { val(chunk[3])? } else { 0 };
        let n = (a << 18) | (b << 12) | (c << 6) | d;
        out.push(((n >> 16) & 0xFF) as u8);
        if chunk.len() > 2 && chunk[2] != b'=' { out.push(((n >> 8) & 0xFF) as u8); }
        if chunk.len() > 3 && chunk[3] != b'=' { out.push((n & 0xFF) as u8); }
    }
    Ok(out)
}
