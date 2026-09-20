# ============================================================================
# AIMRAN Data Science Lab - Windows 11 Build & Package Script
# ============================================================================
# This script builds the MAUI desktop app and creates an installer package
# for Windows 11 deployment.
# ============================================================================

param(
	[switch]$Release = $false,
	[switch]$Package = $false,
	[switch]$Sign = $false,
	[string]$CertificatePath = $null,
	[string]$OutputDir = ".\publish\windows",
	[switch]$Help = $false
)

if ($Help) {
	Write-Host @"
AIMRAN Data Science Lab - Windows Build Script

Usage:
	.\build-windows.ps1 [options]

Options:
	-Release                Build in Release configuration (optimized)
	-Package                Create installer package (MSIX or ZIP)
	-Sign                   Sign the executable (requires certificate)
	-CertificatePath <path> Path to code signing certificate (.pfx)
	-OutputDir <path>       Output directory for published app (default: .\publish\windows)
	-Help                   Show this help message

Examples:
	# Debug build
	.\build-windows.ps1

	# Release build with packaging
	.\build-windows.ps1 -Release -Package

	# Release build with code signing
	.\build-windows.ps1 -Release -Sign -CertificatePath "C:\Certs\codesign.pfx"

"@
	exit 0
}

# ============================================================================
# Setup
# ============================================================================

$Configuration = if ($Release) { "Release" } else { "Debug" }
$ProjectPath = "AIMRAN Data Science Lab.csproj"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = Split-Path -Parent $ScriptDir

Write-Host "🔨 AIMRAN Data Science Lab - Windows Build Script" -ForegroundColor Cyan
Write-Host "=" * 70
Write-Host "Configuration:  $Configuration"
Write-Host "Output:         $OutputDir"
Write-Host "Package:        $(if ($Package) { 'Yes' } else { 'No' })"
Write-Host "Sign:           $(if ($Sign) { 'Yes' } else { 'No' })"
Write-Host ""

# ============================================================================
# Step 1: Clean previous build
# ============================================================================

Write-Host "📋 Step 1: Cleaning previous build artifacts..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
	Remove-Item $OutputDir -Recurse -Force
	Write-Host "  ✓ Cleaned $OutputDir"
}

# ============================================================================
# Step 2: Restore NuGet packages
# ============================================================================

Write-Host "📋 Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
	Write-Host "  ✗ Restore failed!" -ForegroundColor Red
	exit 1
}
Write-Host "  ✓ Packages restored"

# ============================================================================
# Step 3: Build the application
# ============================================================================

Write-Host "📋 Step 3: Building application ($Configuration)..." -ForegroundColor Yellow
dotnet build $ProjectPath `
	-c $Configuration `
	-f net10.0-windows10.0.19041.0 `
	-r win-x64

if ($LASTEXITCODE -ne 0) {
	Write-Host "  ✗ Build failed!" -ForegroundColor Red
	exit 1
}
Write-Host "  ✓ Build completed"

# ============================================================================
# Step 4: Publish application
# ============================================================================

Write-Host "📋 Step 4: Publishing application..." -ForegroundColor Yellow

$PublishArgs = @(
	$ProjectPath
	"-c", $Configuration
	"-f", "net10.0-windows10.0.19041.0"
	"-r", "win-x64"
	"-o", $OutputDir
)

# Add self-contained and single-file options for Release
if ($Release) {
	$PublishArgs += "-p:SelfContained=true"
	$PublishArgs += "-p:PublishReadyToRun=true"
	$PublishArgs += "-p:PublishSingleFile=true"
	$PublishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

dotnet publish @PublishArgs

if ($LASTEXITCODE -ne 0) {
	Write-Host "  ✗ Publish failed!" -ForegroundColor Red
	exit 1
}
Write-Host "  ✓ Published to $OutputDir"

# ============================================================================
# Step 5: Code Signing (optional)
# ============================================================================

if ($Sign -and $CertificatePath) {
	Write-Host "📋 Step 5: Code Signing..." -ForegroundColor Yellow

	if (-not (Test-Path $CertificatePath)) {
		Write-Host "  ✗ Certificate not found: $CertificatePath" -ForegroundColor Red
		exit 1
	}

	$ExePath = Join-Path $OutputDir "AIMRAN Data Science Lab.exe"

	# Sign with signtool (requires Windows SDK)
	$SignToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

	if (Test-Path $SignToolPath) {
		Write-Host "  Signing $ExePath..."
		& $SignToolPath sign /f $CertificatePath /t http://timestamp.digicert.com `
			/fd SHA256 /as $ExePath

		if ($LASTEXITCODE -eq 0) {
			Write-Host "  ✓ Code signing completed"
		}
		else {
			Write-Host "  ⚠ Code signing failed (non-critical)" -ForegroundColor Yellow
		}
	}
	else {
		Write-Host "  ⚠ signtool.exe not found (install Windows SDK)" -ForegroundColor Yellow
	}
}

# ============================================================================
# Step 6: Create Package (optional)
# ============================================================================

if ($Package) {
	Write-Host "📋 Step 6: Creating package..." -ForegroundColor Yellow

	$AppName = "AIMRAN Data Science Lab"
	$Version = "2.0.0"
	$PackageDir = Join-Path (Split-Path $OutputDir) "packages"

	if (-not (Test-Path $PackageDir)) {
		New-Item $PackageDir -ItemType Directory | Out-Null
	}

	# Create ZIP archive
	$ZipPath = Join-Path $PackageDir "$($AppName -replace ' ', '-')_$Version.zip"
	Write-Host "  Creating ZIP package: $ZipPath"

	Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipPath -Force
	Write-Host "  ✓ ZIP package created"

	# Show package info
	$ZipSize = (Get-Item $ZipPath).Length / 1MB
	Write-Host "  Package size: $($ZipSize.ToString('F1')) MB"
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "=" * 70
Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output location: $OutputDir"

if ($Release) {
	$ExePath = Join-Path $OutputDir "AIMRAN Data Science Lab.exe"
	if (Test-Path $ExePath) {
		$ExeSize = (Get-Item $ExePath).Length / 1MB
		Write-Host "Executable:      AIMRAN Data Science Lab.exe ($($ExeSize.ToString('F1')) MB)"
	}
}

Write-Host ""
Write-Host "📦 Distribution:"
Write-Host "  1. Zip for distribution: Copy $OutputDir to any location on Windows 11"
Write-Host "  2. Single file: Run AIMRAN Data Science Lab.exe"
Write-Host "  3. Portable: No installation required, all dependencies included"
Write-Host ""
Write-Host "💾 Data Storage:"
Write-Host "  Database:  %APPDATA%\AIMRAN Lab\Database\"
Write-Host "  Engines:   %APPDATA%\AIMRAN Lab\Engines\"
Write-Host "  Logs:      %APPDATA%\AIMRAN Lab\Logs\"
Write-Host "  Backups:   %APPDATA%\AIMRAN Lab\Backups\"
Write-Host ""
Write-Host "🚀 To run: $OutputDir\AIMRAN Data Science Lab.exe"
