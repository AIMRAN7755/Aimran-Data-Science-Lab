namespace AIMRAN_Data_Science_Lab.Services.Collaboration;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// In-memory thread-safe collaboration management service.
/// </summary>
public class CollaborationService : ICollaborationService
{
    private readonly ConcurrentBag<ActivityEvent> _activities = new();
    private readonly ConcurrentDictionary<string, CollaborativeComment> _comments = new();
    private readonly ConcurrentBag<LiveEdit> _edits = new();
    private readonly ConcurrentDictionary<string, ApprovalRequest> _approvals = new();
    private readonly object _lock = new();

    public Task<ActivityEvent> LogActivityAsync(
        string userId, string userName, string activityType, string description, 
        string resourceId, string resourceType, string projectId, string userRole)
    {
        var activity = new ActivityEvent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            ActivityType = activityType,
            Description = description,
            ResourceId = resourceId,
            ResourceType = resourceType,
            ProjectId = projectId,
            UserRole = userRole,
            CreatedAt = DateTime.UtcNow
        };

        _activities.Add(activity);
        return Task.FromResult(activity);
    }

    public Task<CollaborativeComment> AddCommentAsync(
        string userId, string userName, string userEmail, string content, 
        string resourceId, string resourceType, string projectId, 
        List<string>? mentionedUserIds = null, int? lineNumber = null, int? col = null, string? selText = null, string userRole = "User")
    {
        var comment = new CollaborativeComment
        {
            Id = Guid.NewGuid().ToString(),
            AuthorId = userId,
            AuthorName = userName,
            AuthorEmail = userEmail,
            Content = content,
            ResourceId = resourceId,
            ResourceType = resourceType,
            ProjectId = projectId,
            LineNumber = lineNumber,
            ColumnStart = col,
            MentionedUserIds = mentionedUserIds ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _comments[comment.Id] = comment;
        return Task.FromResult(comment);
    }

    public Task<CollaborativeComment> ReplyToCommentAsync(
        string userId, string userName, string userEmail, string content, 
        string parentCommentId, string projectId, List<string>? mentionedUserIds)
    {
        var reply = new CollaborativeComment
        {
            Id = Guid.NewGuid().ToString(),
            AuthorId = userId,
            AuthorName = userName,
            AuthorEmail = userEmail,
            Content = content,
            ParentCommentId = parentCommentId,
            ProjectId = projectId,
            MentionedUserIds = mentionedUserIds ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        if (_comments.TryGetValue(parentCommentId, out var parent))
        {
            lock (_lock)
            {
                parent.Replies.Add(reply);
            }
        }

        _comments[reply.Id] = reply;
        return Task.FromResult(reply);
    }

    public Task<CollaborativeComment> ResolveCommentAsync(string commentId)
    {
        if (_comments.TryGetValue(commentId, out var comment))
        {
            comment.IsResolved = true;
            comment.ResolvedAt = DateTime.UtcNow;
            return Task.FromResult(comment);
        }

        var resolved = new CollaborativeComment { Id = commentId, IsResolved = true, ResolvedAt = DateTime.UtcNow };
        return Task.FromResult(resolved);
    }

    public Task AddCommentReactionAsync(string commentId, string userId, string reaction)
    {
        if (_comments.TryGetValue(commentId, out var comment))
        {
            lock (_lock)
            {
                if (!comment.Reactions.ContainsKey(reaction))
                    comment.Reactions[reaction] = new List<string>();

                if (!comment.Reactions[reaction].Contains(userId))
                    comment.Reactions[reaction].Add(userId);
            }
        }
        return Task.CompletedTask;
    }

    public Task<List<CollaborativeComment>> GetCommentsForResourceAsync(string resourceId)
    {
        var comments = _comments.Values
            .Where(c => c.ResourceId == resourceId || c.ProjectId == resourceId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(comments);
    }

    public Task<List<ActivityEvent>> GetProjectActivitiesAsync(string projectId, int take = 50)
    {
        var activities = _activities
            .Where(a => string.IsNullOrEmpty(projectId) || a.ProjectId == projectId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToList();
        return Task.FromResult(activities);
    }

    public Task<TeamActivitySummary> GetTeamActivitySummaryAsync(string projectId)
    {
        var projectActivities = _activities.Where(a => a.ProjectId == projectId).ToList();
        var uniqueUsers = projectActivities.Select(a => a.UserId).Distinct().Count();

        var summary = new TeamActivitySummary
        {
            ProjectId = projectId,
            TotalActivities = projectActivities.Count,
            ActiveCollaborators = uniqueUsers,
            RecentActivities = projectActivities.OrderByDescending(a => a.CreatedAt).Take(10).ToList()
        };

        return Task.FromResult(summary);
    }

    public Task<LiveEdit> SubmitEditAsync(
        string userId, string userName, string resourceId, string resourceType, string projectId, 
        int position, string? insertedText, int deletedLength, string previousContent, string newContent)
    {
        var edit = new LiveEdit
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            ResourceId = resourceId,
            ResourceType = resourceType,
            ProjectId = projectId,
            Position = position,
            InsertedText = insertedText,
            DeletedLength = deletedLength,
            PreviousContent = previousContent,
            NewContent = newContent,
            Version = _edits.Count(e => e.ResourceId == resourceId) + 1
        };

        _edits.Add(edit);
        return Task.FromResult(edit);
    }

    public Task<ApprovalRequest> CreateApprovalRequestAsync(
        string projectId, string changeDescription, string changedResourceId, string changedResourceType, 
        string requestedByUserId, string requestedByUserName, List<string> approverUserIds, 
        string originalContent, string proposedContent)
    {
        var approval = new ApprovalRequest
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            ChangeDescription = changeDescription,
            ChangedResourceId = changedResourceId,
            ChangedResourceType = changedResourceType,
            RequestedByUserId = requestedByUserId,
            RequestedByUserName = requestedByUserName,
            ApproverUserIds = approverUserIds ?? new List<string>(),
            OriginalContent = originalContent,
            ProposedContent = proposedContent,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _approvals[approval.Id] = approval;
        return Task.FromResult(approval);
    }

    public Task<ApprovalRequest> SubmitApprovalResponseAsync(
        string approvalRequestId, string approverId, string approverName, string decision, string? comments)
    {
        if (_approvals.TryGetValue(approvalRequestId, out var approval))
        {
            lock (_lock)
            {
                approval.ApprovalResponses[approverId] = new ApprovalResponse
                {
                    ApproverId = approverId,
                    ApproverName = approverName,
                    Decision = decision,
                    Comments = comments,
                    RespondedAt = DateTime.UtcNow
                };

                if (approval.ApprovalResponses.Count >= approval.ApproverUserIds.Count)
                {
                    approval.Status = approval.ApprovalResponses.Values.All(r => r.Decision == "Approved")
                        ? "Approved"
                        : "Rejected";
                    approval.ResolvedAt = DateTime.UtcNow;
                }
            }
            return Task.FromResult(approval);
        }

        return Task.FromResult(new ApprovalRequest { Id = approvalRequestId, Status = decision });
    }
}
