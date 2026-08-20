namespace WebApp.Models;

public enum TrainingSessionType
{
    Push,
    Pull,
    Legs
}

public static class TrainingSessionTypeExtensions
{
    public static string ToDisplayName(this TrainingSessionType type) => type switch
    {
        TrainingSessionType.Push => "Push",
        TrainingSessionType.Pull => "Pull",
        TrainingSessionType.Legs => "Legs",
        _ => type.ToString()
    };
}
