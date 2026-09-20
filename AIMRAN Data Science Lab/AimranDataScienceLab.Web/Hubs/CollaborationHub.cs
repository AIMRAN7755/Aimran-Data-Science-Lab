namespace AIMRAN_Data_Science_Lab.Web.Hubs;

using Microsoft.AspNetCore.SignalR;
using AIMRAN_Data_Science_Lab.Services.Collaboration;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// SignalR hub for real-time collaboration in the web host.
/// Handles presence, activity, comments, edits, and notifications.
/// </summary>
public class CollaborationHub : Hub
{
    private readonly IPresenceService _presenceService;
    private readonly ICollaborationService _collaborationService;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(
        IPresenceService presenceService,
        ICollaborationService collaborationService,
        ILogger<CollaborationHub> logger)
    {
        _presenceService = presenceService;
        _collaborationService = collaborationService;
        _logger = logger;
    }

    /// <summary>
    /// Client calls this when connecting to register their presence.
    /// </summary>
    public async Task RegisterPresenceAsync(string userId, string userName, string email, string roleType, string avatarUrl)
    {
        try
        {
            var presence = await _presenceService.RegisterUserAsync(userId, userName, email, roleType, Context.ConnectionId);
            presence.AvatarUrl = avatarUrl;

            // Join a group for notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            _logger.LogInformation($"User {userName} registered with connection {Context.ConnectionId}");

            // Broadcast presence update to all connected clients
            await Clients.All.SendAsync("UserOnline", new
            {
                userId,
                userName,
                roleType,
                avatarUrl,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering presence");
            await Clients.Caller.SendAsync("Error", "Failed to register presence");
        }
    }

    /// <summary>
    /// Client calls this to update their current activity/location.
    /// </summary>
    public async Task UpdatePresenceAsync(string projectId, string? resourceId, string? resourceType)
    {
        try
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

            await _presenceService.UpdatePresenceAsync(userId, projectId, resourceId, resourceType, null);

            // Notify project group about presence update
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");

            await Clients.Group($"project-{projectId}").SendAsync("PresenceUpdated", new
            {
                userId,
                userName,
                projectId,
                resourceId,
                resourceType,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation($"User {userName} updated presence in project {projectId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating presence");
            await Clients.Caller.SendAsync("Error", "Failed to update presence");
        }
    }

    /// <summary>
    /// Client sends cursor position update to be broadcast to other editors.
    /// </summary>
    public async Task UpdateCursorPositionAsync(string resourceId, int line, int column, int selectionStart, int selectionEnd)
    {
        try
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

            var cursorPosition = new CursorPosition
            {
                Line = line,
                Column = column,
                SelectionStart = selectionStart,
                SelectionEnd = selectionEnd
            };

            await _presenceService.UpdateCursorPositionAsync(userId, cursorPosition);

            // Broadcast cursor update to all others editing this resource
            await Clients.Group($"edit-{resourceId}").SendAsync("CursorMoved", new
            {
                userId,
                userName,
                line,
                column,
                selectionStart,
                selectionEnd,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cursor");
            await Clients.Caller.SendAsync("Error", "Failed to update cursor");
        }
    }

    /// <summary>
    /// Client starts editing a resource, joins edit group.
    /// </summary>
    public async Task StartEditingResourceAsync(string resourceId)
    {
        try
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

            await Groups.AddToGroupAsync(Context.ConnectionId, $"edit-{resourceId}");

            // Notify others that this user started editing
            await Clients.Group($"edit-{resourceId}").SendAsync("UserStartedEditing", new
            {
                userId,
                userName,
                resourceId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation($"User {userName} started editing resource {resourceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting edit");
            await Clients.Caller.SendAsync("Error", "Failed to start editing");
        }
    }

    /// <summary>
    /// Client stops editing a resource, leaves edit group.
    /// </summary>
    public async Task StopEditingResourceAsync(string resourceId)
    {
        try
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"edit-{resourceId}");

            // Notify others that this user stopped editing
            await Clients.Group($"edit-{resourceId}").SendAsync("UserStoppedEditing", new
            {
                userId,
                userName,
                resourceId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation($"User {userName} stopped editing resource {resourceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping edit");
            await Clients.Caller.SendAsync("Error", "Failed to stop editing");
        }
    }

    /// <summary>
    /// Client submits an edit for live collaboration.
    /// </summary>
    public async Task SubmitEditAsync(string resourceId, string resourceType, string projectId, 
        int position, string? insertedText, int deletedLength, string previousContent, string newContent)
    {
        try
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

            var edit = await _collaborationService.SubmitEditAsync(
                userId, userName, resourceId, resourceType, projectId,
                position, insertedText, deletedLength, previousContent, newContent);

            // Broadcast edit to all other editors
            await Clients.Group($"edit-{resourceId}").SendAsync("EditReceived", new
            {
                editId = edit.Id,
                userId,
                userName,
                position,
                insertedText,
                deletedLength,
                version = edit.Version,
                timestamp = edit.CreatedAt
            });

            _logger.LogInformation($"User {userName} submitted edit to {resourceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting edit");
            await Clients.Caller.SendAsync("Error", "Failed to submit edit");
        }
    }

    /// <summary>
    /// Client adds a comment to a resource.
    /// </summary>
    public async Task AddCommentAsync(string userId, string userName, string userEmail,
        string content, string resourceId, string resourceType, string projectId, 
        List<string>? mentionedUserIds, int? lineNumber)
    {
        try
        {
            var comment = await _collaborationService.AddCommentAsync(
                userId, userName, userEmail, content, resourceId, resourceType, projectId,
                mentionedUserIds, lineNumber);

            // Broadcast comment to resource group
            await Clients.Group($"resource-{resourceId}").SendAsync("CommentAdded", new
            {
                commentId = comment.Id,
                userId,
                userName,
                content,
                lineNumber,
                timestamp = comment.CreatedAt
            });

            // Send mention notifications
            if (mentionedUserIds?.Any() == true)
            {
                foreach (var mentionedUserId in mentionedUserIds)
                {
                    await Clients.Group($"user-{mentionedUserId}").SendAsync("MentionNotification", new
                    {
                        mentionedBy = userName,
                        message = $"{userName} mentioned you in a comment",
                        resourceId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation($"Comment added by {userName} to {resourceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            await Clients.Caller.SendAsync("Error", "Failed to add comment");
        }
    }

    /// <summary>
    /// Client replies to a comment.
    /// </summary>
    public async Task ReplyToCommentAsync(string userId, string userName, string userEmail,
        string content, string parentCommentId, string projectId, List<string>? mentionedUserIds)
    {
        try
        {
            var reply = await _collaborationService.ReplyToCommentAsync(
                userId, userName, userEmail, content, parentCommentId, projectId, mentionedUserIds);

            // Broadcast reply
            await Clients.All.SendAsync("CommentReplyAdded", new
            {
                replyId = reply.Id,
                parentCommentId,
                userId,
                userName,
                content,
                timestamp = reply.CreatedAt
            });

            _logger.LogInformation($"Reply added by {userName} to comment {parentCommentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to comment");
            await Clients.Caller.SendAsync("Error", "Failed to reply to comment");
        }
    }

    /// <summary>
    /// Client resolves a comment.
    /// </summary>
    public async Task ResolveCommentAsync(string commentId)
    {
        try
        {
            var comment = await _collaborationService.ResolveCommentAsync(commentId);

            // Broadcast resolution
            await Clients.All.SendAsync("CommentResolved", new
            {
                commentId,
                timestamp = comment.ResolvedAt
            });

            _logger.LogInformation($"Comment {commentId} resolved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving comment");
            await Clients.Caller.SendAsync("Error", "Failed to resolve comment");
        }
    }

    /// <summary>
    /// Client adds reaction to a comment.
    /// </summary>
    public async Task AddCommentReactionAsync(string commentId, string userId, string reaction)
    {
        try
        {
            await _collaborationService.AddCommentReactionAsync(commentId, userId, reaction);

            // Broadcast reaction
            await Clients.All.SendAsync("ReactionAdded", new
            {
                commentId,
                userId,
                reaction,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction");
            await Clients.Caller.SendAsync("Error", "Failed to add reaction");
        }
    }

    /// <summary>
    /// Client requests approval for a change.
    /// </summary>
    public async Task RequestApprovalAsync(string projectId, string changeDescription, 
        string changedResourceId, string changedResourceType, string requestedByUserId, 
        string requestedByUserName, List<string> approverUserIds, 
        string originalContent, string proposedContent)
    {
        try
        {
            var approval = await _collaborationService.CreateApprovalRequestAsync(
                projectId, changeDescription, changedResourceId, changedResourceType,
                requestedByUserId, requestedByUserName, approverUserIds,
                originalContent, proposedContent);

            // Notify approvers
            foreach (var approverId in approverUserIds)
            {
                await Clients.Group($"user-{approverId}").SendAsync("ApprovalRequested", new
                {
                    approvalId = approval.Id,
                    requestedBy = requestedByUserName,
                    changeDescription,
                    timestamp = approval.CreatedAt
                });
            }

            _logger.LogInformation($"Approval requested by {requestedByUserName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting approval");
            await Clients.Caller.SendAsync("Error", "Failed to request approval");
        }
    }

    /// <summary>
    /// Client submits approval response.
    /// </summary>
    public async Task SubmitApprovalAsync(string approvalRequestId, string approverId, 
        string approverName, string decision, string? comments)
    {
        try
        {
            var approval = await _collaborationService.SubmitApprovalResponseAsync(
                approvalRequestId, approverId, approverName, decision, comments);

            // Broadcast approval response
            await Clients.All.SendAsync("ApprovalResponseSubmitted", new
            {
                approvalId = approvalRequestId,
                approverId,
                approverName,
                decision,
                timestamp = DateTime.UtcNow
            });

            // If all approvals received, notify requestor
            if (approval.ApprovalResponses.Count == approval.ApproverUserIds.Count)
            {
                var allApproved = approval.ApprovalResponses.Values.All(r => r.Decision == "Approved");
                var message = allApproved ? "Your changes have been approved!" : "Your approval request was rejected.";

                await Clients.Group($"user-{approval.RequestedByUserId}").SendAsync("ApprovalFinalized", new
                {
                    approvalId = approvalRequestId,
                    approved = allApproved,
                    message,
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation($"Approval {approvalRequestId} response submitted by {approverName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting approval");
            await Clients.Caller.SendAsync("Error", "Failed to submit approval");
        }
    }

    /// <summary>
    /// Client logs an activity event.
    /// </summary>
    public async Task LogActivityAsync(string userId, string userName, string activityType, 
        string description, string resourceId, string resourceType, string projectId, string userRole)
    {
        try
        {
            var activity = await _collaborationService.LogActivityAsync(
                userId, userName, activityType, description, resourceId, resourceType, projectId, userRole);

            // Broadcast activity to project group
            await Clients.Group($"project-{projectId}").SendAsync("ActivityLogged", new
            {
                activityId = activity.Id,
                userName,
                activityType,
                description,
                timestamp = activity.CreatedAt
            });

            _logger.LogInformation($"Activity logged: {activityType} by {userName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity");
            await Clients.Caller.SendAsync("Error", "Failed to log activity");
        }
    }

    /// <summary>
    /// Handle client disconnect.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        var userName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

        await _presenceService.UnregisterUserAsync(userId);

        // Broadcast offline notification
        await Clients.All.SendAsync("UserOffline", new
        {
            userId,
            userName,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation($"User {userName} disconnected");

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle client reconnect after temporary disconnect.
    /// </summary>
    public async Task ReconnectAsync(string userId, string userName)
    {
        try
        {
            var presence = await _presenceService.GetUserPresenceAsync(userId);

            if (presence != null)
            {
                // Restore connection ID
                presence.ConnectionId = Context.ConnectionId;
                presence.IsOnline = true;

                // Restore user to their groups
                if (!string.IsNullOrEmpty(presence.CurrentProjectId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{presence.CurrentProjectId}");
                }
                if (!string.IsNullOrEmpty(presence.CurrentResourceId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"edit-{presence.CurrentResourceId}");
                }

                // Notify others of reconnection
                await Clients.All.SendAsync("UserReconnected", new
                {
                    userId,
                    userName,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"User {userName} reconnected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reconnect");
            await Clients.Caller.SendAsync("Error", "Failed to reconnect");
        }
    }

    /// <summary>
    /// Get presence for a specific project.
    /// </summary>
    public async Task GetProjectPresenceAsync(string projectId)
    {
        try
        {
            var onlineUsers = await _presenceService.GetOnlineUsersInProjectAsync(projectId);

            var presenceData = onlineUsers.Select(u => new
            {
                u.UserId,
                u.UserName,
                u.RoleType,
                u.AvatarUrl,
                u.CurrentResourceType,
                u.CurrentResourceId,
                u.LastActivityAt
            }).ToList();

            await Clients.Caller.SendAsync("ProjectPresenceSnapshot", presenceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project presence");
            await Clients.Caller.SendAsync("Error", "Failed to get project presence");
        }
    }
}
