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

    /// <summary>Shared with ToDoNote's Reminder1hSentAtUtc for the 24h/1h-before scheme.</summary>
    public DateTime? Reminder24hSentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
