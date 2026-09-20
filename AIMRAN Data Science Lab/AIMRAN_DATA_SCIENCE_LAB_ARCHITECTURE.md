# AIMRAN Data Science Lab — Architecture & Functionality Guide

> **Version 1.0** · .NET 10 · Blazor Hybrid (MAUI) + Blazor Server (Web)

---

## 1  Solution Overview

AIMRAN Data Science Lab is a **full-stack data science workbench** that runs as
both a **native desktop app** (MAUI Blazor Hybrid — Windows / macOS / Android / iOS)
and a **web application** (ASP.NET Core Blazor Server). A single Razor Class
Library (Shared RCL) supplies every UI component and service, so both deployment
targets share the identical user experience and business logic.

```
┌─────────────────────────────────────────────────────────────────────┐
│                     AIMRAN Data Science Lab                        │
│                                                                     │
│  ┌──────────────────┐        ┌──────────────────────┐              │
│  │  MAUI Blazor      │        │  ASP.NET Blazor       │              │
│  │  Hybrid (Desktop) │        │  Server (Web)         │              │
│  │                    │        │                        │              │
│  │  MainPage.xaml ────┼────────┤  App.razor             │              │
│  │  BlazorWebView     │        │  MapRazorComponents    │              │
│  └────────┬───────────┘        └────────┬───────────────┘              │
│           │                             │                              │
│           └──────────┬──────────────────┘                              │
│                      ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │              AimranDataScienceLab.Shared (RCL)              │      │
│  │                                                              │      │
│  │   Components/Pages   ·   Services   ·   Models   ·  Engine  │      │
│  └──────────┬─────────────────┬───────────────────┬─────────────┘      │
│             │                 │                   │                     │
│     ┌───────▼──────┐  ┌──────▼──────┐  ┌─────────▼──────────┐        │
│     │ Engine Core  │  │  Gateway    │  │  Engine.Tests      │        │
│     │ (SQLite DB)  │  │ (HTTP)      │  │  (xUnit)           │        │
│     └──────────────┘  └──────┬──────┘  └────────────────────┘        │
│                              │                                        │
│            ┌─────────────────┼─────────────────┐                     │
│            ▼                                   ▼                     │
│  ┌──────────────────┐              ┌──────────────────────┐          │
│  │  Python FastAPI   │              │  Rust Actix-Web      │          │
│  │  AI Engine        │              │  Resource Engine     │          │
│  │  (port 8100)      │              │  (port 8200)         │          │
│  └──────────────────┘              └──────────────────────┘          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2  Project Map

| Project | SDK | Purpose |
|---|---|---|
| **AIMRAN Data Science Lab** | `Microsoft.NET.Sdk.Razor` + MAUI | Native desktop/mobile host |
| **AimranDataScienceLab.Web** | `Microsoft.NET.Sdk.Web` | Blazor Server web host |
| **AimranDataScienceLab.Shared** | `Microsoft.NET.Sdk.Razor` (RCL) | All UI components, services, models |
| **AimranDataScienceLab.Engine** | `Microsoft.NET.Sdk` | SQLite persistence & engine options |
| **AimranDataScienceLab.Gateway** | `Microsoft.NET.Sdk` | HTTP clients for Python/Rust engines |
| **AimranDataScienceLab.Engine.Tests** | `Microsoft.NET.Sdk` | xUnit tests for Engine core |
| **ai-engine/** | Python (FastAPI) | ML training, profiling, cleaning AI |
| **rust-engine/** | Rust (Actix-Web) | Resource monitoring, delta computation |

---

## 3  Functionality Matrix — What Each Host Handles

### 3.1  Shared Functionality (both MAUI and Web)

All business logic lives in the **Shared RCL** and is registered via
`builder.Services.AddAimranDataScienceLab()`. Both hosts get the identical
service graph:

| Feature Area | Page Route | Service Interface | Capabilities |
|---|---|---|---|
| **Dashboard** | `/` | `IAimranEngine` | Live stats, resource meters, quick actions |
| **Project Management** | `/projects` | `IProjectService` | Create/archive projects, Azure workspace binding |
| **Project Workspace** | `/projects/{id}` | `IProjectService` | Dataset/experiment/model linking per project |
| **Dataset Manager** | `/datasets` | `IDatasetService` | Import CSV/Parquet/JSON, preview, metadata |
| **Dataset Versioning** | `/versioning` | `IDatasetVersionService`, `IDeltaEngine` | Binary delta snapshots, version history, diff |
| **Data Cleaning Studio** | `/data-cleaning` | `IDataCleaningService`, `ICleaningRuleService` | Rule-based cleaning, profiling, outlier detection |
| **Experiment Tracker** | `/experiments` | `IExperimentService` | Create experiments, track runs, hyperparameters |
| **Model Lab** | `/models` | `IModelService` | Register models, compare metrics, deploy |
| **AI Engine Manager** | `/ai-engine` | `IGatewayManager` | Health checks, start/stop engines, stream logs |
| **Plugin Hub** | `/plugins` | `IPluginManagerService` | pip/cargo package management, model registry |
| **Resource Monitor** | `/resources` | `IResourceMonitorService` | CPU/RAM/disk/GPU live metrics |
| **Azure Integration** | `/azure` | `IAzureConfigService`, `IAzureStorageService`, `IAzureMlService` | Blob upload, ML workspace, cloud experiments |

### 3.2  MAUI-Specific Capabilities

| Capability | Implementation |
|---|---|
| **Native file dialogs** | `FilePicker.PickAsync()` for dataset import |
| **Offline-first SQLite** | DB stored in `FileSystem.AppDataDirectory` |
| **System tray / notifications** | MAUI platform APIs |
| **GPU detection** | Direct hardware enumeration via Rust engine |
| **Cross-platform** | Windows, macOS, Android, iOS from single codebase |

### 3.3  Web-Specific Capabilities

| Capability | Implementation |
|---|---|
| **Interactive Server rendering** | `@rendermode InteractiveServer` on all routes |
| **Multi-user access** | SignalR circuits per browser session |
| **HTTPS / reverse proxy** | Standard ASP.NET Core middleware pipeline |
| **Server-side resources** | Full CPU/RAM access for large datasets |
| **No install required** | Browser-based access to the full workbench |

---

## 4  Complete Conceptual Flowchart

### 4.1  End-to-End Data Science Pipeline

```
 ┌─────────────────────────────────────────────────────────────────┐
 │                        USER INTERACTION                         │
 │         (Browser for Web · Native window for MAUI)              │
 └──────────────────────────────┬──────────────────────────────────┘
                                │
                    ┌───────────▼───────────┐
                    │   Blazor Component    │
                    │   (Shared RCL Page)   │
                    └───────────┬───────────┘
                                │  @inject services
                    ┌───────────▼───────────┐
                    │   IAimranEngine       │  ◄── Façade API
                    │   (AimranEngine)      │
                    └───────────┬───────────┘
                                │
          ┌─────────────────────┼─────────────────────┐
          │                     │                     │
 ┌────────▼────────┐  ┌────────▼────────┐  ┌────────▼────────┐
 │ Core Services   │  │ Data Pipeline   │  │ Cloud Services  │
 │                  │  │                  │  │                  │
 │ IProjectService │  │ IDataProfiling  │  │ IAzureConfig    │
 │ IDatasetService │  │ IOutlierDetect  │  │ IAzureStorage   │
 │ IExperimentSvc  │  │ IDataCleaning   │  │ IAzureMlService │
 │ IModelService   │  │ ICleaningRules  │  │                  │
 │ IResourceMonSvc │  │ IDatasetVersion │  │                  │
 │ IPluginManager  │  │ IDeltaEngine    │  │                  │
 └────────┬────────┘  └────────┬────────┘  └────────┬────────┘
          │                     │                     │
          ▼                     ▼                     ▼
 ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
 │ SQLite Layer    │  │ Local Impls     │  │ Azure REST APIs │
 │ (Engine.Data)   │  │ (in-memory +    │  │ (Blob, ML       │
 │                  │  │  CSV parsing)   │  │  Workspace)     │
 └─────────────────┘  └─────────────────┘  └─────────────────┘

          ┌──────────────── Gateway Layer ────────────────┐
          │                                                │
          ▼                                                ▼
 ┌─────────────────────┐              ┌─────────────────────────┐
 │  Python AI Engine   │              │  Rust Resource Engine   │
 │  (FastAPI :8100)    │              │  (Actix-Web :8200)      │
 │                      │              │                          │
 │  • Train models     │              │  • CPU/RAM/GPU metrics  │
 │  • Evaluate models  │              │  • Binary delta compute │
 │  • Run predictions  │              │  • High-perf CSV parse  │
 │  • Profile datasets │              │  • File hash (SHA-256)  │
 │  • Suggest cleaning │              │  • Format conversion    │
 │  • Stream metrics   │              │  • Stream resources     │
 └─────────────────────┘              └─────────────────────────┘
```

### 4.2  Engine Mode Flow

```
EngineMode selected
       │
       ├── LocalOnly ──────► All C# in-process, in-memory storage
       │
       ├── LocalFirst ─────► SQLite persistence, C# processing (DEFAULT)
       │
       ├── CloudFirst ─────► Prefer Azure ML/Storage when configured
       │
       └── Hybrid ─────────► AI work → Python engine
                              Resource work → Rust engine
                              Everything else → C# in-process
```

### 4.3  Service Gateway Architecture

```
┌──────────────────────────────────────────────────────┐
│                  IGatewayManager                      │
│  • CheckHealthAsync()   • StartEnginesAsync()        │
│  • StopEnginesAsync()   • GetProcessStates()         │
│  • GetEngineLogs()      • PythonEngine / RustEngine  │
└────────────┬─────────────────────┬───────────────────┘
             │                     │
  ┌──────────▼──────────┐  ┌──────▼──────────────────┐
  │ IPythonEngineClient │  │ IRustEngineClient       │
  │                      │  │                          │
  │  Experiments         │  │  Resource monitoring    │
  │  Model training      │  │  Delta computation     │
  │  Predictions         │  │  CSV parsing            │
  │  Data profiling      │  │  File hashing           │
  │  Cleaning suggest    │  │  Format conversion     │
  └──────────────────────┘  └──────────────────────────┘
         │                           │
  ┌──────▼──────────────┐  ┌────────▼────────────────┐
  │ HttpClient pipeline │  │ HttpClient pipeline     │
  │  └─ AuthToken       │  │  └─ AuthToken           │
  │  └─ Retry (3x)     │  │  └─ Retry (3x)         │
  └─────────────────────┘  └─────────────────────────┘
```

---

## 5  DI Registration — Single Entry Point

Both hosts register the entire service graph with one call:

```csharp
// MauiProgram.cs
builder.Services.AddAimranDataScienceLab();

// Web Program.cs
builder.Services.AddAimranDataScienceLab();
```

The `AddAimranDataScienceLab()` extension method (in
`DataScienceLabServiceExtensions.cs`) registers:

| Layer | Services |
|---|---|
| **Engine Core** | `IAimranEngine`, `EngineOptions` |
| **SQLite Persistence** | `SqliteConnectionFactory`, `DatabaseInitializer` |
| **Core CRUD** | `IProjectService`, `IDatasetService`, `IExperimentService`, `IModelService` |
| **Resource Monitoring** | `IResourceMonitorService` |
| **Data Cleaning** | `IDataProfilingService`, `IOutlierDetectionService`, `IDataCleaningService`, `ICleaningRuleService` |
| **Dataset Versioning** | `IDatasetStorageProvider`, `IDeltaEngine`, `IDatasetVersionService` |
| **Azure Cloud** | `IAzureConfigService`, `IAzureStorageService`, `IAzureMlService` |
| **Service Gateway** | `IGatewayManager`, `IPythonEngineClient`, `IRustEngineClient`, `EngineProcessManager` |
| **Plugin Manager** | `IPluginManagerService` |

---

## 6  Data Model Summary

```
Project ──┬── DatasetIds[]  ─── Dataset ──── DatasetVersion[]
           │                                       └── DeltaData
           ├── ExperimentIds[] ── Experiment ── ExperimentRun[]
           │                                       └── Metrics{}
           └── ModelIds[] ────── MlModel
                                    ├── Hyperparameters{}
                                    ├── PerformanceMetrics{}
                                    └── DeploymentEndpoint?

DataProfile ──── ColumnProfile[] ──── OutlierResult
CleaningRule ──── CleaningOperation[]
ResourceMetrics ── CpuMetrics · MemoryMetrics · DiskMetrics · GpuMetrics?
AzureConfig ── SubscriptionId · ResourceGroup · WorkspaceName
```

---

## 7  Page-by-Page Feature Guide

### `/` — Dashboard
- Real-time project/dataset/experiment/model counts
- Live CPU, RAM, disk usage bars
- Quick-action buttons to create project, import dataset
- Engine snapshot validation

### `/projects` — Project Management
- CRUD operations with status (Active/Archived/Completed)
- Tag-based filtering
- Azure workspace connection per project
- Drill into ProjectWorkspace for linked assets

### `/projects/{id}` — Project Workspace
- Unified view of datasets, experiments, models for one project
- Link/unlink assets
- Run experiments within project scope

### `/datasets` — Dataset Manager
- Import from local file (CSV, Parquet, JSON, Excel)
- Row/column counts, size, format detection
- Preview first N rows
- Version badge and storage location indicator

### `/versioning` — Dataset Versioning
- Create point-in-time snapshots
- Binary delta compression between versions
- SHA-256 integrity verification
- Diff visualization between versions

### `/data-cleaning` — Data Cleaning Studio
- Column-level profiling (mean, median, std, null%, unique)
- Outlier detection (IQR, Z-score methods)
- Rule-based cleaning pipeline (fill-null, trim, regex replace)
- Before/after comparison view

### `/experiments` — Experiment Tracker
- Create experiments tied to projects and datasets
- Track multiple runs with hyperparameters
- Compare metrics across runs
- Submit to Python AI Engine for training

### `/models` — Model Lab
- Register trained models with metadata
- Version management
- Performance metric comparison charts
- Deploy to Azure ML endpoint

### `/ai-engine` — AI Engine Manager
- Health status for Python and Rust engines
- Start/stop engine processes
- Stream real-time logs
- Process state monitoring

### `/plugins` — Plugin Hub
- Browse installed Python packages (pip)
- Browse installed Rust crates
- Install/upgrade/uninstall packages
- Model version registry

### `/resources` — Resource Monitor
- Live CPU utilization per core
- RAM used/available/cached
- Disk I/O and space
- GPU detection and utilization (via Rust engine)

### `/azure` — Azure Integration
- Configure subscription, resource group, workspace
- Test connectivity
- Upload datasets to Azure Blob Storage
- Submit experiments to Azure ML
- Register models in Azure ML

---

## 8  Build & Run

### Web (immediate — no external engines required)
```bash
cd AimranDataScienceLab.Web
dotnet run
# → https://localhost:5001
```

### MAUI Desktop (Windows)
```bash
dotnet build "AIMRAN Data Science Lab.csproj" -f net10.0-windows10.0.19041.0
# Run from Visual Studio or:
dotnet run -f net10.0-windows10.0.19041.0
```

### Run Tests
```bash
dotnet test AimranDataScienceLab.Engine.Tests
```

### Optional: Start External Engines
```bash
# Python AI Engine
cd ai-engine && pip install -r requirements.txt && uvicorn main:app --port 8100

# Rust Resource Engine
cd rust-engine && cargo run -- --port 8200
```

---

## 9  Technology Stack

| Layer | Technology |
|---|---|
| UI Framework | Blazor (.razor components) |
| Desktop Host | .NET MAUI Blazor Hybrid |
| Web Host | ASP.NET Core Blazor Server |
| Database | SQLite via Microsoft.Data.Sqlite |
| AI/ML Engine | Python FastAPI + scikit-learn/PyTorch |
| Performance Engine | Rust Actix-Web |
| Cloud | Azure Blob Storage, Azure ML |
| Testing | xUnit |
| Target Framework | .NET 10 |
