namespace AIMRAN_Data_Science_Lab.Services.Collaboration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Service contract for real-time presence management.
/// </summary>
public interface IPresenceService
{
    Task<UserPresence> RegisterUserAsync(string userId, string userName, string email, string roleType, string connectionId);
    Task UnregisterUserAsync(string userId);
    Task UnregisterConnectionAsync(string connectionId);
    Task UpdatePresenceAsync(string userId, string projectId, string? resourceId, string? resourceType, CursorPosition? cursor);
    Task UpdateUserLocationAsync(string userId, string projectId, string resourceId, string resourceType);
    Task UpdateCursorPositionAsync(string userId, CursorPosition cursorPosition);
    Task HeartbeatAsync(string userId);
    Task<List<UserPresence>> GetOnlineUsersInProjectAsync(string projectId);
    Task<UserPresence?> GetUserPresenceAsync(string userId);
    Task<PresenceStatistics> GetPresenceStatisticsAsync(string projectId);
}
