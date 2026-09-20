# AIMRAN Data Science Lab — Final Overview

## Executive summary
AIMRAN Data Science Lab is a .NET 10, Blazor-based data science workbench that supports two delivery targets:

- **Desktop**: MAUI Blazor Hybrid app
- **Web**: ASP.NET Core Blazor Server app

Both hosts share one UI and one service layer through the shared Razor Class Library.

## Solution components

| Project | Purpose |
|---|---|
| `AIMRAN Data Science Lab` | MAUI Blazor Hybrid desktop host |
| `AimranDataScienceLab.Web` | Blazor Server web host |
| `AimranDataScienceLab.Shared` | Shared components, services, and models |
| `AimranDataScienceLab.Engine` | Core engine and SQLite persistence |
| `AimranDataScienceLab.Gateway` | HTTP gateway for Python and Rust engines |
| `AimranDataScienceLab.Engine.Tests` | Engine validation tests |

## Core capabilities

- Dashboard and project management
- Dataset import, preview, and versioning
- Data cleaning and profiling
- Experiment tracking and model management
- Resource monitoring for CPU, RAM, disk, and GPU
- Azure integration for storage and ML workflows
- Gateway support for Python AI and Rust performance services

## Architecture summary

- **Shared UI layer**: common Blazor pages and reusable components
- **Service layer**: engine, dataset, project, model, and resource services
- **Storage layer**: SQLite-backed metadata and local data handling
- **Gateway layer**: Python FastAPI and Rust Actix-Web integration
- **Host layer**: MAUI for desktop and Blazor Server for browser access

## Final solution and executable

The final solution is the full set of projects working together as one platform.

### Build

```bash
dotnet test AimranDataScienceLab.Engine.Tests
dotnet build AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj -c Release
dotnet build "AIMRAN Data Science Lab.csproj" -c Release -f net10.0-windows10.0.19041.0
```

### Publish

```bash
dotnet publish AimranDataScienceLab.Web\AimranDataScienceLab.Web.csproj -c Release -o .\publish\web
dotnet publish "AIMRAN Data Science Lab.csproj" -c Release -f net10.0-windows10.0.19041.0 -o .\publish\desktop
```

### Result

- The **web host** becomes the browser-ready deployment.
- The **MAUI host** becomes the desktop executable.
- Both use the same shared app logic and data services.
