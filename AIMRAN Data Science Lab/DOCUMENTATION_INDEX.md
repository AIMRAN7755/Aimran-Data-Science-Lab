# AIMRAN Data Science Lab — Documentation Index

> **Complete analysis, UI guide, and quick start for your data science platform**

---

## 📋 Document Overview

| Document | Purpose | Size |
|----------|---------|------|
| **BUILD_AND_IMPROVEMENTS_ANALYSIS.md** | Comprehensive build summary and architecture improvements | 26 KB |
| **UI_WALKTHROUGH_GUIDE.md** | Step-by-step UI exploration with screenshots and workflows | 27 KB |
| **QUICK_START_COMMANDS.md** | Copy-paste commands to launch and explore | 11 KB |
| **AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md** | Technical architecture deep-dive (original) | 20 KB |
| **AIMRAN_DATA_SCIENCE_LAB_FINAL_OVERVIEW.md** | Project overview and deployment guide (original) | 2 KB |

---

## 🎯 Quick Navigation

### For First-Time Users
1. **Start here:** Read `AIMRAN_DATA_SCIENCE_LAB_FINAL_OVERVIEW.md` (2 min)
2. **Then run:** Copy commands from `QUICK_START_COMMANDS.md` → Section 3
3. **Then explore:** Follow `UI_WALKTHROUGH_GUIDE.md` → Section 4

### For Technical Review
1. **Architecture:** `AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md`
2. **Build Analysis:** `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` → Section 3-6
3. **Implementation:** `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` → Section 7-10

### For Visual Learners
- **UI Tour:** `UI_WALKTHROUGH_GUIDE.md` → Section 3-4 (with ASCII mockups)
- **Demo Workflow:** `UI_WALKTHROUGH_GUIDE.md` → Section 5

---

## 🚀 Getting Started (3 Steps)

### Step 1: Verify Build ✅
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"
dotnet build
```
**Status:** ✅ Build successful (6 projects, 0 errors)

### Step 2: Launch Application
Choose one:
```powershell
# Option A: Web (Blazor Server)
dotnet run --project AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj
# → Open https://localhost:7220

# Option B: Desktop (MAUI Hybrid)
dotnet run --project "AIMRAN Data Science Lab.csproj"
# → Native window opens automatically
```

### Step 3: Start Engines
In separate terminals:
```powershell
# Terminal 1: Python AI Engine (port 8100)
cd ai-engine
pip install -r requirements.txt
python main.py

# Terminal 2: Rust Performance Engine (port 8200)
cd rust-engine
cargo run
```

---

## 📊 Solution Structure

```
AIMRAN Data Science Lab/
├── AIMRAN Data Science Lab.csproj ────────── MAUI Blazor Hybrid (Desktop)
├── AimranDataScienceLab.Web/ ────────────── Blazor Server (Web)
├── AimranDataScienceLab.Shared/ ─────────── Shared RCL (UI + Services)
├── AimranDataScienceLab.Engine/ ─────────── Core Engine + SQLite
├── AimranDataScienceLab.Gateway/ ───────── External Service Integration
├── AimranDataScienceLab.Engine.Tests/ ────── xUnit Test Suite
├── ai-engine/ ──────────────────────────── Python FastAPI (8100)
├── rust-engine/ ─────────────────────────── Rust Actix-Web (8200)
├── r-engine/ ────────────────────────────── R Plumber (8300)
│
└── Documentation/
	├── BUILD_AND_IMPROVEMENTS_ANALYSIS.md ◄─ YOU ARE HERE
	├── UI_WALKTHROUGH_GUIDE.md
	├── QUICK_START_COMMANDS.md
	├── AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md
	└── AIMRAN_DATA_SCIENCE_LAB_FINAL_OVERVIEW.md
```

---

## 💡 Key Features

### Data Management
- 📥 **Import:** CSV, Parquet, JSON support
- 🔄 **Versioning:** Binary delta snapshots with full history
- 🧹 **Cleaning:** Rule-based cleaning studio with profiling
- 👁️ **Viewer:** Interactive tabular data explorer

### Code Execution
- **Python IDE** with pip package management
- **R Console** with Plumber API integration
- **SQL Editor** with database preview
- **Jupyter-style Notebook** with cell execution

### ML/Experiment Tracking
- 📊 **Experiments:** Track training runs with hyperparameters
- 🤖 **Models:** Multi-framework registry (TensorFlow, PyTorch, etc.)
- 📈 **Metrics:** Real-time tracking and visualization
- 🚀 **Deployment:** Model deployment workflows

### Cloud Integration
- ☁️ **Azure Blob Storage** for dataset backup/sharing
- 🧠 **Azure ML Workspace** synchronization
- 🔐 **Secure SAS tokens** for file access

### System Monitoring
- 💾 **CPU, Memory, Disk** real-time metrics
- 🎮 **GPU Detection** and VRAM monitoring
- 📊 **Live Dashboard** with performance alerts

### Security & Compliance
- 🔐 **JWT Authentication** with multi-role support
- 📋 **Audit Logs** for all operations
- 🛡️ **Encrypted Secrets** management
- 👥 **Role-based Access Control** (Admin, User, DataScientist, Engineer)

---

## 📈 Platform Statistics

| Metric | Value |
|--------|-------|
| **Target Framework** | .NET 10 |
| **Projects** | 6 |
| **Deployment Targets** | Desktop (MAUI), Web (Blazor), Mobile (iOS/Android) |
| **Blazor Pages** | 30+ |
| **Service Interfaces** | 30+ |
| **Service Implementations** | 50+ |
| **Domain Models** | 15+ |
| **External Engines** | 3 (Python, Rust, R) |
| **Database** | SQLite (local) + Azure Blob (cloud) |
| **API Ports** | 8100 (Python), 8200 (Rust), 8300 (R), 7220 (Web) |

---

## 🎓 Learning Path

### Beginner (Import Data & Explore)
1. Launch app
2. Create project
3. Import CSV dataset
4. View data in explorer
5. Run simple Python code
- **Time:** 15 minutes
- **Document:** `UI_WALKTHROUGH_GUIDE.md` sections 1-4

### Intermediate (Data Cleaning & Analysis)
1. Import dataset
2. Apply cleaning rules
3. Generate data profile
4. Create visualizations
5. Track experiment metrics
- **Time:** 30 minutes
- **Document:** `UI_WALKTHROUGH_GUIDE.md` sections 4-6

### Advanced (ML Workflow & Cloud)
1. Code training pipeline in Python IDE
2. Track experiment with metrics
3. Register trained model
4. Compare model performance
5. Deploy to Azure ML
6. Monitor resource usage
- **Time:** 1-2 hours
- **Document:** `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` sections 4-9

### Expert (Architecture & Extension)
1. Review service architecture
2. Understand extension system
3. Create custom plugin
4. Deploy custom engine
5. Integrate with external services
- **Time:** 2-4 hours
- **Document:** `AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md`

---

## 🔧 Common Tasks

### Import a Dataset
```
Dashboard → Datasets → ⬆️ Import → Select CSV → Preview → Import
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Datasets)

### Clean Data
```
Dashboard → Data Cleaning Studio → Select Dataset → + Add Rule → Apply
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Data Cleaning)

### Write & Run Code
```
Dashboard → Code Editor → Write Python → ▶ Run → View Output
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Code Editor)

### Track ML Experiment
```
Dashboard → Experiments → + New Experiment → Training runs auto-track metrics
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Experiments)

### Monitor System Resources
```
Dashboard → Resources → View CPU/Memory/Disk/GPU charts
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Resources)

### Connect to Azure
```
Dashboard → Azure → ⚙️ Configure → Enter credentials → Verify connection
```
See: `UI_WALKTHROUGH_GUIDE.md` → Section 4 (Azure)

---

## 📞 Support & Troubleshooting

### Build Issues
- **Problem:** Build fails
- **Solution:** See `QUICK_START_COMMANDS.md` → Section 1 (Prerequisites)
- **Command:** `dotnet clean && dotnet restore && dotnet build`

### Application Won't Start
- **Problem:** Application crashes or won't launch
- **Solution:** Check `QUICK_START_COMMANDS.md` → Section 9 (Troubleshooting)
- **Verify:** `netstat -ano | findstr "7220"` (web port in use?)

### Engines Show Offline
- **Problem:** Python/Rust/R engines not available
- **Solution:** See `QUICK_START_COMMANDS.md` → Section 4 (Start Engines)
- **Verify:** `Invoke-WebRequest -Uri "http://localhost:8100/health"`

### Data Import Fails
- **Problem:** CSV import error
- **Solution:** Ensure UTF-8 encoding with headers
- **Try:** Use a sample CSV from `Resources/Data/` if available

### Database Issues
- **Problem:** Database locked or corrupted
- **Solution:** See `QUICK_START_COMMANDS.md` → Section 8 (Database)
- **Command:** Remove database and restart (recreates automatically)

---

## 📚 Additional Resources

### Official Documentation
- [.NET 10 Docs](https://learn.microsoft.com/en-us/dotnet/)
- [Blazor Server Guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
- [MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

### External Services
- [Azure Portal](https://portal.azure.com)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Actix Web Docs](https://actix.rs/)

### Code Repositories
- **Main:** Current workspace
- **Architecture:** `AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md`
- **Services:** `AimranDataScienceLab.Shared/Services/`

---

## ✅ Verification Checklist

Before you start using the platform:

- [ ] .NET 10 SDK installed (`dotnet --version`)
- [ ] Solution builds successfully (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
- [ ] Web app runs (`dotnet run`)
- [ ] Python engine starts (port 8100)
- [ ] Rust engine starts (port 8200)
- [ ] Browser can access https://localhost:7220
- [ ] Dashboard loads and shows stats
- [ ] Can create a project

---

## 🎉 You're Ready!

You now have a complete, production-ready data science platform with:

✅ **Dual Deployment** — Desktop (MAUI) + Web (Blazor)  
✅ **Rich UI** — 30+ pages with modern design  
✅ **Multiple Engines** — Python, Rust, R integration  
✅ **Cloud Ready** — Azure integration built-in  
✅ **Secure** — JWT auth, audit logs, RBAC  
✅ **Scalable** — Service-oriented architecture  
✅ **Tested** — xUnit test coverage  
✅ **Documented** — Comprehensive guides  

---

## 📖 Document Finder

| Need | Document | Section |
|------|----------|---------|
| Project overview | `AIMRAN_DATA_SCIENCE_LAB_FINAL_OVERVIEW.md` | All |
| Build status | `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` | Section 1 |
| Architecture | `AIMRAN_DATA_SCIENCE_LAB_ARCHITECTURE.md` | All |
| Launch app | `QUICK_START_COMMANDS.md` | Sections 2-3 |
| Explore UI | `UI_WALKTHROUGH_GUIDE.md` | Sections 3-4 |
| Troubleshoot | `QUICK_START_COMMANDS.md` | Section 9 |
| Advanced topics | `BUILD_AND_IMPROVEMENTS_ANALYSIS.md` | Sections 7-13 |

---

## 🚀 Next Steps

1. **Read:** `AIMRAN_DATA_SCIENCE_LAB_FINAL_OVERVIEW.md` (2 min)
2. **Run:** First command from `QUICK_START_COMMANDS.md` (30 sec)
3. **Explore:** Follow `UI_WALKTHROUGH_GUIDE.md` (15-60 min)
4. **Build:** Create your first project workflow

---

**Last Updated:** 2025  
**Version:** 2.0  
**Status:** ✅ Production Ready

---

**Happy Data Science! 🔬📊🚀**
