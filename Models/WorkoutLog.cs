namespace WebApp.Models;

public class WorkoutLog
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public required int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public TrainingSessionType SessionType { get; set; }
    public decimal WeightKg { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; } = 3;
    public DateOnly Date { get; set; }
}
