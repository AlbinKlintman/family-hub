namespace WebApp.Models;

/// <summary>
/// Marks a Schedule as visible to another account. Unlike NoteShare, there's no
/// per-viewer overlay here -- sharing a schedule just makes every note tagged
/// with it (directly, or via a linked folder) visible to that person, computed
/// live rather than stamped onto each note.
/// </summary>
public class ScheduleShare
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public required string SharedWithUserId { get; set; }
}
