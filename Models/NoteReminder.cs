namespace WebApp.Models;

public class NoteReminder
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public int OffsetValue { get; set; }
    public TimeUnit OffsetUnit { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
