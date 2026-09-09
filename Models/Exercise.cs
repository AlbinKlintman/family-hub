namespace WebApp.Models;

public class Exercise
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }

    /// <summary>Which kind of session this exercise belongs to, so logging a session only offers matching exercises.</summary>
    public TrainingSessionType SessionType { get; set; }
    public ExerciseWeightType WeightType { get; set; }

    /// <summary>How far forward the seat/pad should be set, e.g. leg press safety bar notch. Optional.</summary>
    public decimal? SeatForwardPosition { get; set; }
    /// <summary>How high the seat/pad should be set. Optional.</summary>
    public decimal? SeatHeightPosition { get; set; }

    /// <summary>Only meaningful when WeightType is Machine.</summary>
    public ICollection<ExerciseMachineWeight> MachineWeights { get; set; } = new List<ExerciseMachineWeight>();
    /// <summary>Only meaningful when WeightType is Machine.</summary>
    public ICollection<ExerciseMachineAddOn> MachineAddOns { get; set; } = new List<ExerciseMachineAddOn>();

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
