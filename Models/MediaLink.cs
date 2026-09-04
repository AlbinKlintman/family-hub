namespace WebApp.Models;

public class MediaLink
{
    public int Id { get; set; }
    public int MediaEntryId { get; set; }
    public required string Url { get; set; }
}
