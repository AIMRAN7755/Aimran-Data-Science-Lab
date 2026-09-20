using System;
using System.Collections.Generic;

namespace AimranDataScienceLab.Core.Domain.RoleManagement;

/// <summary>
/// Represents a professional role in the data science ecosystem.
/// Defines capabilities, permissions, and default workflows.
/// </summary>
public class ProfessionalRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Data Analyst, Data Scientist, Data Engineer, AI Engineer
    public string Description { get; set; } = string.Empty;
    public RoleType Type { get; set; }
    public int Priority { get; set; } // 1=highest, determines workflow precedence

    public string[] PermissionScopes { get; set; } = Array.Empty<string>();
    public string[] AllowedWorkflows { get; set; } = Array.Empty<string>();
    public string[] AllowedTools { get; set; } = Array.Empty<string>();

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum RoleType
{
    DataAnalyst = 1,      // SQL, Dashboards, Reporting, Business Insights
    DataScientist = 2,    // ML, Feature Engineering, Experimentation, Model Versioning
    DataEngineer = 3,     // Pipelines, Data Integration, ETL, Storage
    AIEngineer = 4        // Deployment, APIs, Model Serving, Optimization
}

/// <summary>
/// Represents the permissions a role has for specific resources.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string Resource { get; set; } = string.Empty; // dataset, model, pipeline, etc.
    public PermissionAction[] AllowedActions { get; set; } = Array.Empty<PermissionAction>();
    public string? Condition { get; set; } // e.g., "own_datasets_only", "shared_projects"
    public DateTime CreatedAt { get; set; }
}

public enum PermissionAction
{
    Create,
    Read,
    Update,
    Delete,
    Share,
    Execute,
    Deploy,
    Monitor
}

/// <summary>
/// Represents a user assigned to one or more roles.
/// </summary>
public class DataScienceUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public Guid[] RoleIds { get; set; } = Array.Empty<Guid>(); // Multi-role support
    public Guid[] ProjectIds { get; set; } = Array.Empty<Guid>(); // Projects user is part of

    public Dictionary<string, object> Preferences { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
