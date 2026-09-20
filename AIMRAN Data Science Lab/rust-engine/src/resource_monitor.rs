//! System resource monitoring using the `sysinfo` crate.

use crate::models::{GpuInfo, ResourceSnapshot};
use sysinfo::System;

pub fn get_snapshot() -> ResourceSnapshot {
    let mut sys = System::new_all();
    sys.refresh_all();

    let cpu_usage = sys.global_cpu_usage() as f64;
    let cpu_count = sys.cpus().len();
    let cpu_freq = sys.cpus().first().map(|c| c.frequency() as f64).unwrap_or(0.0);

    let total_mem = sys.total_memory();
    let used_mem = sys.used_memory();
    let available_mem = sys.available_memory();

    let disks = sysinfo::Disks::new_with_refreshed_list();
    let (disk_total, disk_free) = disks
        .list()
        .iter()
        .fold((0u64, 0u64), |(t, f), d| {
            (t + d.total_space(), f + d.available_space())
        });
    let disk_used = disk_total.saturating_sub(disk_free);

    ResourceSnapshot {
        cpu_usage_percent: cpu_usage,
        cpu_core_count: cpu_count,
        cpu_frequency_mhz: cpu_freq,
        memory_total_bytes: total_mem,
        memory_used_bytes: used_mem,
        memory_available_bytes: available_mem,
        disk_total_bytes: disk_total,
        disk_used_bytes: disk_used,
        disk_free_bytes: disk_free,
        gpu: None, // GPU detection requires platform-specific APIs
        timestamp: chrono_now(),
    }
}

pub fn detect_gpu() -> Option<GpuInfo> {
    // Placeholder: real implementation would use NVML bindings
    None
}

fn chrono_now() -> String {
    // Simple ISO 8601 timestamp without pulling in chrono crate
    let d = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap_or_default();
    format!("{}Z", d.as_secs())
}
