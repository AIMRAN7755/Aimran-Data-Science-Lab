# 🚀 AIMRAN Lab - Developer Quick Start Guide

## Current Status: Architecture Phase ✅ COMPLETE

---

## 📂 Project Structure

```
AimranDataScienceLab/
├── AimranDataScienceLab.Shared/
│   ├── Domain/
│   │   ├── RoleManagement/          (ProfessionalRole, RoleType)
│   │   ├── Projects/                (DataScienceProject, Workflows)
│   │   ├── DataManagement/          (Dataset, DatasetVersion)
│   │   ├── MachineLearning/         (MLModel, ModelVersion, Experiment)
│   │   ├── DataPipelines/           (DataPipeline, PipelineStep)
│   │   ├── Analytics/               (InsightReport, Dashboard)
│   │   └── AI/                      (AIAssistant, AutoML, Workflow)
│   ├── Services/
│   │   ├── RoleManagement/          (IRoleService, RoleService)
│   │   ├── Authorization/           (IAuthorizationService)
│   │   ├── Projects/                (IProjectService, ProjectService)
│   │   ├── Analytics/               (SQLExplorer, Dashboard, Reports)
│   │   ├── MachineLearning/         (MLStudio, Features, Experiments)
│   │   ├── DataEngineering/         (Pipelines, Connectors, ETL)
│   │   ├── Deployment/              (DeploymentCenter, APIs, Monitoring)
│   │   ├── AI/                      (AIAssistant, Insights, Workflow)
│   │   ├── Events/                  (EventBus, Notifications, Versions)
│   │   └── Administration/          (UserMgmt, Monitoring, Audit)
│   └── Components/
│       ├── EngineControl.razor      (Engine lifecycle UI)
│       └── Pages/Admin/
│           └── AdminDashboard.razor (Admin console)
│
├── AIMRAN Data Science Lab.csproj   (MAUI Windows host)
├── AimranDataScienceLab.Engine/     (External engine layer)
├── AimranDataScienceLab.Web/        (Web host)
└── Tests/                           (Unit tests)
```

---

## 🎯 Key Files to Know

### 1. Role-Based Access Control
**File**: `Services/RoleManagement/RoleService.cs`
```csharp
// 4 default roles pre-configured
- DataAnalyst (SQL, Dashboards, Reports)
- DataScientist (ML, Features, Experiments)
- DataEngineer (Pipelines, ETL, Quality)
- AIEngineer (Deployment, APIs, Monitoring)
```

### 2. Unified Project Context
**File**: `Services/Projects/ProjectService.cs`
```csharp
// All roles work in same project
// Each user sees role-specific resources
// Cross-role workflows coordinated via events
```

### 3. Domain Events (Communication)
**File**: `Services/Events/DomainEvents.cs`
```csharp
// 30+ event types for inter-role communication
// Example: DatasetCleanedEvent → triggers scientist notification
// Example: ModelDeployedEvent → triggers monitoring setup
```

### 4. Service Contracts
All in `Services/{role}/` directories:
```
DataAnalystServices.cs     → SQLExplorer, Dashboard, Reports, EDA
DataScientistServices.cs   → MLStudio, Features, Experiments, AutoML
DataEngineerServices.cs    → Pipelines, Connectors, ETL, Quality
AIEngineerServices.cs      → Deployment, APIs, Serving, Monitoring
```

---

## 🔄 Common Workflows

### Adding a New Feature

1. **Create Domain Model** (if new resource type)
   ```csharp
   // Example: AimranDataScienceLab.Shared/Domain/Analytics/CustomReport.cs
   public class CustomReport
   {
	   public Guid Id { get; set; }
	   public Guid ProjectId { get; set; }
	   // ... properties
   }
   ```

2. **Create Service Contract** (interface)
   ```csharp
   // Example: Services/Analytics/ICustomReportService.cs
   public interface ICustomReportService
   {
	   Task<CustomReport> CreateAsync(CustomReport report);
	   Task<CustomReport?> GetAsync(Guid id);
	   // ...
   }
   ```

3. **Implement Service**
   ```csharp
   // Example: Services/Analytics/CustomReportService.cs
   public class CustomReportService : ICustomReportService
   {
	   // Implementation with DbContext (when DB layer added)
   }
   ```

4. **Wire Dependency Injection**
   ```csharp
   // In MauiProgram.cs or Web host startup
   builder.Services.AddScoped<ICustomReportService, CustomReportService>();
   ```

5. **Create Blazor Component** (if UI needed)
   ```csharp
   // Components/Analytics/CustomReportEditor.razor
   @implements IAsyncDisposable
   @inject ICustomReportService ReportService
   ```

---

## 🏗️ Next Phase: Database Implementation

### Step 1: Create DbContext
```csharp
// AimranDataScienceLab.Shared/Data/AimranDbContext.cs
public class AimranDbContext : DbContext
{
	public DbSet<DataScienceProject> Projects { get; set; }
	public DbSet<AdminUser> Users { get; set; }
	public DbSet<Dataset> Datasets { get; set; }
	public DbSet<MLModel> Models { get; set; }
	// ... all entities
}
```

### Step 2: Add Migrations
```bash
cd AimranDataScienceLab.Shared
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Step 3: Implement Repository Pattern
```csharp
public interface IRepository<T> where T : class
{
	Task<T?> GetByIdAsync(Guid id);
	Task<IEnumerable<T>> GetAllAsync();
	Task AddAsync(T entity);
	Task UpdateAsync(T entity);
	Task DeleteAsync(Guid id);
}
```

---

## 🛠️ Available Services

### By Role

**Data Analyst** (4 services)
- ISQLExplorerService
- IDashboardBuilderService
- IReportGeneratorService
- IEDAService

**Data Scientist** (5 services)
- IMLStudioService
- IFeatureEngineeringService
- IExperimentTrackerService
- IModelRegistryService
- IAutoMLService

**Data Engineer** (5 services)
- IPipelineBuilderService
- IDataConnectorService
- IETLService
- IDataQualityService
- ISchedulerService

**AI Engineer** (5 services)
- IDeploymentCenterService
- IAPIManagerService
- IModelServingService
- IMonitoringService
- IDriftDetectionService

### Cross-Role

- **IAIAssistantService** - AI queries and recommendations
- **IAutoInsightEngine** - Pattern discovery
- **IWorkflowOrchestrationService** - Intelligent workflow planning
- **IEventBus** - Pub-sub communication
- **INotificationService** - Alerts and updates
- **IVersionControlService** - Resource versioning
- **IAuditTrailService** - Compliance logging
- **IUserManagementService** - User admin
- **ISystemMonitoringService** - System health

---

## 📋 Checklists

### ✅ Architecture Phase (COMPLETE)
- [x] 4 role models defined
- [x] RBAC system designed
- [x] 20+ service contracts
- [x] 40+ domain models
- [x] 30+ domain events
- [x] Cross-role communication
- [x] Admin infrastructure
- [x] Build successful

### ⬜ Database Phase (NEXT)
- [ ] DbContext created
- [ ] All entities mapped
- [ ] Relationships configured
- [ ] Migrations generated
- [ ] Seed data added
- [ ] Repository pattern implemented
- [ ] Services use repositories

### ⬜ Implementation Phase (AFTER)
- [ ] Service methods implemented
- [ ] Business logic complete
- [ ] Error handling added
- [ ] Validation added
- [ ] Unit tests written
- [ ] Integration tests written

### ⬜ UI Phase (AFTER THAT)
- [ ] Blazor pages created
- [ ] Components built
- [ ] Forms validated
- [ ] Tables and grids
- [ ] Real-time updates (SignalR)
- [ ] Role-specific views

---

## 🎓 Code Examples

### Using ProjectService
```csharp
@inject IProjectService ProjectService

// Create project
var project = new DataScienceProject { Name = "Churn Prediction" };
project = await ProjectService.CreateProjectAsync(project);

// Get user's project context
var context = await ProjectService.GetProjectContextAsync(projectId, userId);
// context.UserRole tells you what role user has in this project
// context.VisibleResources filters by role
```

### Using Event Bus
```csharp
@inject IEventBus EventBus

// Subscribe to events
await EventBus.SubscribeAsync<ModelTrainingCompletedEvent>(async @event =>
{
	// Handle model completion
	await NotifyUserAsync($"Model trained with {(@event.Metrics["accuracy"])} accuracy");
});

// Publish event
await EventBus.PublishAsync(new DatasetCleanedEvent 
{ 
	DatasetId = datasetId,
	ProjectId = projectId,
	InitiatedByUserId = userId
});
```

### Using Authorization
```csharp
@inject IAuthorizationService AuthService

// Check permission
if (!await AuthService.CanUserAccessProjectAsync(userId, projectId))
{
	throw new UnauthorizedAccessException();
}

// Throw if unauthorized
await AuthService.ThrowIfUnauthorizedAsync(userId, "dataset", PermissionAction.Read);
```

---

## 🔌 Integration Points

### With External Engines
```csharp
// In EngineManager (already implemented)
await _engineManager.StartEngineAsync("python");
var status = await _engineManager.GetStatusAsync();
var logs = await _engineManager.GetEngineLogsAsync("python");
```

### With MAUI Host
```csharp
// MauiProgram.cs already configured
// App lifecycle handled
// AppData paths managed
// Services registered
```

### With SignalR (Ready for Real-Time)
```csharp
// When implementing real-time features:
builder.Services.AddSignalR();
// Create hub for notifications
public class ProjectHub : Hub { }
```

---

## 📚 Documentation Files

- **ARCHITECTURE_DOCUMENTATION.md** - Complete architecture overview
- **IMPLEMENTATION_COMPLETE.md** - What was built and how
- **IMPLEMENTATION_SUCCESS.md** - Build status and next steps
- **This file** - Developer quick reference

---

## 🚨 Common Pitfalls

1. **Forgetting to register service in DI**
   ```csharp
   // Add this in startup
   builder.Services.AddScoped<IMyService, MyService>();
   ```

2. **Not checking permissions**
   ```csharp
   // Always check before allowing user action
   await _authService.ThrowIfUnauthorizedAsync(userId, resource, action);
   ```

3. **Forgetting to publish events**
   ```csharp
   // After important action, publish event
   await _eventBus.PublishAsync(new ImportantEvent { /* ... */ });
   ```

4. **Not logging to audit trail**
   ```csharp
   // Add audit entry for compliance
   await _auditService.LogActionAsync(new AuditEntry { /* ... */ });
   ```

---

## 💪 Tips for Success

1. **Follow the pattern**: Each role ecosystem follows same pattern (Service → Domain → Events)
2. **Async all the way**: Services are async, use `await` everywhere
3. **Test early**: Write unit tests as you implement
4. **Stay DRY**: Share code via interfaces and base classes
5. **Document as you go**: Add XML comments to public methods
6. **Use dependency injection**: Never `new` up dependencies

---

## 🎯 Success Criteria

Your implementation is successful when:

- ✅ Database migrations run without errors
- ✅ All services have implementations
- ✅ UI components render correctly
- ✅ End-to-end workflows complete
- ✅ Audit logs record all actions
- ✅ Events trigger properly
- ✅ RBAC enforces permissions
- ✅ Performance meets targets

---

## 📞 Key Contacts

- **Architecture Questions**: See ARCHITECTURE_DOCUMENTATION.md
- **Implementation Details**: See IMPLEMENTATION_COMPLETE.md
- **Build Issues**: Run `dotnet build` in workspace root
- **Pattern Questions**: Look at existing services (RoleService, ProjectService)

---

**Ready to build?** Start with the Database phase!

Created: 2024 | AIMRAN Data Science Lab | Professional Enterprise Edition
