namespace WebApp.Models;

public class ApplicationDescription
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public required string Text { get; set; }
}
