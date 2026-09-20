# AIMRAN Data Science Lab — Quick Start Commands

> **Copy-paste ready commands to launch and explore the lab**

---

## 1. Prerequisites

### Install .NET 10 SDK
```powershell
# Check current version
dotnet --version

# Download from https://dotnet.microsoft.com/en-us/download/dotnet/10.0
# Or via chocolatey:
choco install dotnet-sdk-10.0
```

### Install Python 3.9+
```powershell
python --version
pip --upgrade pip
```

### Install Rust (for performance engine)
```powershell
# Download from https://www.rust-lang.org/tools/install
# Or via scoop:
scoop install rustup
rustup toolchain install stable
```

### Install R (for statistics engine - optional)
```powershell
# Download from https://cran.r-project.org/
# Or via chocolatey:
choco install r.project
```

---

## 2. Initial Setup (One-Time)

### Clone/Navigate to Repository
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"
```

### Restore NuGet Packages
```powershell
dotnet restore
```

### Build Solution
```powershell
dotnet build
```

### Run Tests
```powershell
dotnet test AimranDataScienceLab.Engine.Tests
```

---

## 3. Launch Application (Choose One)

### Option A: Launch Web Version (Blazor Server)

**Terminal 1: Main Application**
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"
dotnet run --project AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj
```

**Wait for:** `Now listening on: https://localhost:7220`

**Then open browser:**
```
https://localhost:7220
```

---

### Option B: Launch Desktop Version (MAUI Hybrid)

**Terminal 1: Main Application**
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"
dotnet run --project "AIMRAN Data Science Lab.csproj"
```

**Result:** Native MAUI window opens automatically

---

## 4. Start External Engines (in separate terminals)

### Terminal 2: Python FastAPI Engine

```powershell
# Navigate to Python engine
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\ai-engine"

# Install dependencies
pip install -r requirements.txt

# Run engine
python main.py
```

**Expected Output:**
```
INFO:     Uvicorn running on http://0.0.0.0:8100 [Press CTRL+C to quit]
```

---

### Terminal 3: Rust Performance Engine

```powershell
# Navigate to Rust engine
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\rust-engine"

# Build and run (first time will take ~2-3 min)
cargo run

# Subsequent runs (cached):
cargo run
```

**Expected Output:**
```
2025-XX-XX HH:MM:SS [INFO] Server running on http://0.0.0.0:8200
```

---

### Terminal 4: R Plumber Engine (Optional)

```powershell
# Navigate to R engine
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab\r-engine"

# Install packages (one-time)
R -e "source('R/packages.R')"

# Run engine
Rscript plumber_api.R
```

**Expected Output:**
```
2025-XX-XX HH:MM:SS [INFO] Listening on http://127.0.0.1:8300
```

---

## 5. Verify All Engines Are Running

```powershell
# Test each engine endpoint
Invoke-WebRequest -Uri "http://localhost:8100/health" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8200/health" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8300/health" -UseBasicParsing
```

**Expected Output:** HTTP 200 with JSON response

---

## 6. First-Time User Actions

### Create a Project
1. Open app at `https://localhost:7220` (or launch MAUI app)
2. Click **"Projects"** in sidebar
3. Click **"+ New Project"**
4. Enter name: `"My First Project"`
5. Click **"Create"**

### Import Sample Dataset
1. Click **"Datasets"** in sidebar
2. Click **"⬆️ Import Dataset"**
3. Select a CSV file from your computer
4. Click **"Preview"** to verify
5. Click **"Import"**

### Clean Data
1. Click **"Data Cleaning Studio"**
2. Select your imported dataset
3. Add rules: Remove Duplicates, Handle Missing Values
4. Click **"Apply"**

### Write Code
1. Click **"Code Editor"**
2. Paste sample code:
```python
import pandas as pd
import numpy as np

# Create sample data
df = pd.DataFrame({
	'A': np.random.rand(100),
	'B': np.random.rand(100),
	'C': np.random.rand(100)
})

print("Summary Statistics:")
print(df.describe())
```
3. Click **"▶ Run"**

### Track Experiment
1. Click **"Experiments"**
2. Click **"+ New Experiment"**
3. Name it: `"Test Experiment"`
4. During execution, engine logs metrics automatically

---

## 7. Development Workflow Commands

### Run with Debug Output
```powershell
dotnet run --project AimranDataScienceLab.Web --configuration Debug
```

### Build for Release
```powershell
# Web build
dotnet build AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj -c Release

# Desktop build (Windows)
dotnet build "AIMRAN Data Science Lab.csproj" -c Release -f net10.0-windows10.0.19041.0

# Desktop build (macOS)
dotnet build "AIMRAN Data Science Lab.csproj" -c Release -f net10.0-maccatalyst
```

### Publish for Distribution
```powershell
# Web publish
dotnet publish AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj `
  -c Release -o .\publish\web

# Desktop publish (Windows)
dotnet publish "AIMRAN Data Science Lab.csproj" `
  -c Release -f net10.0-windows10.0.19041.0 -o .\publish\desktop
```

---

## 8. Database Management

### Reset Database (Fresh Start)
```powershell
# Remove SQLite database
Remove-Item "$ENV:APPDATA\AIMRAN Lab\*.db" -Force -ErrorAction SilentlyContinue

# Or for web app
Remove-Item "AimranDataScienceLab.Web\wwwroot\data\*.db" -Force -ErrorAction SilentlyContinue

# Restart app (database recreates automatically)
```

### Backup Database
```powershell
# Copy database file
$dbPath = "$ENV:APPDATA\AIMRAN Lab\aimran.db"
Copy-Item $dbPath ".\backup\aimran_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
```

---

## 9. Troubleshooting Commands

### Check Ports Are Available
```powershell
# Check if ports are in use
netstat -ano | findstr "8100\|8200\|8300\|7220"

# Kill process on port (e.g., 8100)
Get-Process | Where-Object { $_.MainWindowTitle -like "*8100*" } | Stop-Process
```

### View Application Logs
```powershell
# Web app logs (if running in console)
Get-Content ".\logs\*.log" -Tail 50

# Python engine logs
Get-Content ".\ai-engine\logs\*.log" -Tail 50
```

### Verify Environment Setup
```powershell
# Check .NET version
dotnet --version

# Check Python
python --version
pip --version

# Check Rust
rustc --version
cargo --version

# Check R (if installed)
R --version
```

### Clear NuGet Cache
```powershell
dotnet nuget locals all --clear
dotnet restore
```

---

## 10. All-in-One Launch Script

### PowerShell Script: `launch-lab.ps1`

```powershell
# Save this as launch-lab.ps1 in the solution directory

param(
	[switch]$Desktop,
	[switch]$Web,
	[switch]$WithEngines
)

$SolutionPath = "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"
cd $SolutionPath

Write-Host "🚀 AIMRAN Data Science Lab Launcher" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Default to web if neither option specified
if (-not $Desktop -and -not $Web) { $Web = $true }

# Build if needed
Write-Host "📦 Ensuring build is up to date..." -ForegroundColor Yellow
dotnet build -q

# Start main application
if ($Web) {
	Write-Host "🌐 Starting Blazor Server (Web)..." -ForegroundColor Green
	Start-Process powershell -ArgumentList "cd '$SolutionPath'; dotnet run --project AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj" -NoNewWindow
}
elseif ($Desktop) {
	Write-Host "🖥️  Starting MAUI Desktop App..." -ForegroundColor Green
	Start-Process powershell -ArgumentList "cd '$SolutionPath'; dotnet run --project 'AIMRAN Data Science Lab.csproj'" -NoNewWindow
}

# Start engines if requested
if ($WithEngines) {
	Write-Host "⚙️  Starting Python FastAPI Engine (8100)..." -ForegroundColor Green
	Start-Process powershell -ArgumentList "cd '$SolutionPath\ai-engine'; pip install -q -r requirements.txt; python main.py" -NoNewWindow

	Write-Host "⚙️  Starting Rust Engine (8200)..." -ForegroundColor Green
	Start-Process powershell -ArgumentList "cd '$SolutionPath\rust-engine'; cargo run -q" -NoNewWindow
}

Write-Host ""
Write-Host "✅ Applications launched!" -ForegroundColor Green

if ($Web) {
	Write-Host "📖 Open your browser: https://localhost:7220" -ForegroundColor Cyan
}
if ($WithEngines) {
	Write-Host "✅ Python Engine: http://localhost:8100" -ForegroundColor Cyan
	Write-Host "✅ Rust Engine: http://localhost:8200" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "💡 Tip: Check the other terminal windows for engine output" -ForegroundColor Yellow
```

**Run:**
```powershell
# Launch web version with all engines
.\launch-lab.ps1 -Web -WithEngines

# Launch desktop version
.\launch-lab.ps1 -Desktop -WithEngines
```

---

## 11. Common Command Reference

```powershell
# Navigate to project
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"

# Restore & build
dotnet restore
dotnet build

# Run tests
dotnet test

# Start web app (port 7220)
dotnet run --project AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj

# Start desktop app
dotnet run --project "AIMRAN Data Science Lab.csproj"

# Install Python deps
pip install -r ai-engine\requirements.txt

# Run Python engine (port 8100)
cd ai-engine
python main.py

# Run Rust engine (port 8200)
cd rust-engine
cargo run

# Clean build artifacts
dotnet clean

# Publish for release
dotnet publish -c Release
```

---

## 12. URLs & Ports Reference

| Component | URL | Purpose |
|-----------|-----|---------|
| **Web UI** | `https://localhost:7220` | Main Blazor Server app |
| **Python Engine** | `http://localhost:8100` | AI/ML FastAPI service |
| **Rust Engine** | `http://localhost:8200` | Performance monitoring |
| **R Engine** | `http://localhost:8300` | Statistics & visualization |

---

## 13. Next Steps

1. ✅ Run build → `dotnet build`
2. ✅ Review comprehensive analysis → See `BUILD_AND_IMPROVEMENTS_ANALYSIS.md`
3. ✅ Explore UI → See `UI_WALKTHROUGH_GUIDE.md`
4. ✅ Launch application → Follow section 3 above
5. ✅ Start with a simple workflow: Import dataset → Clean data → Create experiment

---

**Happy coding! 🎉**

