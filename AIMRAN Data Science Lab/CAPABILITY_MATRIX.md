# AIMRAN Data Science Lab - Role-Based Capability Matrix

## Quick Reference Matrix

| Capability | Viewer | Analyst | DataScientist | DataEngineer | AIEngineer | Admin |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **COLLABORATION** |
| ViewCollaborationDashboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewActivityFeed | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewTeamPresence | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| JoinLiveSession | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **LIVE EDITING** |
| EditLive | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| ViewOtherCursors | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewConflictResolution | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ResolveConflicts | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| **COMMENTING** |
| ViewComments | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| AddComments | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ReplyToComments | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ResolveComments | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| ManageComments | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| MentionUsers | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **EXPERIMENTATION** |
| ViewExperiments | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| RunExperiments | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| CreateExperiments | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| CompareExperiments | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| ManageExperiments | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| **DATA MANAGEMENT** |
| ViewDatasets | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ImportDatasets | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| ExportDatasets | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ProfileData | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| ManageDatasets | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| **PIPELINE MANAGEMENT** |
| ViewPipelines | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| ExecutePipelines | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| CreatePipelines | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| EditPipelines | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| ManagePipelines | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| ConfigureIntegrations | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| MonitorPipelines | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| **MODEL MANAGEMENT** |
| ViewModels | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| TrainModels | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| CreateModels | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| EvaluateModels | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| VersionModels | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| ManageModels | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| **DEPLOYMENT** |
| ViewDeployments | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| DeployModels | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ |
| ConfigureDeployments | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| ManageDeployments | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| OptimizeDeployments | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| ManageAPIs | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| **MONITORING** |
| ViewMetrics | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewLogs | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| MonitorPerformance | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| ManageAlerts | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| **ADMINISTRATION** |
| ManageUsers | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| ViewAuditLogs | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| ManageSecrets | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| ConfigureSystem | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **PROJECTS** |
| ViewProjects | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| CreateProject | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| DeleteProject | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **CODE EXECUTION** |
| ExecutePython | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| ExecuteR | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| ExecuteNotebook | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |

---

## Legend
- ✅ = Permission Granted
- ❌ = Permission Denied

---

## Role Descriptions

### Viewer
**Use Case**: Stakeholders, clients, read-only observers
- View dashboards, datasets, models, and experiments
- See team activity and presence
- View comments and metrics
- **Cannot**: Modify anything, execute code, run pipelines

### Analyst
**Use Case**: Business analysts, data explorers, report creators
- All Viewer capabilities
- Import/export datasets
- Execute Python, R, Notebooks
- Add comments and participate in discussions
- Profile data for quality assessment
- View pipeline and model status
- **Cannot**: Modify infrastructure, train models, deploy

### DataScientist
**Use Case**: ML engineers, model builders, researchers
- All Analyst capabilities
- **Full**: Experimentation (create, run, compare, manage)
- **Full**: Model management (train, create, evaluate, version)
- Deploy models
- Edit documents in real-time
- Resolve editing conflicts
- Create projects
- **Cannot**: Manage infrastructure, optimize deployments

### DataEngineer
**Use Case**: Platform engineers, ETL specialists, data architects
- All Analyst capabilities (partial)
- **Full**: Pipeline management (create, edit, execute, monitor)
- **Full**: Data management (import, export, profile, manage)
- Configure integrations
- Monitor pipelines and performance
- View logs
- Run experiments (view only)
- **Focus**: Data infrastructure and integrations
- **Cannot**: Train models, deploy models

### AIEngineer
**Use Case**: ML Ops engineers, API developers, deployment specialists
- **Full**: Model management (all operations)
- **Full**: Deployment (configure, manage, optimize)
- **Full**: Monitoring (metrics, logs, alerts)
- **Full**: API management
- Deploy and serve models
- Optimize deployment performance
- Compare experiments
- **Focus**: Production readiness and serving
- **Cannot**: Data management (mostly read), infrastructure

### Admin
**Use Case**: System administrators, security officers
- **Full Access**: All capabilities
- User management
- Audit logs
- Secret management
- System configuration

---

## Capability Categories by Specialization

### Collaboration (Universal Access)
**All roles can participate in teamwork**
- Real-time activity feeds
- Team presence
- Comment threads (write access varies)
- Live editing (varies by role)

### Data Operations (Analyst → Engineer)
**Escalating from view to full control**
- Viewer: Read datasets
- Analyst: Import/export, profile
- DataEngineer: Full management

### Model Lifecycle (DataScientist → AIEngineer)
**Escalating from experimentation to production**
- DataScientist: Experiment and train
- AIEngineer: Deploy and optimize

### Infrastructure (DataEngineer Specialized)
**Complete pipeline and integration management**
- DataEngineer: All pipeline operations
- DataEngineer: All integration configuration

### Production Serving (AIEngineer Specialized)
**Deployment and performance optimization**
- AIEngineer: Model deployment
- AIEngineer: API management
- AIEngineer: Performance optimization

---

## Permission Grant Rules

### Inheritance Rules
1. Higher roles inherit all lower role permissions
2. Admin has all permissions
3. No horizontal permission transfer (e.g., Analyst → Engineer)

### Specialization Rules
1. DataScientist: Experimentation focus (models/experiments)
2. DataEngineer: Infrastructure focus (pipelines/data)
3. AIEngineer: Production focus (deployment/monitoring)

### Hierarchy
```
Admin
  ├── User Management
  ├── Audit & Security
  ├── System Configuration
  │
  └── Operational Roles:
	  ├── DataScientist (Experimentation)
	  │   └── Analyst
	  │       └── Viewer
	  │
	  ├── DataEngineer (Infrastructure)
	  │   └── Analyst
	  │       └── Viewer
	  │
	  └── AIEngineer (Production)
		  └── Analyst
			  └── Viewer
```

---

## Usage Examples in Code

### Check if user can deploy
```csharp
var canDeploy = await capabilityService.IsCapabilityAvailableForRoleAsync(
	RoleType.AIEngineer, 
	LabCapabilities.DeployModels);
```

### Get all capabilities for a role
```csharp
var aiEngineerCaps = await capabilityService.GetCapabilitiesForRoleAsync(RoleType.AIEngineer);
// Returns 38 capabilities
```

### Create role-based UI
```razor
@if (await CapabilityService.IsCapabilityAvailableForRoleAsync(currentRole, LabCapabilities.DeployModels))
{
	<button @onclick="DeployModel">Deploy to Production</button>
}
else
{
	<p class="text-muted">Deploy capability requires AIEngineer or Admin role</p>
}
```

---

## Statistics

| Metric | Count |
|--------|-------|
| Total Capabilities | 50+ |
| Roles (excluding Admin) | 5 |
| Roles (including Admin) | 6 |
| Collaboration Capabilities | 4 |
| Live Editing Capabilities | 4 |
| Commenting Capabilities | 6 |
| Experimentation Capabilities | 5 |
| Data Management Capabilities | 5 |
| Pipeline Capabilities | 7 |
| Model Capabilities | 6 |
| Deployment Capabilities | 6 |
| Monitoring Capabilities | 4 |
| Admin Capabilities | 4 |
| Project Capabilities | 3 |
| Code Execution Capabilities | 3 |
| Avg Capabilities per Role | 28 |
| Min (Viewer) | 9 |
| Max (Admin) | 50+ |

---

## Future Enhancements

1. **Custom Roles**: Organizations can create domain-specific roles
2. **Permission Composition**: Combine capabilities into permission sets
3. **Time-Based Permissions**: Temporary elevated access
4. **Team-Based Permissions**: Role varies by project/team
5. **Resource-Based Permissions**: Fine-grained access per resource
6. **Delegation**: Users can delegate capabilities within constraints

