using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public static class FriendConnectionProvider
{
    /// <summary>Accepted connections only -- keyed by the other person's UserId, not including the caller themselves.</summary>
    public static async Task<Dictionary<string, string>> GetAcceptedConnectionUsernamesAsync(ApplicationDbContext context, string userId)
    {
        var connections = await context.FriendConnections
            .Where(f => f.Status == FriendConnectionStatus.Accepted && (f.RequesterUserId == userId || f.RecipientUserId == userId))
            .ToListAsync();

        var friendIds = connections
            .Select(f => f.RequesterUserId == userId ? f.RecipientUserId : f.RequesterUserId)
            .ToList();

        return await context.UserProfiles
            .Where(p => friendIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.Username);
    }
}
