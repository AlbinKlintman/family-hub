namespace WebApp.Models;

/// <summary>One set of one exercise -- weight and reps vary set to set, so each is its own row.</summary>
public class WorkoutSet
{
    public int Id { get; set; }
    public int WorkoutExerciseId { get; set; }
    public WorkoutExercise? WorkoutExercise { get; set; }

    public int SetNumber { get; set; }

    /// <summary>
    /// The total weight actually lifted. For free weight/bodyweight this is the
    /// only weight value; for a Machine exercise it's BaseWeightKg + AddOnKg,
    /// kept in sync so charts/summaries never need to know about weight types.
    /// Null for a bodyweight set with no added weight.
    /// </summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Machine exercises only: which base weight was selected, kept separately so the UI can redisplay the exact selection.</summary>
    public decimal? BaseWeightKg { get; set; }
    /// <summary>Machine exercises only: which add-on was selected, if any.</summary>
    public decimal? AddOnKg { get; set; }

    public int Reps { get; set; }
}
