namespace WebApp.Models;

public class WeightEntry
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public decimal WeightKg { get; set; }
}
