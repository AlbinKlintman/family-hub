namespace WebApp.Models;

public enum ExerciseWeightType
{
    /// <summary>No required weight -- extra weight (a vest, a belt) is optional.</summary>
    Bodyweight,
    /// <summary>Type in whatever weight you're lifting each time, e.g. squat or bench.</summary>
    FreeWeight,
    /// <summary>Picked from the exercise's own fixed weight stack, optionally plus an add-on.</summary>
    Machine
}

public static class ExerciseWeightTypeExtensions
{
    public static string ToDisplayName(this ExerciseWeightType type) => type switch
    {
        ExerciseWeightType.Bodyweight => "Bodyweight",
        ExerciseWeightType.FreeWeight => "Free weight",
        ExerciseWeightType.Machine => "Machine",
        _ => type.ToString()
    };
}
