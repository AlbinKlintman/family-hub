using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public static class NoteVisibilityProvider
{
    /// <summary>Every schedule currently shared with this viewer, by anyone.</summary>
    public static async Task<HashSet<int>> GetVisibleScheduleIdsAsync(ApplicationDbContext context, string userId)
    {
        var ids = await context.ScheduleShares
            .Where(s => s.SharedWithUserId == userId)
            .Select(s => s.ScheduleId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    /// <summary>
    /// A note is visible to userId if they own it, it's explicitly shared with
    /// them (NoteShare), or its schedule -- directly, or via a linked folder --
    /// is currently shared with them. The last case needs no explicit per-note
    /// share record, so a newly-added note picks up visibility automatically.
    /// </summary>
    public static Expression<Func<TNote, bool>> VisibleTo<TNote>(string userId, HashSet<int> sharedScheduleIds) where TNote : Note => n =>
        n.UserId == userId
        || n.Shares.Any(s => s.SharedWithUserId == userId)
        || (n.ScheduleId != null && sharedScheduleIds.Contains(n.ScheduleId.Value))
        || (n.Folder != null && n.Folder.ScheduleId != null && sharedScheduleIds.Contains(n.Folder.ScheduleId.Value));
}
