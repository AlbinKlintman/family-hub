namespace WebApp.Models;

/// <summary>
/// One person's private rating for a media entry shared with them -- everything
/// else (progress, status, links, cover) is shared, single truth, since usually
/// only one person needs to record what episode/chapter you're both on.
/// </summary>
public class MediaEntryShare
{
    public int Id { get; set; }
    public int MediaEntryId { get; set; }
    public required string SharedWithUserId { get; set; }
    public int? Rating { get; set; }
}
