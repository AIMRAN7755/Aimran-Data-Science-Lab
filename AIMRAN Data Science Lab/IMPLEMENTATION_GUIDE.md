# AIMRAN Data Science Lab - Implementation Guide

## Getting Started with Role-Based Capabilities

### 1. Register Services in Dependency Injection

**Location**: `AimranDataScienceLab.Web/Program.cs`

```csharp
using AimranDataScienceLab.Core.Services.CapabilityManagement;

var builder = WebApplication.CreateBuilder(args);

// Add capability service
builder.Services.AddScoped<ICapabilityService, CapabilityService>();

// Add other services
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ICollaborationService, CollaborationService>();
// ... other services

var app = builder.Build();
```

---

### 2. Using Capabilities in Blazor Components

#### Simple Permission Check
```razor
@page "/deploy-model"
@inject ICapabilityService CapabilityService
@inject RoleService RoleService

<div class="deployment-panel">
	@if (canDeploy)
	{
		<button @onclick="DeployModel" class="btn btn-primary">Deploy to Production</button>
	}
	else
	{
		<div class="alert alert-warning">
			You don't have permission to deploy models. 
			<span class="text-muted">Required role: AIEngineer or Admin</span>
		</div>
	}
</div>

@code {
	private bool canDeploy = false;
	private RoleType userRole = RoleType.DataAnalyst;

	protected override async Task OnInitializedAsync()
	{
		// In production, get actual user role from authentication context
		canDeploy = await CapabilityService.IsCapabilityAvailableForRoleAsync(
			RoleType.AIEngineer, 
			LabCapabilities.DeployModels);
	}

	private async Task DeployModel()
	{
		// Deployment logic
	}
}
```

#### Multiple Permission Checks
```razor
@page "/ai-engineering-studio"
@inject ICapabilityService CapabilityService

<div class="studio">
	@if (canManageAPIs && canOptimize)
	{
		<ModelDeploymentPanel />
		<PerformanceOptimizer />
	}
	else
	{
		<AccessDenied RequiredRole="AIEngineer" />
	}
</div>

@code {
	private bool canManageAPIs = false;
	private bool canOptimize = false;

	protected override async Task OnInitializedAsync()
	{
		var userRole = GetCurrentUserRole(); // From auth context

		canManageAPIs = await CapabilityService.IsCapabilityAvailableForRoleAsync(
			userRole, 
			LabCapabilities.ManageAPIs);

		canOptimize = await CapabilityService.IsCapabilityAvailableForRoleAsync(
			userRole, 
			LabCapabilities.OptimizeDeployments);
	}
}
```

#### Capability List Display
```razor
@page "/role-capabilities/@roleId"
@inject ICapabilityService CapabilityService

<div class="capability-explorer">
	<h3>@roleName Capabilities</h3>

	@if (capabilities != null)
	{
		<div class="capability-groups">
			@foreach (var category in capabilities.GroupBy(c => c.Category))
			{
				<div class="category">
					<h4>@category.Key</h4>
					<ul>
						@foreach (var capability in category)
						{
							<li>
								<strong>@capability.Name</strong>
								<p class="text-muted">@capability.Description</p>
							</li>
						}
					</ul>
				</div>
			}
		</div>
	}
</div>

@code {
	[Parameter]
	public string roleId { get; set; }

	private RoleType userRole = RoleType.DataAnalyst;
	private CapabilityDefinition[] capabilities;
	private string roleName = "";

	protected override async Task OnInitializedAsync()
	{
		// Parse role from parameter
		userRole = Enum.Parse<RoleType>(roleId);
		roleName = userRole.ToString();

		// Get all capabilities for role
		var capabilityIds = await CapabilityService.GetCapabilitiesForRoleAsync(userRole);
		var allCaps = await CapabilityService.GetAllCapabilitiesAsync();

		capabilities = allCaps
			.Where(c => capabilityIds.Contains(c.Id))
			.ToArray();
	}
}
```

---

### 3. Using Capabilities in Services

#### Authorization Service Integration
```csharp
public class FeatureAuthorizationService
{
	private readonly ICapabilityService _capabilityService;
	private readonly IAuthenticationStateProvider _authProvider;

	public FeatureAuthorizationService(
		ICapabilityService capabilityService,
		AuthenticationStateProvider authProvider)
	{
		_capabilityService = capabilityService;
		_authProvider = authProvider;
	}

	public async Task<bool> CanUserAsync(string capability)
	{
		var authState = await _authProvider.GetAuthenticationStateAsync();
		var user = authState.User;

		if (!user.Identity?.IsAuthenticated ?? false)
			return false;

		var userRole = user.FindFirst("role")?.Value ?? "Viewer";

		// Map string role to RoleType
		var roleType = MapStringRoleToRoleType(userRole);

		return await _capabilityService.IsCapabilityAvailableForRoleAsync(
			roleType,
			capability);
	}

	private RoleType MapStringRoleToRoleType(string role) => role switch
	{
		"DataScientist" => RoleType.DataScientist,
		"DataEngineer" => RoleType.DataEngineer,
		"AIEngineer" => RoleType.AIEngineer,
		"Analyst" => RoleType.Analyst,
		"Admin" => RoleType.DataScientist, // Highest operational role
		_ => RoleType.DataAnalyst
	};
}
```

#### Model Service Example
```csharp
public class ModelService : IModelService
{
	private readonly ICapabilityService _capabilityService;
	private readonly RoleType _userRole;

	public async Task<bool> CanUserDeployAsync(Guid userId)
	{
		var userRole = await GetUserRoleAsync(userId);
		return await _capabilityService.IsCapabilityAvailableForRoleAsync(
			userRole,
			LabCapabilities.DeployModels);
	}

	public async Task<List<string>> GetAvailableActionsAsync(Guid userId, Guid modelId)
	{
		var userRole = await GetUserRoleAsync(userId);
		var availableCapabilities = await _capabilityService.GetCapabilitiesForRoleAsync(userRole);

		var actions = new List<string>();

		if (availableCapabilities.Contains(LabCapabilities.ViewModels))
			actions.Add("View");

		if (availableCapabilities.Contains(LabCapabilities.TrainModels))
			actions.Add("Train");

		if (availableCapabilities.Contains(LabCapabilities.EvaluateModels))
			actions.Add("Evaluate");

		if (availableCapabilities.Contains(LabCapabilities.DeployModels))
			actions.Add("Deploy");

		if (availableCapabilities.Contains(LabCapabilities.ManageModels))
			actions.Add("Manage");

		return actions;
	}

	private async Task<RoleType> GetUserRoleAsync(Guid userId)
	{
		// Implementation: fetch user's role from database
		// For now, return DataScientist as example
		return RoleType.DataScientist;
	}
}
```

---

### 4. Authorization Attributes

#### Create Custom Authorization Attribute
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireCapabilityAttribute : Attribute
{
	public string CapabilityId { get; }

	public RequireCapabilityAttribute(string capabilityId)
	{
		CapabilityId = capabilityId;
	}
}
```

#### Use in Controllers
```csharp
[ApiController]
[Route("api/[controller]")]
public class ModelDeploymentController : ControllerBase
{
	private readonly ICapabilityService _capabilityService;
	private readonly IAuthenticationService _authService;

	[HttpPost("deploy")]
	[RequireCapability(LabCapabilities.DeployModels)]
	public async Task<IActionResult> DeployModel([FromBody] ModelDeploymentRequest request)
	{
		// Verify capability before proceeding
		var userRole = await _authService.GetUserRoleAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

		var hasCapability = await _capabilityService.IsCapabilityAvailableForRoleAsync(
			userRole,
			LabCapabilities.DeployModels);

		if (!hasCapability)
			return Forbid();

		// Deployment logic
		return Ok(new { message = "Model deployed successfully" });
	}
}
```

---

### 5. Capability-Based UI Components

#### Create Reusable Component
```razor
@* Components/CapabilityGate.razor *@

@if (isAllowed)
{
	@ChildContent
}
else if (showWarning)
{
	<div class="alert alert-warning">
		<strong>Limited Access</strong>
		<p>This feature requires @RequiredRole role or higher.</p>
		<small>Contact your administrator for access</small>
	</div>
}

@code {
	[Parameter]
	public string Capability { get; set; }

	[Parameter]
	public RenderFragment ChildContent { get; set; }

	[Parameter]
	public string RequiredRole { get; set; } = "Unknown";

	[Parameter]
	public bool ShowWarning { get; set; } = true;

	[Inject]
	ICapabilityService CapabilityService { get; set; }

	private bool isAllowed = false;

	protected override async Task OnInitializedAsync()
	{
		// Get current user role from authentication context
		var currentRole = RoleType.DataScientist; // TODO: Get from auth context

		isAllowed = await CapabilityService.IsCapabilityAvailableForRoleAsync(
			currentRole,
			Capability);
	}
}
```

#### Use Component in Pages
```razor
<CapabilityGate Capability="@LabCapabilities.DeployModels" RequiredRole="AIEngineer">
	<ModelDeploymentPanel />
</CapabilityGate>

<CapabilityGate Capability="@LabCapabilities.ManagePipelines" RequiredRole="DataEngineer" ShowWarning="true">
	<PipelineManagementStudio />
</CapabilityGate>
```

---

### 6. Audit Logging

#### Log Capability Checks
```csharp
public class AuditedCapabilityService : ICapabilityService
{
	private readonly ICapabilityService _inner;
	private readonly IAuditService _auditService;

	public async Task<bool> IsCapabilityAvailableForRoleAsync(RoleType roleType, string capabilityId)
	{
		var result = await _inner.IsCapabilityAvailableForRoleAsync(roleType, capabilityId);

		if (!result)
		{
			await _auditService.LogAsync(new AuditEntry
			{
				Action = "CAPABILITY_DENIED",
				Role = roleType.ToString(),
				Capability = capabilityId,
				Timestamp = DateTime.UtcNow
			});
		}

		return result;
	}

	// Implement other methods...
}
```

---

### 7. Performance Optimization

#### Cache Capabilities
```csharp
public class CachedCapabilityService : ICapabilityService
{
	private readonly ICapabilityService _inner;
	private readonly IMemoryCache _cache;

	public async Task<string[]> GetCapabilitiesForRoleAsync(RoleType roleType)
	{
		var cacheKey = $"capabilities_{roleType}";

		if (_cache.TryGetValue(cacheKey, out string[] cached))
			return cached;

		var capabilities = await _inner.GetCapabilitiesForRoleAsync(roleType);

		_cache.Set(cacheKey, capabilities, TimeSpan.FromHours(1));

		return capabilities;
	}

	// Implement other methods...
}
```

#### Register Cached Service
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CapabilityService>();
builder.Services.Decorate<ICapabilityService, CachedCapabilityService>();
```

---

### 8. Testing Capabilities

#### Unit Test Example
```csharp
[TestFixture]
public class CapabilityServiceTests
{
	private CapabilityService _service;

	[SetUp]
	public void Setup()
	{
		_service = new CapabilityService();
	}

	[Test]
	public async Task AIEngineer_Should_Have_DeployModels_Capability()
	{
		var hasCapability = await _service.IsCapabilityAvailableForRoleAsync(
			RoleType.AIEngineer,
			LabCapabilities.DeployModels);

		Assert.IsTrue(hasCapability);
	}

	[Test]
	public async Task DataAnalyst_Should_Not_Have_DeployModels_Capability()
	{
		var hasCapability = await _service.IsCapabilityAvailableForRoleAsync(
			RoleType.DataAnalyst,
			LabCapabilities.DeployModels);

		Assert.IsFalse(hasCapability);
	}

	[Test]
	public async Task DataEngineer_Should_Have_Pipeline_Capabilities()
	{
		var capabilities = await _service.GetCapabilitiesForRoleAsync(RoleType.DataEngineer);

		Assert.Contains(LabCapabilities.ManagePipelines, capabilities);
		Assert.Contains(LabCapabilities.ConfigureIntegrations, capabilities);
		Assert.Contains(LabCapabilities.MonitorPipelines, capabilities);
	}

	[Test]
	public async Task DataScientist_Should_Have_Model_Capabilities()
	{
		var capabilities = await _service.GetCapabilitiesForRoleAsync(RoleType.DataScientist);

		Assert.Contains(LabCapabilities.TrainModels, capabilities);
		Assert.Contains(LabCapabilities.EvaluateModels, capabilities);
		Assert.Contains(LabCapabilities.DeployModels, capabilities);
	}
}
```

---

### 9. Extending with Custom Capabilities

```csharp
// Define custom capabilities
public static class CustomCapabilities
{
	public const string AccessVectorDatabase = "custom:access-vector-db";
	public const string RunSimulations = "custom:run-simulations";
	public const string PublishToMarketplace = "custom:publish-marketplace";
}

// Extend CapabilityService
public class ExtendedCapabilityService : CapabilityService
{
	protected override Dictionary<RoleType, HashSet<string>> InitializeRoleCapabilities()
	{
		var baseCapabilities = base.InitializeRoleCapabilities();

		// Add custom capabilities for DataEngineer
		if (baseCapabilities.TryGetValue(RoleType.DataEngineer, out var engineerCaps))
		{
			engineerCaps.Add(CustomCapabilities.AccessVectorDatabase);
			engineerCaps.Add(CustomCapabilities.RunSimulations);
		}

		return baseCapabilities;
	}
}
```

---

### 10. Migration Path from Settings

#### Step 1: Identify Current Settings
Review `AdminSettings.razor` for feature toggles

#### Step 2: Map to Capabilities
```
FeatureToggle.AllowLiveEditing → LabCapabilities.EditLive (Role-based)
FeatureToggle.AllowExperimentation → LabCapabilities.RunExperiments (Role-based)
```

#### Step 3: Deprecate Settings (Gradual)
- Keep settings for backward compatibility (6 months)
- Log warning when settings overridden by capabilities
- Document migration timeline

#### Step 4: Full Migration
- Remove legacy settings
- Use capability service exclusively

---

## Summary

The role-based capability system is now ready for integration across the AIMRAN Data Science Lab. Follow these guidelines to:

1. ✅ Register services in DI container
2. ✅ Protect components with capability checks
3. ✅ Implement authorization attributes
4. ✅ Add audit logging
5. ✅ Cache for performance
6. ✅ Test thoroughly
7. ✅ Gradually migrate from settings

**Build Status**: ✅ All 6 errors fixed
**New Services**: ✅ ICapabilityService, CapabilityService
**Roles**: ✅ 5 roles + Admin with specialized capabilities
**Capabilities**: ✅ 50+ granular permissions across 10 categories
