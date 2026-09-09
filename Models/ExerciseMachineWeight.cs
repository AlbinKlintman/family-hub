namespace WebApp.Models;

/// <summary>One selectable base weight on a Machine-type exercise's weight stack.</summary>
public class ExerciseMachineWeight
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public decimal WeightKg { get; set; }
}
