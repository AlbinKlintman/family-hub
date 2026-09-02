namespace WebApp.Models;

public class ApplicationLink
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public required string Url { get; set; }
}
