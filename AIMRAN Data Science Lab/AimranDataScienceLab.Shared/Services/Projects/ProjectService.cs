using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.Projects;
using AimranDataScienceLab.Core.Domain.RoleManagement;
using AimranDataScienceLab.Core.Services.RoleManagement;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;
using AIMRAN_Data_Science_Lab.Services.Collaboration;

namespace AimranDataScienceLab.Shared.Services.Projects;

/// <summary>
/// Implementation of project service with full workflow orchestration.
/// Manages unified project context and inter-role communication.
/// Integrated with real-time collaboration for activity tracking and team presence.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly Dictionary<Guid, DataScienceProject> _projects = new();
    private readonly Dictionary<Guid, List<ProjectWorkflow>> _projectWorkflows = new();
    private readonly IRoleService _roleService;
    private readonly ICollaborationService _collaborationService;
    private readonly IPresenceService _presenceService;
    private readonly List<ProjectEvent> _eventLog = new();

    public ProjectService(IRoleService roleService, 
        ICollaborationService collaborationService,
        IPresenceService presenceService)
    {
        _roleService = roleService;
        _collaborationService = collaborationService;
        _presenceService = presenceService;
    }

    public async Task<DataScienceProject> CreateProjectAsync(DataScienceProject project)
    {
        project.Id = Guid.NewGuid();
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        project.Status = ProjectStatus.Active;
        project.CurrentPhase = ProjectPhase.ProblemDefinition;

        _projects[project.Id] = project;
        _projectWorkflows[project.Id] = new List<ProjectWorkflow>();

        return project;
    }

    public Task<DataScienceProject?> GetProjectAsync(Guid projectId)
    {
        return Task.FromResult(
            _projects.TryGetValue(projectId, out var project) ? project : null);
    }

    public Task<DataScienceProject[]> GetUserProjectsAsync(Guid userId)
    {
        var userProjects = _projects.Values
            .Where(p => p.OwnerId == userId || p.TeamMemberIds.Contains(userId))
            .ToArray();

        return Task.FromResult(userProjects);
    }

    public Task UpdateProjectAsync(DataScienceProject project)
    {
        if (_projects.ContainsKey(project.Id))
        {
            project.UpdatedAt = DateTime.UtcNow;
            _projects[project.Id] = project;
        }
        return Task.CompletedTask;
    }

    public Task DeleteProjectAsync(Guid projectId)
    {
        _projects.Remove(projectId);
        _projectWorkflows.Remove(projectId);
        return Task.CompletedTask;
    }

    public async Task ArchiveProjectAsync(Guid projectId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            project.Status = ProjectStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task AddTeamMemberAsync(Guid projectId, Guid userId, string role)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var memberList = project.TeamMemberIds.ToList();
            if (!memberList.Contains(userId))
                memberList.Add(userId);

            project.TeamMemberIds = memberList.ToArray();
            project.RoleAssignments[userId] = role;
        }
    }

    public async Task RemoveTeamMemberAsync(Guid projectId, Guid userId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var memberList = project.TeamMemberIds.ToList();
            memberList.Remove(userId);
            project.TeamMemberIds = memberList.ToArray();
            project.RoleAssignments.Remove(userId);
        }
    }

    public async Task UpdateTeamMemberRoleAsync(Guid projectId, Guid userId, string role)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            project.RoleAssignments[userId] = role;
        }
    }

    public async Task<(Guid UserId, string Role)[]> GetTeamMembersAsync(Guid projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project))
            return Array.Empty<(Guid, string)>();

        return project.RoleAssignments
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToArray();
    }

    public async Task<ProjectWorkflow> CreateWorkflowAsync(ProjectWorkflow workflow)
    {
        workflow.Id = Guid.NewGuid();
        workflow.CreatedAt = DateTime.UtcNow;
        workflow.Status = WorkflowStatus.NotStarted;

        if (_projectWorkflows.TryGetValue(workflow.ProjectId, out var workflows))
        {
            workflows.Add(workflow);
        }

        return workflow;
    }

    public Task<ProjectWorkflow?> GetWorkflowAsync(Guid workflowId)
    {
        var workflow = _projectWorkflows.Values
            .SelectMany(wf => wf)
            .FirstOrDefault(w => w.Id == workflowId);

        return Task.FromResult(workflow);
    }

    public Task<ProjectWorkflow[]> GetProjectWorkflowsAsync(Guid projectId)
    {
        if (_projectWorkflows.TryGetValue(projectId, out var workflows))
            return Task.FromResult(workflows.ToArray());

        return Task.FromResult(Array.Empty<ProjectWorkflow>());
    }

    public async Task UpdateWorkflowAsync(ProjectWorkflow workflow)
    {
        if (_projectWorkflows.TryGetValue(workflow.ProjectId, out var workflows))
        {
            var index = workflows.FindIndex(w => w.Id == workflow.Id);
            if (index >= 0)
                workflows[index] = workflow;
        }
    }

    public async Task CompleteWorkflowStageAsync(Guid workflowId, string stageName)
    {
        var workflow = await GetWorkflowAsync(workflowId);
        if (workflow == null)
            return;

        var stage = workflow.Stages.FirstOrDefault(s => s.Name == stageName);
        if (stage != null)
        {
            stage.IsCompleted = true;
        }

        // Check if all stages are completed
        if (workflow.Stages.All(s => s.IsCompleted))
        {
            workflow.Status = WorkflowStatus.Completed;
            await RaiseProjectEventAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = workflow.ProjectId,
                Type = ProjectEventType.WorkflowCompleted,
                ResourceType = "Workflow",
                ResourceId = workflow.Id,
                OccurredAt = DateTime.UtcNow
            });
        }

        await UpdateWorkflowAsync(workflow);
    }

    public async Task UpdateProjectPhaseAsync(Guid projectId, ProjectPhase newPhase)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var oldPhase = project.CurrentPhase;
            project.CurrentPhase = newPhase;
            project.UpdatedAt = DateTime.UtcNow;

            await RaiseProjectEventAsync(new ProjectEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Type = ProjectEventType.PhaseTransition,
                Metadata = new Dictionary<string, object>
                {
                    { "OldPhase", oldPhase },
                    { "NewPhase", newPhase }
                },
                OccurredAt = DateTime.UtcNow
            });
        }
    }

    public Task<ProjectPhase> GetProjectPhaseAsync(Guid projectId)
    {
        if (_projects.TryGetValue(projectId, out var project))
            return Task.FromResult(project.CurrentPhase);

        return Task.FromResult(ProjectPhase.ProblemDefinition);
    }

    public async Task AssociateDatasetAsync(Guid projectId, Guid datasetId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var datasets = project.DatasetIds.ToList();
            if (!datasets.Contains(datasetId))
                datasets.Add(datasetId);

            project.DatasetIds = datasets.ToArray();
        }
    }

    public async Task AssociateModelAsync(Guid projectId, Guid modelId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var models = project.ModelIds.ToList();
            if (!models.Contains(modelId))
                models.Add(modelId);

            project.ModelIds = models.ToArray();
        }
    }

    public async Task AssociatePipelineAsync(Guid projectId, Guid pipelineId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            var pipelines = project.PipelineIds.ToList();
            if (!pipelines.Contains(pipelineId))
                pipelines.Add(pipelineId);

            project.PipelineIds = pipelines.ToArray();
        }
    }

    public async Task<ProjectContext> GetProjectContextAsync(Guid projectId, Guid userId)
    {
        if (!_projects.TryGetValue(projectId, out var project))
            throw new ArgumentException("Project not found");

        var userRoles = await _roleService.GetUserRolesAsync(userId);
        if (!userRoles.Any())
            throw new UnauthorizedAccessException("User has no roles assigned");

        var primaryRole = userRoles.First();

        var context = new ProjectContext
        {
            Project = project,
            UserRole = primaryRole,
            VisibleDatasets = project.DatasetIds,
            VisibleModels = project.ModelIds,
            VisiblePipelines = project.PipelineIds,
            AssignedWorkflows = GetWorkflowsForRole(projectId, primaryRole.Type),
            LoadedAt = DateTime.UtcNow
        };

        return context;
    }

    private ProjectWorkflow[] GetWorkflowsForRole(Guid projectId, RoleType roleType)
    {
        if (!_projectWorkflows.TryGetValue(projectId, out var allWorkflows))
            return Array.Empty<ProjectWorkflow>();

        var roleWorkflows = roleType switch
        {
            RoleType.DataAnalyst => allWorkflows
                .Where(w => w.AssignedRoles.Contains("DataAnalyst") || w.AssignedRoles.Length == 0)
                .ToArray(),
            RoleType.DataScientist => allWorkflows
                .Where(w => w.AssignedRoles.Contains("DataScientist") || w.AssignedRoles.Length == 0)
                .ToArray(),
            RoleType.DataEngineer => allWorkflows
                .Where(w => w.AssignedRoles.Contains("DataEngineer") || w.AssignedRoles.Length == 0)
                .ToArray(),
            RoleType.AIEngineer => allWorkflows
                .Where(w => w.AssignedRoles.Contains("AIEngineer") || w.AssignedRoles.Length == 0)
                .ToArray(),
            _ => allWorkflows.ToArray()
        };

        return roleWorkflows;
    }

    // ===== REAL-TIME COLLABORATION =====

    public async Task<TeamActivitySummary> GetProjectActivitySummaryAsync(Guid projectId)
    {
        return await _collaborationService.GetTeamActivitySummaryAsync(projectId.ToString());
    }

    public async Task<List<UserPresence>> GetProjectParticipantsAsync(Guid projectId)
    {
        return await _presenceService.GetOnlineUsersInProjectAsync(projectId.ToString());
    }

    public async Task LogProjectActivityAsync(Guid projectId, string userId, string activityType, string description)
    {
        // Determine user role from project team
        var teamMembers = await GetTeamMembersAsync(projectId);
        var userRole = teamMembers.FirstOrDefault(t => t.UserId.ToString() == userId).Role;

        await _collaborationService.LogActivityAsync(
            userId,
            userId, // userName would come from user service in production
            activityType,
            description,
            projectId.ToString(),
            "Project",
            projectId.ToString(),
            userRole ?? "Unknown"
        );
    }

    public async Task<List<CollaborativeComment>> GetResourceCommentsAsync(Guid projectId, string resourceId)
    {
        return await _collaborationService.GetCommentsForResourceAsync(resourceId);
    }

    public async Task<TeamActivitySession> GetTeamSessionAsync(Guid projectId)
    {
        var activities = await _collaborationService.GetProjectActivitiesAsync(projectId.ToString());
        var participants = await GetProjectParticipantsAsync(projectId);

        return new TeamActivitySession
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId.ToString(),
            MemberUserIds = participants.Select(p => p.UserId).ToList(),
            Activities = activities.Take(20).ToList(),
            StartedAt = DateTime.UtcNow.AddHours(-1),
            Topic = $"Project {projectId} Collaboration"
        };

    }

    private async Task RaiseProjectEventAsync(ProjectEvent projectEvent)
    {
        projectEvent.Id = Guid.NewGuid();
        projectEvent.OccurredAt = DateTime.UtcNow;
        _eventLog.Add(projectEvent);
    }
}
