namespace WebApp.Models;

/// <summary>One training session: created first (date, time, session type), then filled in with exercises and sets.</summary>
public class Workout
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public TrainingSessionType SessionType { get; set; }

    public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
}
