namespace WebApp.Models;

/// <summary>
/// One person's private view of a note shared with them: their own folder,
/// their own schedule/calendar placement, and their own priority -- entirely
/// independent of the owner's (and any other viewer's) values for the same
/// note. The note's actual content (title, due date, type-specific fields)
/// and done/not-done state stay on the Note itself, shared by everyone.
/// </summary>
public class NoteShare
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public required string SharedWithUserId { get; set; }

    public int? FolderId { get; set; }
    public Folder? Folder { get; set; }
    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }
    public NotePriority? Priority { get; set; }
}
