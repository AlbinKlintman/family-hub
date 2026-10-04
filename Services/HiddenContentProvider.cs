using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// What one viewer has chosen not to see. FolderIds/ScheduleIds hold every
/// hidden folder (plus its subfolders) and schedule across all owners: an
/// owner hiding their folder hides it for everyone it's shared with, and a
/// viewer's own overlay placement only ever points at their own folders and
/// schedules, so one flat set covers both cases. Nothing here changes data --
/// un-hiding brings everything straight back.
/// </summary>
public record HiddenScope(HashSet<int> FolderIds, HashSet<int> ScheduleIds, HashSet<NoteType> NoteTypes)
{
    /// <summary>Checks both the owner's placement (note) and the viewer's own overlay (viewerShare), so call this before substituting the overlay in.</summary>
    public bool Hides(Note note, NoteShare? viewerShare = null) =>
        NoteTypes.Contains(note.GetNoteType())
        || IsHiddenPlacement(note.FolderId, note.Folder, note.ScheduleId)
        || (viewerShare is not null && IsHiddenPlacement(viewerShare.FolderId, viewerShare.Folder, viewerShare.ScheduleId));

    public bool HidesFolder(int? folderId) => folderId is { } id && FolderIds.Contains(id);

    public bool HidesSchedule(int? scheduleId) => scheduleId is { } id && ScheduleIds.Contains(id);

    private bool IsHiddenPlacement(int? folderId, Folder? folder, int? scheduleId) =>
        HidesFolder(folderId) || HidesSchedule(scheduleId) || HidesSchedule(folder?.ScheduleId);
}

public static class HiddenContentProvider
{
    public static async Task<HiddenScope> GetHiddenScopeAsync(ApplicationDbContext context, string userId)
    {
        var hiddenFolderIds = await context.Folders
            .Where(f => f.IsHidden)
            .Select(f => f.Id)
            .ToListAsync();

        var hiddenScheduleIds = await context.Schedules
            .Where(s => s.IsHidden)
            .Select(s => s.Id)
            .ToListAsync();

        var hiddenTypes = await context.UserProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.HiddenNoteTypes)
            .FirstOrDefaultAsync() ?? [];

        return new HiddenScope(
            await NoteVisibilityProvider.WithDescendantsAsync(context, hiddenFolderIds),
            hiddenScheduleIds.ToHashSet(),
            hiddenTypes.ToHashSet());
    }

    /// <summary>Note types still available to pick or filter by, in their usual order.</summary>
    public static List<NoteType> VisibleNoteTypes(HiddenScope hidden) =>
        Enum.GetValues<NoteType>().Where(t => !hidden.NoteTypes.Contains(t)).ToList();
}
