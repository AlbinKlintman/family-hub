namespace WebApp.Models;

public abstract class Note
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public string? Title { get; set; }
    public bool IsDone { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
