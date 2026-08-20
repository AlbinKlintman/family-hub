namespace WebApp.Models;

public class Exercise
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }

    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
}
