namespace WebApp.Models;

public abstract class Note
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public string? Title { get; set; }
    public bool IsDone { get; set; }

    public int? FolderId { get; set; }
    public Folder? Folder { get; set; }
    public NotePriority? Priority { get; set; }

    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    /// <summary>Shared with ToDoNote's Reminder1hSentAtUtc for the 24h/1h-before scheme.</summary>
    public DateTime? Reminder24hSentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Who actually marked this done -- the owner or whoever it's shared with. Cleared when reopened.</summary>
    public string? DoneByUserId { get; set; }
    public DateTime? DoneAtUtc { get; set; }

    /// <summary>
    /// Per-viewer overlays -- each person this note is shared with gets their own
    /// Folder/Schedule/Priority here, independent of the owner's (and each other's).
    /// </summary>
    public ICollection<NoteShare> Shares { get; set; } = new List<NoteShare>();
}
