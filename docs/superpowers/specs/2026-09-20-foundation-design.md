# Foundation — Design Spec

**Date:** 2026-09-20
**Slice:** ① Foundation (of a 4-slice program)
**Status:** Approved — ready for implementation planning

---

## 1. Context

AIMRAN Data Science Lab is a four-runtime system:

| Subsystem | Tech | Role |
|---|---|---|
| Root platform | Python / FastAPI (`src/`, `cli.py`, `worker.py`) | Original data-science platform (AutoML, gRPC :50051, Celery) |
| Control plane | .NET 10 — MAUI Blazor Hybrid (desktop) + ASP.NET Core Blazor Server (web), sharing RCL `AimranDataScienceLab.Shared` | UI + orchestration |
| Python engine | FastAPI, `:8100` | Experiments, models, profiling, cleaning, code exec, viz |
| Rust engine | axum, `:8200` | Resource snapshot/stream, delta compute, hashing, CSV I/O |
| R engine | plumber, `:8300` | Statistics, R code exec, viz, profiling |

The umbrella goal is an enterprise-grade lab with a complete ecosystem, enhanced UI, modern stack,
and all runtime errors resolved. That is too large for one spec, so it is decomposed into four
slices. This spec covers **slice ① Foundation** only.

### Program roadmap

1. **Foundation** *(this spec)* — all four runtimes build, launch, and stay healthy in **both** hosts; publishable executable.
2. **Enterprise UI** — enhanced UX layer.
3. **Stack modernization** — replace dated dependencies.
4. **Enterprise hardening + extensibility** — authz, audit, observability, multi-tenancy.

Foundation is deliberately first: slices ②–④ all assume a green build and reachable engines.

---

## 2. Problem statement — verified defects

Every item below was confirmed by reading source or build output, not inferred from stale logs.

### 2.1 The .NET build is RED (5 errors)

Prior belief was ".NET builds clean." It does not. `build-maui-publish2.txt` records:

| Error | Location |
|---|---|
| `RZ10009` — component parameter `Engine` used two or more times (×3) | `AdminDashboard.razor` lines 128, 129, 130 col 36 |
| `CS0115` — `App.OnDestroying()`: no suitable method found to override | `App.xaml.cs:19,33` |
| `CS0246` — `HttpContext` could not be found | `AimranDataScienceLab.Web/Components/Error.razor:3,9` |

All five are attributed to the desktop head, `AIMRAN Data Science Lab.csproj`.

**Root cause A — Razor/XAML glob leak (4 of 5 errors).** The desktop head's project directory
physically *contains every sibling project as a subfolder*. Its exclusion items cover only
`Compile`, `Content`, and `None`:

```xml
<Compile Remove="AimranDataScienceLab.Web\**" />
<Content Remove="AimranDataScienceLab.Web\**" />
<None    Remove="AimranDataScienceLab.Web\**" />
<!-- repeated for Core, Engine, Engine.Tests, Gateway, ServiceTemplates, Shared -->
```

They do **not** cover the implicit `RazorComponent` glob (`**/*.razor`) or `MauiXaml`. Consequences:

- `Shared/Components/EngineControl.razor` is compiled **twice** — once via the project reference,
  once via the leaked glob — producing a duplicate component type. Blazor then sees the `Engine`
  parameter as defined twice, emitting RZ10009 at each usage site.
- `Web/Components/Error.razor` leaks into the MAUI head, which has no ASP.NET Core `HttpContext`,
  producing CS0246.

Verified non-cause: `EngineControl.razor` defines `Engine` exactly **once** (lines 117–118:
`[Parameter] public ExternalEngine Engine { get; set; }`). RZ10009 is therefore an artifact of the
leak, **not** a real double-definition. Do not "fix" the component.

**Root cause B — MAUI API drift (1 error).** `Microsoft.Maui.Controls.Application` in .NET 10 has
no `OnDestroying` to override:

```csharp
protected override void OnDestroying()      // CS0115
{
    base.OnDestroying();
    _ = AppLifecycleManager.ShutdownAsync();
}
```

### 2.2 Web host: every shared route 404s at runtime

`AimranDataScienceLab.Web/Program.cs:116` registers the **Web** assembly for route discovery:

```csharp
.AddAdditionalAssemblies(typeof(AimranDataScienceLab.Web.Components.Routes).Assembly)
```

The Web project contains no `@page` components — every page lives in the Shared RCL. So the web
host compiles and starts, then 404s on all application routes. `Routes.razor:4` has the matching
omission (no `AdditionalAssemblies`).

### 2.3 None of the three engines reliably serve

| Engine | Defect | Effect |
|---|---|---|
| Python `ai-engine/main.py` | Hardcodes `:8100`; `reload=True`; ignores `--port` | Cannot be placed on another port; the reloader forks a child, breaking parent-process supervision and PID-based shutdown |
| Rust `rust-engine/src/main.rs` | Hardcodes `:8200`; ignores `--port`; `.unwrap()` on bind | Port conflict ⇒ panic instead of a diagnosable error |
| R `r-engine/plumber_api.R` | **No `pr_run()` call** (present only in a comment) | `Rscript plumber_api.R` defines routes and **exits**. The R engine has never served. |

### 2.4 Two conflicting engine subsystems

Two independent launch/health layers disagree, and "works in web **and** desktop" forces a reconciliation:

| Aspect | Subsystem A — `Gateway` (drives Web) | Subsystem B — `EngineManager` (drives Desktop) |
|---|---|---|
| R port | `:9000` | `:8300` |
| Launches | Python + Rust only (no R launcher) | All three |
| Port argument | passes `--port` | passes **nothing** (engines must self-default) |
| Engine file location | relative to `AppContext.BaseDirectory` | `%APPDATA%\AIMRAN Lab\Engines\{python,rust,r}`, override `AIMRAN_ENGINES_PATH` |
| Readiness | poll `/health` every 500 ms up to 15 s (warn only) | start → wait 1000 ms → single `/health`, else stop + fail |
| Supervision | watchdog polls `HasExited` every 5 s; restart backoff 1→2→4→8→30 s cap | 30 s monitor; restarts Python/Rust only |

Shared contract (both): `GET /health`, any 2xx = healthy, snake_case JSON, optional
`X-Engine-Version` header. Timeouts: Python 60 s, Rust 10 s, R 60 s; retry 3× exponential backoff.

Note the R contradiction: Gateway expects `:9000` but never launches R; EngineManager expects
`:8300`. The R engine cannot serve either (§2.3), so this has never surfaced at runtime.

### 2.5 The publish driver cannot execute

`build-windows.ps1` is saved as UTF-8 **without a BOM**. Windows PowerShell 5.1 reads BOM-less files
as ANSI, so its emoji (`🔨 📋 📦 …`) become mojibake; a stray quote byte desynchronizes the
tokenizer and the script fails to **parse**. Observed: cascading `UnexpectedToken` /
`Missing closing '}'` errors at lines 68, 148, 153, 206, 211. **Zero lines execute.** The "make
executable" step has never run, independent of the compile errors.

### 2.6 NuGet version drift

`Microsoft.Extensions.*` is pinned at `10.0.0` in `Core`, `ServiceTemplates`, and the desktop head
(`Logging.Debug`, csproj line 83), while `Shared`, `Gateway`, and `Web` use `10.0.3`. This risks
NU1605 downgrade errors.

### 2.7 Build-status reporting is unreliable — a meta-defect

Three separate times, a build failure was reported as success because the command was piped
(`| tee`, `| tail`) and the shell returned the **pipe's** exit status, not the build's:

1. `.NET publish | tee` → "exit 0" while emitting 5 errors.
2. `build-windows.ps1 | tee` → "exit 0" while failing to parse.
3. `cargo build | tail` → "exit 0" while failing with E0786.

This is why the project was believed green. It is a first-class defect: **the build gate itself was
lying.** Fixing it is in scope (§5, P4).

### 2.8 Toolchain status

| Tool | Status |
|---|---|
| .NET SDK | **10.0.401** ✓ |
| Rust | **1.98.1** ✓ (`rustc 1.98.1`, `cargo 1.98.1`) — satisfies the engine's `edition 2024` / `rust-version 1.85` floor |
| Python | 3.14.7 present — too bleeding-edge for the ML stack; pin **3.12** |
| R | **absent** — interpreted, so it must exist at runtime; cannot be bundled |

Rust dependency resolution was verified: cargo locked 205 crates to "Rust 1.85-compatible versions,"
correctly declining newer crates requiring 1.88. Dependency compilation succeeds.

**Artifact-corruption caveat:** interrupting a cargo build leaves truncated `.rmeta` files that
cause `E0786: found invalid metadata files` on the next run. `cargo clean -p <pkg>` does **not**
reliably clear them (observed: "Removed 0 files", with corruption in both `rayon` and
`sharded_slab`). A full `cargo clean` is required after any aborted build.

---

## 3. Scope

### In scope

- The five workstreams in §5 (W1–W5).
- All defects in §2.

### Out of scope (deferred to later slices)

- UI enhancement beyond what is needed for engine status to render correctly → slice ②.
- Dependency modernization beyond the version alignment in W1 → slice ③.
- AuthN/AuthZ, audit, multi-tenancy, telemetry backends → slice ④.

### Decision: root `src/` platform — **regression check only** *(approved)*

The root FastAPI platform is **not** wired into the hosts in Foundation. It gets a build/import
regression check so it cannot silently rot, and nothing more.

**Rationale:** Foundation's job is making the four runtimes rock-solid. Once the engine manifest
seam (§4) exists, wiring `src/` in becomes a manifest entry plus a typed client — cheap and clean in
slice ④, distracting now.

---

## 4. Architecture

### 4.1 Central decision: an engine manifest as single source of truth

Engine facts — port, health path, launch command, working directory, timeout, version — are
currently hardcoded in **two** places (Gateway and `EngineManager`). That duplication *is* the root
cause of §2.4. Patching the two copies to agree would leave the same defect latent: the next change
re-introduces drift.

Instead, declare engine facts **once** in `engines.json`, read by both hosts and the Gateway.

```
                    ┌──────────────────┐
                    │  engines.json    │  ← single source of truth
                    └────────┬─────────┘
                             │ read at startup
                ┌────────────┴────────────┐
                │      EngineManager      │  ← sole launch / health / supervision layer
                │  (parameterized by      │
                │   engine-root path)     │
                └────────────┬────────────┘
             ┌───────────────┼───────────────┐
        ┌────┴────┐     ┌────┴────┐     ┌────┴────┐
        │ Python  │     │  Rust   │     │    R    │
        │  :8100  │     │  :8200  │     │  :8300  │
        └─────────┘     └─────────┘     └─────────┘
                             ▲
                    ┌────────┴─────────┐
                    │     Gateway      │  ← typed HTTP clients only; launches nothing
                    └──────────────────┘
        ┌────────────────────┴────────────────────┐
   Desktop host (MAUI)                     Web host (Blazor Server)
   root: %APPDATA%\AIMRAN Lab\Engines      root: AppContext.BaseDirectory
```

**Benefits:**

- **(a) Drift is designed out, not patched over.** One definition; disagreement becomes impossible
  by construction rather than by discipline.
- **(b) Extensibility.** Adding a fifth engine (GPU inference, Julia, a model server, or the root
  `src/` platform) becomes a manifest entry plus a typed client — not a rewrite. This is the
  "ecosystem for future advancements" requirement, satisfied structurally.
- **(c) Uniform observability.** Health, version, and latency become one consistent surface that
  `EngineControl.razor` already has a home for.

### 4.2 Locked decisions (previously approved — do not relitigate)

**Provisioning = Hybrid.** Bundle the .NET self-contained output, the prebuilt Rust executable, and
a slim embedded Python 3.12. Detect R and guide the user to install it; degrade gracefully when
absent. Rationale: Rust and .NET produce static binaries (bundle them); Python can be embedded at
acceptable size; R cannot be legally or practically bundled.

**Engine architecture = unify on `EngineManager`.** It becomes the single launch/health/supervision
layer for **both** hosts, parameterized by an engine-root path. Gateway keeps its typed clients as
the API surface but stops launching processes. R standardizes on `:8300` everywhere.

### 4.3 Manifest schema (illustrative)

```jsonc
{
  "schemaVersion": 1,
  "engines": [
    {
      "id": "python",
      "displayName": "Python Engine",
      "defaultPort": 8100,
      "healthPath": "/health",
      "executable": "python/python.exe",
      "args": ["main.py", "--port", "{port}"],
      "workingDirectory": "python",
      "startupTimeoutSeconds": 30,
      "requestTimeoutSeconds": 60,
      "required": true,
      "supervised": true
    },
    {
      "id": "rust",
      "displayName": "Resource Engine",
      "defaultPort": 8200,
      "healthPath": "/health",
      "executable": "rust/aimran-resource-engine.exe",
      "args": ["--port", "{port}"],
      "workingDirectory": "rust",
      "startupTimeoutSeconds": 15,
      "requestTimeoutSeconds": 10,
      "required": true,
      "supervised": true
    },
    {
      "id": "r",
      "displayName": "R Statistics Engine",
      "defaultPort": 8300,
      "healthPath": "/health",
      "executable": "Rscript",
      "args": ["plumber_api.R", "--port", "{port}"],
      "workingDirectory": "r",
      "startupTimeoutSeconds": 45,
      "requestTimeoutSeconds": 60,
      "required": false,
      "supervised": false
    }
  ]
}
```

Field notes. `required: false` on R encodes graceful degradation declaratively rather than as a
special case in code. `{port}` is substituted at launch — this is what makes port override uniform
across engines. `supervised` reproduces the existing behavior of restarting only Python and Rust;
R is excluded because its interpreter may legitimately be absent. `executable` is resolved relative
to the engine root, except where it is a bare command name (`Rscript`) resolved from `PATH`, which
is what detect-and-guide provisioning requires.

---

## 5. Engineering principles

These are the "best software-engineering techniques" the work must demonstrably follow. Each is
stated so it can be checked in review, not merely aspired to.

| # | Principle | Concrete application here |
|---|---|---|
| **P1** | **Single source of truth** | Engine facts live only in `engines.json`. A second definition of a port or health path is a review-blocking defect. |
| **P2** | **Fix root causes, not symptoms** | Add the missing glob exclusions; do **not** edit `EngineControl.razor`, whose `Engine` parameter is already correct. Symptom-level edits here would mask the leak and break the component. |
| **P3** | **Fail fast, fail loud, fail diagnosably** | Replace `.unwrap()` on bind with an error path naming the port and conflict. No silent catch-all swallowing. Required-engine failure surfaces in the UI. |
| **P4** | **Trustworthy verification** | Build gates assert on the build's real exit status — never a pipe's (§2.7). Verify in dependency order: build → engine → host → degradation. |
| **P5** | **Reproducible builds** | One documented command sequence produces the distributable from a clean tree. Full `cargo clean` after any aborted build. No dependence on an operator's shell encoding. |
| **P6** | **Separation of concerns** | `EngineManager` = process lifecycle. Gateway = typed HTTP API. Manifest = configuration. No layer reaches into another's responsibility. |
| **P7** | **Graceful degradation over hard failure** | An absent optional runtime (R) degrades to a clearly-marked-unavailable engine; it never prevents startup. |
| **P8** | **Encoding and platform hygiene** | Build scripts are ASCII-safe or BOM-marked. Paths with spaces (this repo has double spaces) are always quoted. |
| **P9** | **Change one variable at a time** | Workstreams land in dependency order with a green gate between them, so a regression is attributable to one change. |
| **P10** | **Test at the seam that actually broke** | The web-routing defect was invisible to compilation; therefore assert on *rendered routes*, not build success. |

---

## 6. Workstreams

Ordered by dependency. Each ends at a green gate before the next begins (**P9**).

### W1 — .NET builds clean

**Closes:** §2.1 (all 5 errors), §2.6.

1. Add `<RazorComponent Remove="<Sibling>\**" />` and `<MauiXaml Remove="<Sibling>\**" />` to the
   desktop head's csproj for all seven sibling projects (`Web`, `Core`, `Engine`, `Engine.Tests`,
   `Gateway`, `ServiceTemplates`, `Shared`). Resolves RZ10009 ×3 and CS0246 together.
2. Replace the invalid `App.OnDestroying()` override with a supported .NET 10 MAUI shutdown hook
   (window-destroying event or `IMauiLifecycleEvents`), preserving the
   `AppLifecycleManager.ShutdownAsync()` call. Shutdown must remain wired — dropping the override
   would silently leak engine processes.
3. Align all `Microsoft.Extensions.*` to `10.0.3`.
4. Suppress `CS1591` (missing XML doc) via `NoWarn` on the projects generating documentation;
   keep documentation generation enabled.

**Gate:** `dotnet build` of the solution ⇒ **0 errors**, asserted on real exit status (**P4**).

### W2 — Web routing

**Closes:** §2.2.

1. `Program.cs:116` — register the **Shared** assembly, anchored on a type guaranteed public and
   present there (`AIMRAN_Data_Science_Lab.Components.Layout.MainLayout`, already referenced as
   `Routes.razor`'s `DefaultLayout`).
2. `Routes.razor:4` — add the matching `AdditionalAssemblies`.

Desktop routing already resolves correctly (BlazorWebView hosts the shared `Routes`); no desktop
change.

**Gate:** deferred to **G3** (§7) — this defect is invisible to the compiler (**P10**).

### W3 — Engines actually serve

**Closes:** §2.3.

All three engines acquire the same three properties: **read `--port`** (falling back to the manifest
default), **bind without panicking**, and **stay running**.

- **Python:** honor `--port`; remove `reload=True` (its forked reloader breaks parent-process
  supervision and PID-based shutdown).
- **Rust:** honor `--port`; replace the binding `.unwrap()` with a diagnosable error path (**P3**).
- **R:** add the missing `pr_run()`; honor `--port`; standardize on `:8300`.

**Gate:** for each engine, launched on a *non-default* port, `GET /health` returns 2xx and one real
endpoint responds. The non-default port is essential — it is the only way to prove `--port` is
honored rather than coincidentally hitting a hardcoded default.

### W4 — One engine layer

**Closes:** §2.4.

1. Introduce `engines.json` (§4.3) and a typed loader with validation (unknown engine id, malformed
   port, missing executable ⇒ clear startup error, not a late null-reference).
2. Parameterize `EngineManager` by engine-root path; wire the desktop host to
   `%APPDATA%\AIMRAN Lab\Engines` (honoring `AIMRAN_ENGINES_PATH`) and the web host to
   `AppContext.BaseDirectory`.
3. Make `EngineManager` the sole launcher for both hosts; remove Gateway's launch responsibility,
   retaining its typed clients (**P6**).
4. Adopt the stronger behaviors from each subsystem: Gateway's readiness polling (every 500 ms up to
   the manifest timeout) over EngineManager's single fixed-delay check, and supervision with
   exponential backoff (1→2→4→8→30 s cap) for engines marked `supervised`.
5. Drive all ports, health paths, and timeouts from the manifest — including R at `:8300`,
   eliminating the `:9000`/`:8300` contradiction.

**Gate:** both hosts start all three engines through one code path; ports and timeouts observably
originate from the manifest (changing a manifest port changes observed behavior with no code edit).

### W5 — Build → executable

**Closes:** §2.5, §2.7.

1. Retire `build-windows.ps1` in favor of a reproducible sequence (**P5**):
   `cargo build --release` → `dotnet publish` (self-contained, single-file, `win-x64`) → stage
   engines beside the executable per the manifest.
2. The driver must be ASCII-safe or BOM-marked, quote all paths (**P8**), and propagate real exit
   codes (**P4**).
3. Hybrid provisioning: bundle the Rust executable and slim embedded Python 3.12; detect R and guide
   installation if absent.

**Gate:** the full host and degradation gates in §7.

---

## 7. Verification strategy

Mechanical and in dependency order, so a failure localizes to one workstream (**P9**).

| Gate | Assertion | Catches |
|---|---|---|
| **G1 Build** | `dotnet build` ⇒ 0 errors; `cargo build --release` ⇒ binary exists on disk. Real exit status, never a pipe's. | §2.1, §2.6, §2.7 |
| **G2 Engine** | Each engine launched on a **non-default** port answers `GET /health` 2xx plus one real endpoint. | §2.3 |
| **G3 Host — web** | Load the Admin page and one Settings route: both render (no 404); three engine cards report healthy. | §2.2, §2.4 |
| **G4 Host — desktop** | Published `.exe` launches; same pages render; engines auto-start healthy; app-data directories initialize under `%APPDATA%\AIMRAN Lab\`. | §2.1, §2.4, §2.5 |
| **G5 Degradation** | With R made unavailable, the app still starts and marks R unavailable — no crash, no hang. | §4.2, P7 |
| **G6 Regression** | Engine tests pass; root `src/` platform still builds/imports (per the §3 decision). | scope protection |

**Definition of Done** = G1–G6 all green, plus: one documented command reproduces the distributable
from a clean tree.

---

## 8. Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| MAUI single-file publish trips on native assets | Medium | `IncludeNativeLibrariesForSelfExtract=true`; documented fallback to folder-based self-contained — still portable, install-free |
| Embedded Python payload size | Medium | Slim the dependency set to what the engine's endpoints actually import; measure before/after |
| R absent on target machines | **Certain** (absent here) | Designed for: `required: false` in the manifest; detect-and-guide; G5 proves it |
| No .NET 10 MAUI `OnDestroying` equivalent behaves identically | Low | Verify shutdown actually runs (engine processes terminate) rather than assuming the hook fires |
| Aborted cargo build corrupts `target/` | **Observed twice** | Build gate performs a full `cargo clean` after any abort; never rely on `cargo clean -p` |
| Slow filesystem; `env/ env1/ env2/` at repo root | High | Always scope builds to the `.sln` or a specific `.csproj`; never a root-wide glob |
| Path contains double spaces | **Certain** | Quote every path in every script (**P8**) |
| `.env` at repo root | **Certain** | Treat as sensitive: never read, print, or commit its contents |

---

## 9. Open recommendation — version control

This tree is **not a git repository** (`git rev-parse` ⇒ *fatal: not a git repository*). For work of
this scope that is itself a significant engineering risk: no rollback, no bisect, no attributable
history across five workstreams touching build files, hosts, and three runtimes.

**Recommended before implementation begins:** initialize git — but only *after* authoring a
`.gitignore` covering at minimum `.env`, `env/`, `env1/`, `env2/`, `bin/`, `obj/`, `target/`,
`publish/`, and `__pycache__/`. Initializing without it would stage secrets and gigabytes of
virtualenv and build artifacts.

This is a recommendation, not a prerequisite; it is flagged rather than performed because it is the
user's decision.

---

## 10. Decisions log

| # | Decision | Status |
|---|---|---|
| D1 | Provisioning = **Hybrid** (bundle .NET + Rust + embedded Python 3.12; detect/guide R) | Approved |
| D2 | Engine architecture = **unify on `EngineManager`**; Gateway keeps typed clients only | Approved |
| D3 | R standardizes on **`:8300`** everywhere | Approved |
| D4 | Root `src/` platform = **regression check only** in Foundation (option A) | Approved |
| D5 | Engine facts centralized in **`engines.json`** manifest | Approved |
| D6 | Retire `build-windows.ps1`; replace with a reproducible, encoding-safe driver | Approved |
| D7 | Python pinned to **3.12** (not the installed 3.14.7) | Approved |

---

## 11. Traceability — defect to workstream to gate

| Defect | Workstream | Gate |
|---|---|---|
| §2.1 Razor glob leak (RZ10009 ×3, CS0246) | W1 | G1 |
| §2.1 CS0115 `OnDestroying` | W1 | G1, G4 |
| §2.2 Web routes 404 | W2 | G3 |
| §2.3 Python port/reload | W3 | G2 |
| §2.3 Rust port/panic | W3 | G2 |
| §2.3 R missing `pr_run()` | W3 | G2 |
| §2.4 Two engine subsystems | W4 | G3, G4 |
| §2.5 Publish script cannot parse | W5 | G4 |
| §2.6 NuGet drift | W1 | G1 |
| §2.7 Pipe-masked exit codes | W5 | G1 (all gates) |
