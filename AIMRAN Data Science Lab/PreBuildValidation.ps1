#!/usr/bin/env pwsh
<#
.SYNOPSIS
	AIMRAN Data Science Lab - Pre-Build Validation Script

.DESCRIPTION
	Comprehensive validation script to ensure the application is production-ready
	before building executable or MSI package.

.NOTES
	Author: AIMRAN Team
	Version: 1.0
	LastModified: January 8, 2025

.EXAMPLE
	.\PreBuildValidation.ps1
	.\PreBuildValidation.ps1 -Verbose
#>

param(
	[switch]$Verbose,
	[switch]$FixIssues = $true
)

# Color configuration
$colors = @{
	Success = 'Green'
	Warning = 'Yellow'
	Error   = 'Red'
	Info    = 'Cyan'
}

# Counter for issues
$script:errorCount = 0
$script:warningCount = 0
$script:successCount = 0

# Utility functions
function Write-ValidationHeader {
	param([string]$Title)

	Write-Host ""
	Write-Host ("=" * 80) -ForegroundColor $colors.Info
	Write-Host "🔍 $Title" -ForegroundColor $colors.Info
	Write-Host ("=" * 80) -ForegroundColor $colors.Info
}

function Write-ValidationResult {
	param(
		[string]$Check,
		[bool]$Passed,
		[string]$Details = ""
	)

	if ($Passed) {
		Write-Host "✅ $Check" -ForegroundColor $colors.Success
		$script:successCount++
	}
	else {
		Write-Host "❌ $Check" -ForegroundColor $colors.Error
		$script:errorCount++
	}

	if ($Details) {
		Write-Host "   📌 $Details" -ForegroundColor $colors.Info
	}
}

function Write-ValidationWarning {
	param([string]$Message)

	Write-Host "⚠️  $Message" -ForegroundColor $colors.Warning
	$script:warningCount++
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 1: Environment Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "1️⃣  ENVIRONMENT VALIDATION"

# Check .NET SDK
try {
	$dotnetVersion = dotnet --version
	$passed = $dotnetVersion -match "10\."
	Write-ValidationResult ".NET 10 SDK Installed" $passed "Version: $dotnetVersion"
}
catch {
	Write-ValidationResult ".NET SDK Installed" $false "Error: $_"
}

# Check Node.js (optional for build tools)
try {
	$nodeVersion = node --version 2>$null
	if ($nodeVersion) {
		Write-ValidationResult "Node.js Available" $true "Version: $nodeVersion"
	} else {
		Write-ValidationWarning "Node.js not found (optional for build tools)"
	}
}
catch {
	Write-ValidationWarning "Node.js check failed (optional)"
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 2: Project Structure Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "2️⃣  PROJECT STRUCTURE VALIDATION"

$requiredFiles = @(
	"AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj",
	"AimranDataScienceLab.Shared\AimranDataScienceLab.Shared.csproj",
	"AimranDataScienceLab.Engine\AimranDataScienceLab.Engine.csproj",
	"AimranDataScienceLab.Gateway\AimranDataScienceLab.Gateway.csproj",
	"AimranDataScienceLab.Web\Program.cs",
	"AimranDataScienceLab.Web\appsettings.json",
	"AimranDataScienceLab.Web\appsettings.Production.json"
)

foreach ($file in $requiredFiles) {
	$exists = Test-Path $file
	Write-ValidationResult "File exists: $file" $exists
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 3: Dependencies Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "3️⃣  DEPENDENCIES VALIDATION"

Write-Host "📦 Restoring NuGet packages..." -ForegroundColor $colors.Info
$restoreOutput = dotnet restore 2>&1
$restoreSuccess = $LASTEXITCODE -eq 0
Write-ValidationResult "NuGet Restore" $restoreSuccess

if (-not $restoreSuccess) {
	Write-Host "   Package restore output:" -ForegroundColor $colors.Error
	Write-Host $restoreOutput
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 4: Code Quality Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "4️⃣  CODE QUALITY VALIDATION"

Write-Host "🔨 Building solution in Release configuration..." -ForegroundColor $colors.Info
$buildOutput = dotnet build -c Release --no-restore --verbosity minimal 2>&1
$buildSuccess = $LASTEXITCODE -eq 0
Write-ValidationResult "Build Succeeds" $buildSuccess

if (-not $buildSuccess) {
	Write-Host "   Build errors found:" -ForegroundColor $colors.Error
	$buildOutput | Select-String -Pattern "error|ERROR" | ForEach-Object { 
		Write-Host "   $_" -ForegroundColor $colors.Error 
	}
}

# Check for warnings
$warningLines = $buildOutput | Select-String -Pattern "warning|WARNING"
if ($warningLines.Count -gt 0) {
	Write-ValidationWarning "Build warnings detected: $($warningLines.Count) warning(s)"
	if ($Verbose) {
		$warningLines | ForEach-Object { Write-Host "   $_" -ForegroundColor $colors.Warning }
	}
} else {
	Write-ValidationResult "No Build Warnings" $true "Clean build"
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 5: Configuration Files Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "5️⃣  CONFIGURATION VALIDATION"

# Check appsettings.json
try {
	$appSettings = Get-Content "AimranDataScienceLab.Web\appsettings.json" | ConvertFrom-Json
	Write-ValidationResult "appsettings.json Valid" $true
}
catch {
	Write-ValidationResult "appsettings.json Valid" $false "Error: $_"
}

# Check appsettings.Production.json
try {
	$prodSettings = Get-Content "AimranDataScienceLab.Web\appsettings.Production.json" | ConvertFrom-Json
	Write-ValidationResult "appsettings.Production.json Valid" $true
}
catch {
	Write-ValidationResult "appsettings.Production.json Valid" $false "Error: $_"
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 6: Security Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "6️⃣  SECURITY VALIDATION"

# Check for hardcoded secrets
Write-Host "🔍 Scanning for hardcoded credentials..." -ForegroundColor $colors.Info
$secretPatterns = @(
	"password\s*=\s*['\"]",
	"api[_-]?key\s*=\s*['\"]",
	"secret\s*=\s*['\"]"
)

$foundSecrets = $false
foreach ($pattern in $secretPatterns) {
	$matches = Get-ChildItem -Path "AimranDataScienceLab.Web" -Recurse -Include "*.cs", "*.json" | 
		Select-String -Pattern $pattern

	if ($matches) {
		$foundSecrets = $true
		Write-Host "   ⚠️  Potential hardcoded secret found in:" -ForegroundColor $colors.Warning
		$matches | ForEach-Object { Write-Host "      $_" }
	}
}

if (-not $foundSecrets) {
	Write-ValidationResult "No Hardcoded Credentials Found" $true
} else {
	Write-ValidationWarning "Review found items for actual secrets"
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 7: UI Components Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "7️⃣  UI COMPONENTS VALIDATION"

# Check for MudBlazor components
$mudBlazorUsage = Get-ChildItem -Path "AimranDataScienceLab.Shared\Components" -Recurse -Include "*.razor" | 
	Select-String -Pattern "<Mud" | Measure-Object | Select-Object -ExpandProperty Count

Write-ValidationResult "MudBlazor Components Found" ($mudBlazorUsage -gt 0) "$mudBlazorUsage Mud* components"

# Check for Material Icons
$iconUsage = Get-ChildItem -Path "AimranDataScienceLab.Shared\Components" -Recurse -Include "*.razor" | 
	Select-String -Pattern "Icons\.Material\." | Measure-Object | Select-Object -ExpandProperty Count

Write-ValidationResult "Material Icons Used" ($iconUsage -gt 0) "$iconUsage icon references"

# ════════════════════════════════════════════════════════════════════════════════
# Section 8: Database & Services Validation
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "8️⃣  SERVICES VALIDATION"

# Check for key services in Program.cs
$programFile = Get-Content "AimranDataScienceLab.Web\Program.cs" -Raw

$requiredServices = @(
	@{ Pattern = "AddRazorComponents"; Name = "Blazor Components" },
	@{ Pattern = "AddMudServices"; Name = "MudBlazor Services" },
	@{ Pattern = "AddSignalR"; Name = "SignalR" },
	@{ Pattern = "ISettingsService"; Name = "Settings Service" },
	@{ Pattern = "ICollaborationService"; Name = "Collaboration Service" },
	@{ Pattern = "AddAimranDataScienceLab"; Name = "AIMRAN Engine" }
)

foreach ($service in $requiredServices) {
	$found = $programFile -match $service.Pattern
	Write-ValidationResult "Service Registered: $($service.Name)" $found
}

# Check for Serilog logging
$loggingConfigured = $programFile -match "Serilog|Log\.Logger"
Write-ValidationResult "Enterprise Logging Configured" $loggingConfigured

# ════════════════════════════════════════════════════════════════════════════════
# Section 9: Output Directory Check
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "9️⃣  BUILD OUTPUT VALIDATION"

$outputPath = "bin\Release"
$outputExists = Test-Path $outputPath
Write-ValidationResult "Release Output Directory" $outputExists

if ($outputExists) {
	$dllCount = (Get-ChildItem -Path $outputPath -Recurse -Include "*.dll" | Measure-Object).Count
	Write-ValidationResult "DLL Files Generated" ($dllCount -gt 0) "$dllCount assemblies"
}

# ════════════════════════════════════════════════════════════════════════════════
# Section 10: Documentation Check
# ════════════════════════════════════════════════════════════════════════════════

Write-ValidationHeader "🔟 DOCUMENTATION VALIDATION"

$docFiles = @(
	"AimranDataScienceLab.Shared\BUILD_AND_DEPLOYMENT_GUIDE.md",
	"AimranDataScienceLab.Shared\THEME_IMPLEMENTATION_COMPLETE.md",
	"AimranDataScienceLab.Shared\IMPLEMENTATION_SUMMARY.md"
)

foreach ($doc in $docFiles) {
	$exists = Test-Path $doc
	Write-ValidationResult "Documentation: $(Split-Path $doc -Leaf)" $exists
}

# ════════════════════════════════════════════════════════════════════════════════
# Summary Report
# ════════════════════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host ("=" * 80) -ForegroundColor $colors.Info
Write-Host "📊 VALIDATION SUMMARY" -ForegroundColor $colors.Info
Write-Host ("=" * 80) -ForegroundColor $colors.Info

Write-Host "✅ Passed:  $($script:successCount)" -ForegroundColor $colors.Success
Write-Host "⚠️  Warnings: $($script:warningCount)" -ForegroundColor $colors.Warning
Write-Host "❌ Failed:  $($script:errorCount)" -ForegroundColor $colors.Error

Write-Host ""

if ($script:errorCount -eq 0) {
	Write-Host "🎉 All validations passed! Application is ready for build." -ForegroundColor $colors.Success
	Write-Host ""
	Write-Host "Next steps:" -ForegroundColor $colors.Info
	Write-Host "  1. Run: dotnet publish -c Release -r win-x64 --self-contained" -ForegroundColor $colors.Info
	Write-Host "  2. Output: bin\Release\net10.0\win-x64\publish\" -ForegroundColor $colors.Info
	Write-Host "  3. Execute: .\bin\Release\net10.0\win-x64\publish\AIMRAN-DataScienceLab.exe" -ForegroundColor $colors.Info
}
else {
	Write-Host "❌ Validation failed! $($script:errorCount) issue(s) must be resolved." -ForegroundColor $colors.Error
	Write-Host ""
	Write-Host "Please fix the errors above before proceeding with the build." -ForegroundColor $colors.Warning
	exit 1
}

Write-Host ""
Write-Host ("=" * 80) -ForegroundColor $colors.Info
