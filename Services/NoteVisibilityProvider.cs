using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Everything shared with one viewer that grants note visibility without a
/// per-note NoteShare row. FolderIds already includes every subfolder of a
/// shared folder, so callers can do a flat Contains check.
/// </summary>
public record SharedScope(HashSet<int> ScheduleIds, HashSet<int> FolderIds);

public static class NoteVisibilityProvider
{
    /// <summary>Every schedule and folder (plus its subfolders) currently shared with this viewer, by anyone.</summary>
    public static async Task<SharedScope> GetSharedScopeAsync(ApplicationDbContext context, string userId)
    {
        var scheduleIds = await context.ScheduleShares
            .Where(s => s.SharedWithUserId == userId)
            .Select(s => s.ScheduleId)
            .ToListAsync();

        var folderIds = await context.FolderShares
            .Where(s => s.SharedWithUserId == userId)
            .Select(s => s.FolderId)
            .ToListAsync();

        return new SharedScope(scheduleIds.ToHashSet(), await WithDescendantsAsync(context, folderIds));
    }

    /// <summary>The given folders plus every folder nested anywhere beneath them.</summary>
    public static async Task<HashSet<int>> WithDescendantsAsync(ApplicationDbContext context, IEnumerable<int> folderIds)
    {
        var result = folderIds.ToHashSet();
        var frontier = result.ToList();

        while (frontier.Count > 0)
        {
            var children = await context.Folders
                .Where(f => f.ParentFolderId != null && frontier.Contains(f.ParentFolderId.Value))
                .Select(f => f.Id)
                .ToListAsync();
            frontier = children.Where(result.Add).ToList();
        }

        return result;
    }

    /// <summary>
    /// A note is visible to userId if they own it, it's explicitly shared with
    /// them (NoteShare), its schedule -- directly, or via a linked folder -- is
    /// currently shared with them, or it's filed in a shared folder. The last
    /// two cases need no explicit per-note share record, so a newly-added note
    /// picks up visibility automatically.
    /// </summary>
    public static Expression<Func<TNote, bool>> VisibleTo<TNote>(string userId, SharedScope scope) where TNote : Note
    {
        var sharedScheduleIds = scope.ScheduleIds;
        var sharedFolderIds = scope.FolderIds;
        return n =>
            n.UserId == userId
            || n.Shares.Any(s => s.SharedWithUserId == userId)
            || (n.ScheduleId != null && sharedScheduleIds.Contains(n.ScheduleId.Value))
            || (n.Folder != null && n.Folder.ScheduleId != null && sharedScheduleIds.Contains(n.Folder.ScheduleId.Value))
            || (n.FolderId != null && sharedFolderIds.Contains(n.FolderId.Value));
    }
}
