# AIMRAN Data Science Lab — UI Walkthrough & Viewing Guide

> **Quick reference for exploring the lab's user interface**

---

## 1. Getting Started — Launch the Application

### Option A: Desktop (MAUI Blazor Hybrid)
```powershell
# Terminal: Navigate to solution directory
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"

# Run the desktop app (creates native window)
dotnet run --project "AIMRAN Data Science Lab.csproj"
```

**Result:** Native MAUI window opens with Blazor UI embedded. Database stored locally.

### Option B: Web (Blazor Server)
```powershell
# Terminal: Navigate to solution directory
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"

# Run the web app
dotnet run --project AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj
```

**Result:** ASP.NET Core web server starts. Open browser to:
- `https://localhost:7220` (or port shown in console)

---

## 2. External Engine Prerequisites

Before exploring full features, start the Python and Rust engines:

### Terminal 1: Python FastAPI Engine
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\ai-engine"
pip install -r requirements.txt
python main.py
```

**Expected:** `Uvicorn running on http://0.0.0.0:8100`

### Terminal 2: Rust Actix-Web Engine
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\rust-engine"
cargo run
```

**Expected:** `Server running on http://0.0.0.0:8200`

### Terminal 3: R Plumber Engine (Optional)
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\r-engine"
Rscript plumber_api.R
```

**Expected:** `Listening on http://127.0.0.1:8300`

---

## 3. UI Navigation Map

### 3.1 Main Layout
```
┌────────────────────────────────────────────────────────────────────┐
│  AIMRAN Data Science Lab                   🔔  Local 📶          │  ← Title Bar
├────────────────────────────────────────────────────────────────────┤
│ ☰ │  Dashboard                                                     │
│   │  Projects                                                      │
│   │  Datasets                                                      │
│   │  Experiments                                                   │
│   │  Models                                                        │
│   │  Cloud                                                         │
│   │  Resources                                                     │
│   │  Administration                                                │
│                                                                     │
│                    📊 Main Content Area                             │
│                   (Pages render here)                              │
│                                                                     │
├────────────────────────────────────────────────────────────────────┤
│  Python 🟢  Rust 🟢  R 🔴    Local                                 │  ← Status Bar
└────────────────────────────────────────────────────────────────────┘
```

### 3.2 Sidebar Navigation (Left)
- **☰ Toggle** — Collapse/expand sidebar (Ctrl+B)
- **Dashboard** — Home with quick stats
- **Projects** — Project CRUD and workspace
- **Datasets** — Import, preview, manage datasets
- **Experiments** — Track ML training runs
- **Models** — Model registry and comparison
- **Cloud** → Azure integration, storage, ML workspace
- **Code** → Code Editor, Notebook, Visualizations
- **Resources** → CPU/RAM/Disk/GPU monitoring
- **Administration** → Admin dashboard, users, audit logs
- **Help** → Knowledge center, documentation

---

## 4. Page Exploration Workflow

### 📊 Dashboard (`/`)
**Location:** Click "AIMRAN Data Science Lab" logo or go to `/`

**What You'll See:**
```
┌─ Welcome Banner ────────────────────────────────────┐
│  "AIMRAN Data Science Lab — AI-Powered Multi-      │
│   Language IDE"                                     │
│                                                     │
│  [New Code 📝]  [Open Notebook 📓]                 │
└─────────────────────────────────────────────────────┘

┌─ Stat Cards (clickable) ────────────────────────────┐
│  📁 0 Projects    📊 0 Datasets    🧪 0 Experiments  │
│  🤖 0 Models      ☁️ 0 Azure Items 💾 0 Notebooks    │
└─────────────────────────────────────────────────────┘

┌─ Quick Actions Panel ───────────────────────────────┐
│  [📝 Code Editor] [📓 Notebook] [📊 Visualize]     │
│  [⬆️ Import Data] [🧪 Start Experiment]            │
└─────────────────────────────────────────────────────┘

┌─ Live Metrics ──────────────────────────────────────┐
│  CPU: 12% | Memory: 2.1/8GB | Disk: 250/500GB     │
│  GPU: Not available | Python: 🟢 Online            │
└─────────────────────────────────────────────────────┘
```

**Actions:**
- Click stat cards to jump to that section
- Click "New Code" to go to Code Editor
- Click "Open Notebook" to start notebook
- Observe live resource metrics

---

### 📁 Projects (`/projects`)
**Purpose:** Organize datasets, experiments, and models

**Workflow:**
1. Click **"+ New Project"** button
2. Enter name: `"Sales Forecasting"`
3. Optional: Select Azure workspace
4. Click **"Create"**

**View:**
```
┌─ Projects List ─────────────────────────┐
│                                         │
│ [+ New Project]                         │
│                                         │
│ ┌─ Sales Forecasting ─────────────┐   │
│ │ Owner: You                       │   │
│ │ Created: 2025-01-15              │   │
│ │ Items: 2 datasets, 1 experiment  │   │
│ │ [📂 Open] [⚙️ Settings]          │   │
│ └─────────────────────────────────────┘  │
│                                         │
└─────────────────────────────────────────┘
```

---

### 📊 Datasets (`/datasets`)
**Purpose:** Import and preview data

**Workflow:**
1. Click **"⬆️ Import Dataset"**
2. Select CSV file from your computer
3. Preview appears with column detection
4. Click **"Import"** to save

**View:**
```
┌─ Dataset Manager ───────────────────────┐
│                                         │
│ [⬆️ Import Dataset]                     │
│                                         │
│ ┌─ Sample_Data.csv ──────────────────┐ │
│ │ Format: CSV | Size: 1.2 MB         │ │
│ │ Rows: 5,432 | Columns: 12          │ │
│ │ Last Updated: Today 10:30 AM       │ │
│ │ [👁️ Preview] [🗑️ Delete]           │ │
│ └─────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

**Click "👁️ Preview":**
```
┌─ Data Viewer ──────────────────────────┐
│  ID  │ Name    │ Age │ Salary  │ Dept  │
├──────┼─────────┼─────┼─────────┼───────┤
│ 1    │ Alice   │ 28  │ $85,000 │ Sales │
│ 2    │ Bob     │ 35  │ $95,000 │ Eng   │
│ 3    │ Charlie │ 42  │ $110K   │ Mgmt  │
│ ...  │ ...     │ ... │ ...     │ ...   │
│ [◀ 1  2 3 4 5 ▶]                      │
│ [Filter] [Sort] [Export]              │
└─────────────────────────────────────────┘
```

---

### 🧹 Data Cleaning Studio (`/data-cleaning`)
**Purpose:** Clean and profile datasets

**Workflow:**
1. Select dataset from dropdown
2. Click **"+ Add Cleaning Rule"**
3. Choose rule type: `Remove Duplicates`, `Handle Missing`, `Outlier Detection`
4. Configure rule parameters
5. Click **"Apply Rule"** and preview changes

**View:**
```
┌─ Data Cleaning Studio ──────────────────┐
│                                         │
│ Dataset: [Sample_Data ▼]                │
│                                         │
│ ┌─ Applied Rules ─────────────────────┐│
│ │ 1. Remove Duplicates (5 rows removed)│
│ │ 2. Remove Outliers (IQR method)      │
│ │ 3. Fill Missing Values (mean)        │
│ │ [+ Add Rule]                         │
│ └─────────────────────────────────────┘│
│                                         │
│ ┌─ Data Profile ──────────────────────┐│
│ │ Column: Salary                       │
│ │ • Mean: $92,500                      │
│ │ • Std Dev: $15,200                   │
│ │ • Min: $45,000 | Max: $250,000      │
│ │ • Nulls: 12 (0.2%)                  │
│ └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

---

### 📝 Code Editor (`/code-editor`)
**Purpose:** Write and execute Python/R/SQL code

**View:**
```
┌─ Code Editor ───────────────────────────┐
│ Python ▼  [New] [Open] [Save] [Export]  │
│                                         │
│ ┌─────────────────────────────────────┐│
│ │ # Load and analyze dataset          │ │
│ │ import pandas as pd                 │ │
│ │                                     │ │
│ │ df = pd.read_csv('sales.csv')      │ │
│ │ print(df.describe())                │ │
│ │ print(df.corr())                    │ │
│ │                                     │ │
│ └─────────────────────────────────────┘│
│                                         │
│ [▶ Run] [Stop] [Format] [Clear]        │
│                                         │
│ ┌─ Output ──────────────────────────┐ │
│ │ >>> df.describe()                  │ │
│ │         Age      Salary             │ │
│ │ count  100.00    100.00            │ │
│ │ mean    35.42  92500.00            │ │
│ │ std     10.21  22100.00            │ │
│ │ ...                                 │ │
│ └────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

---

### 📓 Notebook (`/notebook`)
**Purpose:** Jupyter-style notebook with cells

**Workflow:**
1. Click **"+ New Cell"**
2. Choose type: `Code`, `Markdown`, `SQL`
3. Write code
4. Click **"▶ Run Cell"** to execute

**View:**
```
┌─ Notebook ──────────────────────────────┐
│ Untitled                   [Save] [...]  │
│                                         │
│ ┌─ Cell 1 [Python] ──────────────────┐│
│ │ import numpy as np                  │ │
│ │ x = np.linspace(0, 10, 100)        │ │
│ │ y = np.sin(x)                       │ │
│ │                                    │ │
│ │ [▶ Run] [⋮ ...] [Delete]           │ │
│ └─────────────────────────────────────┘│
│                                         │
│ ┌─ Output 1 ─────────────────────────┐│
│ │ [Line chart rendered here]          │ │
│ │ (matplotlib/plotly interactive)     │ │
│ └─────────────────────────────────────┘│
│                                         │
│ ┌─ Cell 2 [Markdown] ─────────────────┐│
│ │ ## Analysis Results                 │ │
│ │ ...                                  │ │
│ │ [✎ Edit] [Done]                     │ │
│ └─────────────────────────────────────┘│
│                                         │
│ [+ New Cell ▼]                          │
│                                         │
└─────────────────────────────────────────┘
```

---

### 🧪 Experiments (`/experiments`)
**Purpose:** Track ML training runs

**Workflow:**
1. Click **"+ New Experiment"**
2. Enter name: `"XGBoost v1"`
3. Select project
4. During training, metrics auto-update:
   ```python
   tracking.log_param("max_depth", 5)
   tracking.log_metric("accuracy", 0.95)
   ```

**View:**
```
┌─ Experiments ───────────────────────────┐
│ Project: Sales Forecasting              │
│                                         │
│ ┌─ XGBoost v1 ──────────────────────┐ │
│ │ Status: Running ⏳                  │ │
│ │ Created: 2 hours ago                │ │
│ │ Best Metric: Accuracy = 0.92        │ │
│ │ [📊 View Details] [⏹ Stop] [🔄 ...]│ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ Runs ─────────────────────────────┐ │
│ │ Run │ Accuracy │ Loss │ Duration  │ │
│ ├─────┼──────────┼──────┼───────────┤ │
│ │ 5   │ 0.945    │ 0.08 │ 2m 15s   │ │
│ │ 4   │ 0.923    │ 0.11 │ 2m 10s   │ │
│ │ 3   │ 0.910    │ 0.15 │ 2m 05s   │ │
│ └─────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

---

### 🤖 Models (`/models`)
**Purpose:** Model registry and deployment

**Workflow:**
1. After training, click **"Register Model"**
2. Name: `"xgboost_model_v1"`
3. Select framework and upload file
4. Add metadata (version, tags, description)

**View:**
```
┌─ Models ────────────────────────────────┐
│                                         │
│ [+ Register Model]                      │
│                                         │
│ ┌─ xgboost_model_v1 ─────────────────┐│
│ │ Framework: XGBoost                   │ │
│ │ Size: 245 MB                         │ │
│ │ Accuracy: 0.945 | F1: 0.92          │ │
│ │ Registered: 5 hours ago              │ │
│ │ [📊 Metrics] [🚀 Deploy] [📋 ...] │ │
│ └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

---

### ☁️ Azure Integration (`/azure`)
**Purpose:** Connect to Azure resources

**Configuration:**
1. Click **"⚙️ Configure Azure"**
2. Enter Subscription ID
3. Authenticate (browser popup)
4. Select Storage Account and ML Workspace

**View:**
```
┌─ Azure Integration ─────────────────────┐
│                                         │
│ [⚙️ Configure Azure]                    │
│                                         │
│ ┌─ Connected Resources ────────────────┐│
│ │ Subscription: prod-data-science      │ │
│ │ Storage: labstorageacct.blob.core    │ │
│ │ ML Workspace: aimran-workspace       │ │
│ │ Status: ✅ Connected                 │ │
│ └─────────────────────────────────────┘│
│                                         │
│ ┌─ Upload to Azure ─────────────────────┐│
│ │ Dataset: [Sales Forecast ▼]          │ │
│ │ Target: [Blob Container ▼]           │ │
│ │ [⬆️ Upload] [📝 Schedule]             │ │
│ └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

---

### 💾 Resources Monitor (`/resources`)
**Purpose:** Real-time system metrics

**View:**
```
┌─ Resources Monitor ─────────────────────┐
│                                         │
│ ┌─ CPU Usage ───────────────────────┐ │
│ │ ████████░░░░░░░░░░░░░ 32% (8/16) │ │
│ │ Core 1: 45%  Core 2: 28%          │ │
│ │ Core 3: 31%  Core 4: 25%          │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ Memory ───────────────────────────┐ │
│ │ ████████████░░░░░░░░░░░ 58% (7/12G)│ │
│ │ System: 4.2 GB | Available: 4.8 GB │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ Disk ─────────────────────────────┐ │
│ │ ███████░░░░░░░░░░░░░░░░░ 35% (175GB)│
│ │ C: 175/500 GB | D: Free             │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ GPU (NVIDIA GeForce RTX 4090) ───┐ │
│ │ ███████████░░░░░░░░░░░░░░ 55% VRAM│ │
│ │ Memory: 22GB / 24GB                 │ │
│ │ Temp: 68°C | Throttling: No         │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ [Refresh] [📊 Chart View] [⚙️ Alert...]│
│                                         │
└─────────────────────────────────────────┘
```

---

### 🛡️ Security Dashboard (`/security`)
**Purpose:** User, permissions, and audit logs

**View:**
```
┌─ Security & Compliance ─────────────────┐
│                                         │
│ ┌─ Users & Roles ───────────────────┐ │
│ │ [+ Invite User]                    │ │
│ │                                   │ │
│ │ You (Owner)      🔑 Super Admin   │ │
│ │ analyst1         📊 Data Analyst   │ │
│ │ engineer2        ⚙️ Engineer       │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ Audit Log ─────────────────────────┐│
│ │ Time      │ User │ Action │ Resource │
│ ├───────────┼──────┼────────┼──────────┤│
│ │ 10:35 AM  │ You  │ IMPORT │ Dataset  │
│ │ 09:22 AM  │ You  │ CREATE │ Exp...   │
│ │ 08:45 AM  │ You  │ DELETE │ Model    │
│ └─────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

---

### ⚙️ Administration (`/admin`)
**Purpose:** System-wide settings

**View:**
```
┌─ Admin Dashboard ───────────────────────┐
│                                         │
│ [📊 Dashboard] [👥 Users] [📁 Datasets]│
│ [💾 Storage] [📋 Audit Logs] [⚙️ Settings]
│                                         │
│ ┌─ System Health ────────────────────┐ │
│ │ Engines:                             │ │
│ │  • Python FastAPI: ✅ 8100           │ │
│ │  • Rust Actix: ✅ 8200               │ │
│ │  • R Plumber: ⚠️ 8300 (offline)     │ │
│ │ Database: ✅ Healthy                 │ │
│ │ Storage: ✅ 250GB available          │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─ Global Settings ──────────────────┐ │
│ │ Max Dataset Size: [1024 ▼ MB]      │ │
│ │ Backup Schedule: [Daily ▼] [🔄 ...]│ │
│ │ Maintenance Window: [Off ▼]         │ │
│ │ [💾 Save]                           │ │
│ └─────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

---

## 5. Feature Demonstration Checklist

Use this checklist to explore all features:

### Core Features
- [ ] **Dashboard** — View stats and quick actions
- [ ] **Projects** — Create a new project
- [ ] **Datasets** — Import a CSV file
- [ ] **Data Preview** — Explore dataset in viewer
- [ ] **Data Cleaning** — Apply cleaning rules

### Code Execution
- [ ] **Code Editor** — Write and run Python code
- [ ] **Notebook** — Create multi-cell notebook
- [ ] **Visualizations** — Create a chart

### ML Workflows
- [ ] **Experiments** — Start a training experiment
- [ ] **Models** — Register a model

### Cloud & Admin
- [ ] **Azure** — Configure Azure integration
- [ ] **Resources** — Monitor system metrics
- [ ] **Security** — Review audit logs
- [ ] **Admin** — Access admin dashboard

---

## 6. Keyboard Shortcuts

```
Ctrl+B          Toggle sidebar
Ctrl+K          Open command palette / knowledge search
Ctrl+`          Toggle terminal (if supported)
Ctrl+S          Save current work
Ctrl+Shift+P    Open settings
Esc             Close dialogs / go back
Alt+↑/↓         Navigate between pages
```

---

## 7. Troubleshooting

| Issue | Solution |
|-------|----------|
| **Engines show offline** | Start Python/Rust engines in separate terminals (see section 2) |
| **Import fails** | Ensure CSV is UTF-8 encoded and has headers |
| **Code doesn't run** | Check Python engine logs in terminal |
| **Database locked** | Close other instances of the app |
| **Azure auth fails** | Check subscription ID and network connectivity |

---

## 8. Performance Tips

- **Large datasets:** Use data cleaning to remove unnecessary columns first
- **Code execution:** Keep Python cell outputs under 1000 lines for UI responsiveness
- **Resource monitoring:** Refresh less frequently on slower machines
- **Multiple projects:** Archive inactive projects to improve navigation performance

---

## 9. Support & Documentation

- **Full Architecture:** `AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md`
- **Build & Improvements:** `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` (generated)
- **Inline Help:** Click `?` icon on any page

---

**Happy exploring! 🚀**
