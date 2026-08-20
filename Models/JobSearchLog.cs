namespace WebApp.Models;

public class JobSearchLog
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public DateOnly Date { get; set; }
    public int Count { get; set; }
}
