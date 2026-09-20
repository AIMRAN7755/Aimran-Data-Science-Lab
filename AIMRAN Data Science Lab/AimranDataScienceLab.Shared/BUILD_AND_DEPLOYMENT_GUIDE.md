# AIMRAN Data Science Lab - Build & Deployment Guide
## Enterprise Release Process v1.0

---

## 📋 Prerequisites

### System Requirements
- **OS**: Windows 10 or later (Server 2019+)
- **.NET Runtime**: .NET 10.0 SDK
- **RAM**: Minimum 4GB (8GB+ recommended)
- **Storage**: 2GB minimum for application and data
- **Network**: Internet connection for initial setup

### Required Tools
```powershell
# Install .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Verify installation
dotnet --version

# Expected output: 10.0.x (or latest)
```

---

## 🔨 Build Process

### Step 1: Clean Build
```powershell
cd "C:\Users\ABDULLAH IMRAN\source\repos\Aimran Data Science Lab\AIMRAN Data Science Lab"

# Clean previous builds
dotnet clean -c Release

# Restore NuGet packages
dotnet restore
```

### Step 2: Build Solution
```powershell
# Build in Release configuration
dotnet build -c Release --no-restore --verbosity normal
```

### Step 3: Run Tests (if any)
```powershell
# Run test suite
dotnet test -c Release --no-build --verbosity normal
```

---

## 📦 Publishing Options

### Option 1: Self-Contained Executable (Recommended for Distribution)

```powershell
# Publish as single executable file
dotnet publish -c Release `
  -r win-x64 `
  --self-contained `
  -p:PublishReadyToRun=true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o .\bin\Release\publish\exe

# Output: AIMRAN-DataScienceLab.exe (~150-200 MB)
```

**Advantages:**
- Single executable file
- No .NET runtime required on target machine
- Easy distribution
- Larger file size

**Usage:**
```powershell
# Run the executable
.\AIMRAN-DataScienceLab.exe

# Application listens on http://localhost:5000 (or configured URL)
```

### Option 2: Framework-Dependent (Smaller Package)

```powershell
# Publish as framework-dependent
dotnet publish -c Release `
  -r win-x64 `
  --self-contained=false `
  -p:PublishReadyToRun=true `
  -o .\bin\Release\publish\framework-dependent

# Output: Multiple files (~20-30 MB)
```

**Advantages:**
- Smaller file size
- Requires .NET 10 runtime on target machine
- Automatic updates with runtime

**Requirements on Target:**
- .NET 10 runtime must be installed

### Option 3: Portable (No Runtime Dependencies)

```powershell
# Publish as portable application
dotnet publish -c Release `
  -p:PublishReadyToRun=true `
  -o .\bin\Release\publish\portable

# Output: .NET Framework required (smaller)
```

---

## 🚀 Deployment

### Local Development Testing
```powershell
# Run directly
dotnet run --project AimranDataScienceLab.Web -c Release

# Access at: http://localhost:5000
```

### Windows Service Installation (Self-Contained)

```powershell
# Create Windows Service
$serviceName = "AIMRAN-DataScienceLab"
$executablePath = "C:\Program Files\AIMRAN DataScienceLab\AIMRAN-DataScienceLab.exe"
$displayName = "AIMRAN Data Science Lab"

# Register service
New-Service `
  -Name $serviceName `
  -DisplayName $displayName `
  -BinaryPathName $executablePath `
  -StartupType Automatic

# Start service
Start-Service -Name $serviceName

# Verify service is running
Get-Service -Name $serviceName
```

### Configure Application URLs
```powershell
# Create appsettings.json in application directory
# Set ASPNETCORE_URLS environment variable
$env:ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"

# Or in appsettings.Production.json:
# "ASPNETCORE_URLS": "http://0.0.0.0:80;https://0.0.0.0:443"
```

---

## 🔐 Security Configuration

### HTTPS Setup
```powershell
# Generate self-signed certificate (Development)
dotnet dev-certs https --trust

# For Production, use proper SSL certificates
# Place certificate in appropriate location
```

### Firewall Rules
```powershell
# Add firewall rule for HTTP
netsh advfirewall firewall add rule `
  name="AIMRAN DSL - HTTP" `
  dir=in `
  action=allow `
  protocol=tcp `
  localport=80

# Add firewall rule for HTTPS
netsh advfirewall firewall add rule `
  name="AIMRAN DSL - HTTPS" `
  dir=in `
  action=allow `
  protocol=tcp `
  localport=443
```

---

## 📊 Performance Optimization

### Build Optimization Settings (Already Configured)

**Release Profile Includes:**
- ✅ Tiered Compilation
- ✅ Ready-to-Run (R2R) compilation
- ✅ Composite R2R
- ✅ Embedded debug symbols
- ✅ Single file publication

### Runtime Optimization

```powershell
# Set environment variables for optimized runtime
$env:DOTNET_TieredCompilation=1
$env:DOTNET_TieredCompilationQuickJit=1
$env:DOTNET_ReadyToRun=1

# For production, set these in system environment or batch file
```

---

## 🧪 Testing Checklist

### Pre-Release Validation

- [ ] **Build Succeeds**: No compilation errors
- [ ] **All Tests Pass**: 100% pass rate
- [ ] **No Warnings**: Zero build warnings
- [ ] **UI Components Load**: All MudBlazor components render
- [ ] **Icons Display**: All Material icons visible
- [ ] **Services Initialize**: Logger, SignalR, Settings all registered
- [ ] **Database Connection**: SQLite initialization works
- [ ] **Error Handling**: Error pages display correctly
- [ ] **Performance**: Application startup < 5 seconds
- [ ] **Security**: HTTPS enforced, headers correct
- [ ] **Logging**: Log files created successfully
- [ ] **Memory**: No memory leaks detected

### Functional Testing

1. **UI/Navigation**
   - [ ] Home page loads
   - [ ] Settings page functional
   - [ ] Theme switching works
   - [ ] All navigation links work
   - [ ] Responsive on mobile

2. **Backend Services**
   - [ ] Settings service working
   - [ ] Collaboration services operational
   - [ ] SignalR real-time updates functional
   - [ ] Database operations successful

3. **Error Scenarios**
   - [ ] 404 page displays
   - [ ] Error page displays
   - [ ] Graceful error handling
   - [ ] Logging captures errors

---

## 📝 Build Output Structure

### Self-Contained Executable
```
publish/
├── AIMRAN-DataScienceLab.exe          (Main executable)
├── AIMRAN-DataScienceLab.pdb          (Debug symbols)
├── shared/                            (Runtime libraries)
├── wwwroot/                           (Static files)
│   ├── css/
│   ├── js/
│   └── lib/
├── appsettings.json                   (Configuration)
├── appsettings.Production.json        (Production config)
└── logs/                              (Log directory)
```

### Framework-Dependent
```
publish/
├── AIMRAN-DataScienceLab.exe
├── AIMRAN-DataScienceLab.dll
├── AIMRAN-DataScienceLab.pdb
├── *.deps.json
├── *.runtimeconfig.json
├── wwwroot/
├── appsettings.json
└── logs/
```

---

## 🐛 Troubleshooting

### Build Issues

**Issue**: "Project does not target a Windows runtime"
```powershell
# Solution: Add runtime identifier
dotnet publish -c Release -r win-x64
```

**Issue**: "Package restore failed"
```powershell
# Solution: Clear NuGet cache and restore
dotnet nuget locals all --clear
dotnet restore
```

### Runtime Issues

**Issue**: "Port already in use"
```powershell
# Find process using port
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID> /F

# Or configure different port in appsettings.json
```

**Issue**: "HTTPS certificate not found"
```powershell
# Regenerate development certificate
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

---

## 📈 Monitoring & Logging

### Log File Location
```
Relative to executable:
logs/
├── .aimran-dsl-2025-01-08.log
├── .aimran-dsl-2025-01-09.log
└── ...
```

### View Logs
```powershell
# Real-time log viewing
Get-Content -Path "logs/.aimran-dsl-*.log" -Wait

# Filter errors only
Select-String -Path "logs/*.log" -Pattern "ERROR|FATAL"
```

### Logging Levels
- **Information**: Application lifecycle events
- **Warning**: Potential issues
- **Error**: Application errors
- **Fatal**: Critical failures

---

## 📦 Packaging as MSI (Optional)

For creating an MSI installer, use WiX Toolset:

```powershell
# Install WiX Toolset
choco install wixtoolset

# Create MSI from published files
# (Requires WiX source files - not included in this guide)
```

Alternatively, use tools like:
- **Advanced Installer**: GUI-based MSI creation
- **Inno Setup**: Script-based installer
- **NSIS**: Open-source installer

---

## ✅ Release Checklist

Before releasing to users:

- [ ] Version number updated (1.0.0)
- [ ] Changelog documented
- [ ] All tests passing
- [ ] Security review complete
- [ ] Performance benchmarked
- [ ] Documentation updated
- [ ] Build artifacts generated
- [ ] File hash/signature verified
- [ ] Deployment tested
- [ ] Rollback plan documented

---

## 🔄 Update Process

### For Minor Updates
```powershell
# Build new version
dotnet publish -c Release -r win-x64 --self-contained

# Stop running instance
Stop-Service -Name "AIMRAN-DataScienceLab"

# Backup current version
Copy-Item "C:\Program Files\AIMRAN DataScienceLab" -Destination "C:\Program Files\AIMRAN DataScienceLab.bak"

# Replace files
Copy-Item "bin\Release\publish\*" -Destination "C:\Program Files\AIMRAN DataScienceLab" -Recurse -Force

# Start service
Start-Service -Name "AIMRAN-DataScienceLab"
```

---

## 📞 Support

For issues or questions:
1. Check logs in `logs/` directory
2. Review error messages in application error page
3. Consult this guide's troubleshooting section
4. Review source code and comments for debugging

---

**Last Updated**: January 8, 2025  
**Version**: 1.0  
**Status**: Production Ready  

---

## Quick Commands Reference

```powershell
# Clean build
dotnet clean -c Release

# Restore packages
dotnet restore

# Build solution
dotnet build -c Release --no-restore

# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true -p:PublishSingleFile=true

# Run application
dotnet run -c Release

# View published output
explorer .\bin\Release\net10.0\win-x64\publish\
```

---
