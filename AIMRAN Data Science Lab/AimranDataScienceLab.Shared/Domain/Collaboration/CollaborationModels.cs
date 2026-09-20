namespace AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Represents a user's presence in the system (online status, activity, location).
/// </summary>
public class UserPresence
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public bool IsOnline { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>Current project the user is viewing</summary>
    public string? CurrentProjectId { get; set; }

    /// <summary>Current resource being edited (model id, dataset id, etc.)</summary>
    public string? CurrentResourceId { get; set; }

    /// <summary>Type of resource: Model, Dataset, Pipeline, Report, etc.</summary>
    public string? CurrentResourceType { get; set; }

    /// <summary>User's current cursor/selection position in editor</summary>
    public CursorPosition? CursorPosition { get; set; }

    public string ConnectionId { get; set; } = string.Empty;
}

/// <summary>
/// Tracks cursor position for real-time display of other users' locations.
/// </summary>
public class CursorPosition
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int SelectionStart { get; set; }
    public int SelectionEnd { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Color assigned to this user's cursor</summary>
    public string CursorColor { get; set; } = "#FF6B6B";
}

/// <summary>
/// Represents an activity/action performed by a user in the system.
/// </summary>
public class ActivityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Type: ModelCreated, DatasetCleaned, PipelineExecuted, etc.</summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>User-friendly description of the activity</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>ID of the resource affected (model id, dataset id, etc.)</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>Type of resource: Model, Dataset, Pipeline, etc.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Project this activity belongs to</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Additional metadata about the activity</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Role of the user who performed this activity</summary>
    public string UserRole { get; set; } = string.Empty;

    /// <summary>Notification/mention data if this activity involves mentions</summary>
    public List<string> MentionedUserIds { get; set; } = new();
}

/// <summary>
/// Represents a collaborative comment on a resource (dataset, model, report, etc.).
/// </summary>
public class CollaborativeComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>Resource this comment is attached to</summary>
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Project this comment belongs to</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Line/section reference in the resource</summary>
    public int? LineNumber { get; set; }
    public int? ColumnStart { get; set; }
    public int? ColumnEnd { get; set; }

    /// <summary>Users mentioned in this comment (@username)</summary>
    public List<string> MentionedUserIds { get; set; } = new();

    /// <summary>Parent comment ID if this is a reply</summary>
    public string? ParentCommentId { get; set; }

    /// <summary>Replies to this comment</summary>
    public List<CollaborativeComment> Replies { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Whether this comment has been resolved/addressed</summary>
    public bool IsResolved { get; set; }

    /// <summary>Reactions: 👍, 👎, 🎉, etc.</summary>
    public Dictionary<string, List<string>> Reactions { get; set; } = new();
}

/// <summary>
/// Represents a concurrent edit to a resource.
/// </summary>
public class LiveEdit
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Resource being edited</summary>
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Project this edit belongs to</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Version/revision number of this edit</summary>
    public int Version { get; set; }

    /// <summary>Position where edit occurs</summary>
    public int Position { get; set; }

    /// <summary>Text inserted (null if delete)</summary>
    public string? InsertedText { get; set; }

    /// <summary>Number of characters deleted</summary>
    public int DeletedLength { get; set; }

    /// <summary>Original content before this edit</summary>
    public string? PreviousContent { get; set; }

    /// <summary>New content after this edit</summary>
    public string? NewContent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when edit was acknowledged by server</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Whether this edit has been applied/accepted</summary>
    public bool IsApplied { get; set; }
}

/// <summary>
/// Tracks and resolves conflicts when multiple users edit concurrently.
/// </summary>
public class ConflictRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResourceId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Two edits that conflict</summary>
    public LiveEdit Edit1 { get; set; } = new();
    public LiveEdit Edit2 { get; set; } = new();

    /// <summary>Resolution strategy used: LastWriteWins, Edit1Wins, Edit2Wins, Merged</summary>
    public string ResolutionStrategy { get; set; } = "LastWriteWins";

    /// <summary>Final resolved content</summary>
    public string ResolvedContent { get; set; } = string.Empty;

    /// <summary>Users notified of this conflict</summary>
    public List<string> NotifiedUserIds { get; set; } = new();

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Represents an annotation/note on a resource (similar to comments but simpler).
/// </summary>
public class Annotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string AnnotationType { get; set; } = "Note"; // Note, Question, Suggestion, Bug

    /// <summary>Resource this annotation is attached to</summary>
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Position in resource</summary>
    public int? LineNumber { get; set; }
    public int? CharacterOffset { get; set; }

    /// <summary>Color/highlight color</summary>
    public string HighlightColor { get; set; } = "#FFEB3B";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Represents a collaborative session (group editing context).
/// </summary>
public class CollaborativeSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Resource being collaborated on</summary>
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Users currently in this session</summary>
    public List<string> ParticipantUserIds { get; set; } = new();

    /// <summary>Session creator</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>Edit history for this session</summary>
    public List<LiveEdit> EditHistory { get; set; } = new();

    /// <summary>Conflicts detected in this session</summary>
    public List<ConflictRecord> Conflicts { get; set; } = new();

    /// <summary>Current version number</summary>
    public int CurrentVersion { get; set; }

    /// <summary>Current content state</summary>
    public string CurrentContent { get; set; } = string.Empty;

    /// <summary>Session started time</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Session ended time (null if still active)</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>Session status: Active, Paused, Completed</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Whether changes in this session require approval</summary>
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Tracks information about a specific team session/interaction.
/// </summary>
public class TeamActivitySession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Team members involved</summary>
    public List<string> MemberUserIds { get; set; } = new();

    /// <summary>Activities in this session</summary>
    public List<ActivityEvent> Activities { get; set; } = new();

    /// <summary>Comments in this session</summary>
    public List<CollaborativeComment> Comments { get; set; } = new();

    /// <summary>Session duration</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    /// <summary>Session topic/title</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Decisions made in this session</summary>
    public List<string> Decisions { get; set; } = new();
}

/// <summary>
/// Represents team activity summary for dashboard display.
/// </summary>
public class TeamActivitySummary
{
    public int TotalActivities { get; set; }
    public int ActiveCollaborators { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Total activities in last 24 hours</summary>
    public int ActivityCount24H { get; set; }

    /// <summary>Active users currently</summary>
    public int ActiveUsersNow { get; set; }

    /// <summary>Total users in last 7 days</summary>
    public int ActiveUsers7D { get; set; }

    /// <summary>Recent activities to display</summary>
    public List<ActivityEvent> RecentActivities { get; set; } = new();

    /// <summary>Top contributors</summary>
    public List<(string UserId, string UserName, int ActivityCount)> TopContributors { get; set; } = new();

    /// <summary>Comment count</summary>
    public int CommentCount { get; set; }

    /// <summary>Unresolved comments</summary>
    public int UnresolvedCommentCount { get; set; }

    /// <summary>Conflicts detected</summary>
    public int ConflictCount { get; set; }
}

/// <summary>
/// Notification for team collaboration (mentions, approvals, alerts).
/// </summary>
public class CollaborationNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>Type: Mention, ApprovalRequest, Alert, CommentReply, ResourceUpdate</summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>User who triggered this notification</summary>
    public string TriggeredByUserId { get; set; } = string.Empty;
    public string TriggeredByUserName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Related resource</summary>
    public string? RelatedResourceId { get; set; }
    public string? RelatedResourceType { get; set; }

    /// <summary>Project this notification is about</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Action URL to navigate to</summary>
    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    /// <summary>Additional metadata</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents approval workflow for collaborative changes.
/// </summary>
public class ApprovalRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Changes requiring approval</summary>
    public string ChangeDescription { get; set; } = string.Empty;
    public string ChangedResourceId { get; set; } = string.Empty;
    public string ChangedResourceType { get; set; } = string.Empty;

    /// <summary>User requesting approval</summary>
    public string RequestedByUserId { get; set; } = string.Empty;
    public string RequestedByUserName { get; set; } = string.Empty;

    /// <summary>Users who need to approve</summary>
    public List<string> ApproverUserIds { get; set; } = new();

    /// <summary>Approvals received</summary>
    public Dictionary<string, ApprovalResponse> ApprovalResponses { get; set; } = new();

    /// <summary>Status: Pending, Approved, Rejected, Expired</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Content before changes</summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>Proposed new content</summary>
    public string ProposedContent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Response to an approval request.
/// </summary>
public class ApprovalResponse
{
    public string ApproverId { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>Approved or Rejected</summary>
    public string Decision { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregated presence and editor statistics.
/// </summary>
public class PresenceStatistics
{
    public int TotalOnlineUsers { get; set; }
    public Dictionary<string, List<string>> EditorsPerResource { get; set; } = new();
}
