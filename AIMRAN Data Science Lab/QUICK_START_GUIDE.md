# Quick Start: AIMRAN Data Science Lab on Windows 11

## ✅ Pre-requisites
- Windows 11 (10.0.17763.0 or newer)
- .NET 10 SDK installed
- Python 3.11 (for AI engine) — optional but recommended
- Rust toolchain (for resource engine) — optional but recommended

## 🚀 Running the Application

### Option 1: Direct Run (Development)
```powershell
cd "AIMRAN Data Science Lab"
dotnet run
```

### Option 2: Build Then Run
```powershell
# Build solution
dotnet build

# Run MAUI app
dotnet run --project "AIMRAN Data Science Lab\AIMRAN Data Science Lab.csproj"
```

### Option 3: Release Build (Production)
```powershell
# Publish self-contained single executable
dotnet publish -c Release -o ./publish

# Run the exe
./publish/AIMRAN\ Data\ Science\ Lab.exe
```

## 📍 Application Paths

On first launch, the app creates this structure:
```
C:\Users\{USERNAME}\AppData\Roaming\AIMRAN Lab\
├── Database\
│   └── aimran-lab.db                 # SQLite metadata database
├── Engines\
│   ├── python\                       # Python FastAPI engine
│   ├── rust\                         # Rust Actix-Web engine
│   └── r\                            # R Plumber engine
├── Logs\
│   ├── python.log
│   ├── rust.log
│   └── r.log
├── Backups\                          # Database backups
└── Config\                           # User preferences
```

## 🎮 Using the Admin Dashboard

1. **Navigate to Admin:**
   - Click "Admin" in the navigation menu or go to `/admin`

2. **Engine Management Section:**
   - Shows three engine cards (Python 🐍, Rust 🦀, R 📊)
   - Each displays:
	 - Current status (Running/Stopped)
	 - Process ID (if running)
	 - Port number
	 - Uptime, CPU, Memory metrics
	 - Error messages (if any)

3. **Engine Controls:**
   - **Start:** Launch an engine
   - **Stop:** Gracefully shut down an engine
   - **Restart:** Stop and restart an engine
   - **Logs:** View last 50 log entries

4. **Engine Settings:**
   - ☑️ Auto-start engines on app launch
   - ☑️ Auto-restart failed engines
   - Preferences saved to Windows Registry

## 🔌 Engine Ports

| Engine | Port | Protocol | Binary |
|--------|------|----------|--------|
| Python | 8100 | HTTP | `python.exe` / `python3` |
| Rust | 8200 | HTTP | `cargo run` or compiled binary |
| R | 8300 | HTTP | `Rscript` or precompiled |

## 📊 System Resources

Monitor real-time metrics on the Admin Dashboard:
- **CPU Usage:** Real-time percentage
- **Memory:** Used / Total
- **Disk:** Used / Total
- **GPU:** Usage and temperature (if available)

## 🔍 Debugging

### View Debug Output:
```powershell
# Run with debug logging
dotnet run --verbose
```

### Check Engine Logs:
- Navigate to: `C:\Users\{USERNAME}\AppData\Roaming\AIMRAN Lab\Logs\`
- Files:
  - `python.log` — Python FastAPI engine output
  - `rust.log` — Rust Actix-Web engine output
  - `r.log` — R Plumber engine output

### Common Issues:

**Engines not starting:**
- Ensure binaries are available in PATH or `AppData\AIMRAN Lab\Engines\`
- Check `.log` files in `AppData\AIMRAN Lab\Logs\`
- Verify firewall allows ports 8100, 8200, 8300

**Database locked:**
- Close all instances of the app
- Delete-and-recreate `AppData\AIMRAN Lab\Database\aimran-lab.db`

**Slow startup:**
- First launch initializes database and creates folder structure
- Subsequent launches are faster
- Check Task Manager for engine initialization

## 🛑 Stopping the Application

Press `Ctrl+C` in the terminal, or close the window.

**Graceful Shutdown:**
- All running engines are automatically stopped
- Database connections are closed
- Logs are flushed
- Preferences are saved

## 📈 Performance Tips

1. **Enable Auto-Start:** Reduces cold-start latency on next launch
2. **Monitor Resource Usage:** Adjust engine settings if CPU/memory is high
3. **Clear Old Logs:** Remove files from `Logs\` if storage is limited
4. **Database Maintenance:** App auto-handles SQLite optimization

## 🔐 Security Features

- JWT-based authentication
- Role-based access control (RBAC)
- Audit logging of all operations
- Secure secret storage
- Rate limiting on API endpoints
- Execution sandboxing for scripts

## 📱 Interface Features

- **Responsive UI:** Works on any display size/DPI
- **Dark/Light Mode:** Automatic based on Windows settings
- **High DPI Support:** PerMonitorV2 DPI awareness
- **Keyboard Navigation:** Full accessibility support

## 🆘 Getting Help

1. Check BUILD_COMPLETION_SUMMARY.md for full architecture
2. Review logs in `AppData\AIMRAN Lab\Logs\`
3. Run in debug mode to capture detailed output
4. Verify prerequisites (Python, Rust, R) are installed

---

**Status:** ✅ Ready to run. Enjoy AIMRAN Data Science Lab!
