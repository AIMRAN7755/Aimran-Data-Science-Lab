namespace AIMRAN_Data_Science_Lab.Services.Collaboration;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// In-memory thread-safe presence management service.
/// </summary>
public class PresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<string, UserPresence> _users = new();

    public Task<UserPresence> RegisterUserAsync(string userId, string userName, string email, string roleType, string connectionId)
    {
        var presence = _users.AddOrUpdate(
            userId,
            _ => new UserPresence
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                RoleType = roleType,
                ConnectionId = connectionId,
                IsOnline = true,
                ConnectedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.UserName = string.IsNullOrEmpty(userName) ? existing.UserName : userName;
                existing.Email = string.IsNullOrEmpty(email) ? existing.Email : email;
                existing.RoleType = string.IsNullOrEmpty(roleType) ? existing.RoleType : roleType;
                existing.ConnectionId = connectionId;
                existing.IsOnline = true;
                existing.LastActivityAt = DateTime.UtcNow;
                return existing;
            });

        return Task.FromResult(presence);
    }

    public Task UnregisterUserAsync(string userId)
    {
        if (_users.TryGetValue(userId, out var presence))
        {
            presence.IsOnline = false;
            presence.LastActivityAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task UnregisterConnectionAsync(string connectionId)
    {
        var user = _users.Values.FirstOrDefault(u => u.ConnectionId == connectionId);
        if (user != null)
        {
            user.IsOnline = false;
            user.LastActivityAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task UpdatePresenceAsync(string userId, string projectId, string? resourceId, string? resourceType, CursorPosition? cursor)
    {
        if (_users.TryGetValue(userId, out var presence))
        {
            presence.CurrentProjectId = projectId;
            presence.CurrentResourceId = resourceId;
            presence.CurrentResourceType = resourceType;
            if (cursor != null)
                presence.CursorPosition = cursor;
            presence.LastActivityAt = DateTime.UtcNow;
            presence.IsOnline = true;
        }
        return Task.CompletedTask;
    }

    public Task UpdateUserLocationAsync(string userId, string projectId, string resourceId, string resourceType)
    {
        return UpdatePresenceAsync(userId, projectId, resourceId, resourceType, null);
    }

    public Task UpdateCursorPositionAsync(string userId, CursorPosition cursorPosition)
    {
        if (_users.TryGetValue(userId, out var presence))
        {
            presence.CursorPosition = cursorPosition;
            presence.LastActivityAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task HeartbeatAsync(string userId)
    {
        if (_users.TryGetValue(userId, out var presence))
        {
            presence.LastActivityAt = DateTime.UtcNow;
            presence.IsOnline = true;
        }
        return Task.CompletedTask;
    }

    public Task<List<UserPresence>> GetOnlineUsersInProjectAsync(string projectId)
    {
        var list = _users.Values
            .Where(u => u.IsOnline && (string.IsNullOrEmpty(projectId) || u.CurrentProjectId == projectId))
            .ToList();
        return Task.FromResult(list);
    }

    public Task<UserPresence?> GetUserPresenceAsync(string userId)
    {
        _users.TryGetValue(userId, out var presence);
        return Task.FromResult(presence);
    }

    public Task<PresenceStatistics> GetPresenceStatisticsAsync(string projectId)
    {
        var activeUsers = _users.Values.Where(u => u.IsOnline).ToList();
        var editorsPerResource = new Dictionary<string, List<string>>();

        foreach (var user in activeUsers.Where(u => !string.IsNullOrEmpty(u.CurrentResourceId)))
        {
            if (!editorsPerResource.ContainsKey(user.CurrentResourceId!))
                editorsPerResource[user.CurrentResourceId!] = new List<string>();
            editorsPerResource[user.CurrentResourceId!].Add(user.UserId);
        }

        var stats = new PresenceStatistics
        {
            TotalOnlineUsers = activeUsers.Count,
            EditorsPerResource = editorsPerResource
        };

        return Task.FromResult(stats);
    }
}
