namespace WebApp.Models;

public class MediaEntry
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public MediaType Type { get; set; }
    public MediaStatus Status { get; set; } = MediaStatus.PlanToStart;
    public int? Rating { get; set; }

    // Anime/Series progress.
    public int? Season { get; set; }
    public int? Episode { get; set; }

    // Manga progress.
    public int? Chapter { get; set; }
    public int? Volume { get; set; }

    // Movies: a single unit, so just watched-or-not rather than a running count.
    public bool Watched { get; set; }

    public ICollection<MediaLink> Links { get; set; } = new List<MediaLink>();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
