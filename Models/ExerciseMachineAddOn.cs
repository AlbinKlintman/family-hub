namespace WebApp.Models;

/// <summary>One optional add-on increment (e.g. +5, +10) stackable on top of a Machine-type exercise's selected base weight.</summary>
public class ExerciseMachineAddOn
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public decimal AddOnKg { get; set; }
}
