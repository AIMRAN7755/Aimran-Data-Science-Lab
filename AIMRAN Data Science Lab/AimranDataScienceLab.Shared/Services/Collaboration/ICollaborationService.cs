namespace AIMRAN_Data_Science_Lab.Services.Collaboration;

using System.Collections.Generic;
using System.Threading.Tasks;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Service contract for real-time team collaboration operations.
/// </summary>
public interface ICollaborationService
{
    Task<ActivityEvent> LogActivityAsync(string userId, string userName, string activityType, string description, string resourceId, string resourceType, string projectId, string userRole);
    Task<CollaborativeComment> AddCommentAsync(string userId, string userName, string userEmail, string content, string resourceId, string resourceType, string projectId, List<string>? mentionedUserIds = null, int? lineNumber = null, int? col = null, string? selText = null, string userRole = "User");
    Task<CollaborativeComment> ReplyToCommentAsync(string userId, string userName, string userEmail, string content, string parentCommentId, string projectId, List<string>? mentionedUserIds);
    Task<CollaborativeComment> ResolveCommentAsync(string commentId);
    Task AddCommentReactionAsync(string commentId, string userId, string reaction);
    Task<List<CollaborativeComment>> GetCommentsForResourceAsync(string resourceId);
    Task<List<ActivityEvent>> GetProjectActivitiesAsync(string projectId, int take = 50);
    Task<TeamActivitySummary> GetTeamActivitySummaryAsync(string projectId);
    Task<LiveEdit> SubmitEditAsync(string userId, string userName, string resourceId, string resourceType, string projectId, int position, string? insertedText, int deletedLength, string previousContent, string newContent);
    Task<ApprovalRequest> CreateApprovalRequestAsync(string projectId, string changeDescription, string changedResourceId, string changedResourceType, string requestedByUserId, string requestedByUserName, List<string> approverUserIds, string originalContent, string proposedContent);
    Task<ApprovalRequest> SubmitApprovalResponseAsync(string approvalRequestId, string approverId, string approverName, string decision, string? comments);
}
