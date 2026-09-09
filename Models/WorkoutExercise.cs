namespace WebApp.Models;

/// <summary>One exercise performed within a Workout, holding its individual sets.</summary>
public class WorkoutExercise
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Order added within the workout, for display.</summary>
    public int SortOrder { get; set; }

    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();
}
