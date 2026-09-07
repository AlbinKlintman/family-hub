namespace WebApp.Models;

public class MediaEntry
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public MediaType Type { get; set; }
    public MediaStatus Status { get; set; } = MediaStatus.PlanToStart;
    public int? Rating { get; set; }
    public string? CoverImageUrl { get; set; }

    // Anime/Series progress.
    public int? Season { get; set; }
    public int? Episode { get; set; }

    // Manga/Book progress. Manga also uses Volume; books use Page instead.
    public int? Chapter { get; set; }
    public int? Volume { get; set; }
    public int? Page { get; set; }

    // Movies: a single unit, so just watched-or-not rather than a running count.
    public bool Watched { get; set; }

    public ICollection<MediaLink> Links { get; set; } = new List<MediaLink>();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
