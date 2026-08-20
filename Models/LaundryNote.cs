namespace WebApp.Models;

public class LaundryNote : Note
{
    public string? Room { get; set; }
    public DateOnly? Day { get; set; }
    public TimeOnly? TimeWindowStart { get; set; }
    public TimeOnly? TimeWindowEnd { get; set; }
}
