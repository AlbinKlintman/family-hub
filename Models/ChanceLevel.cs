namespace WebApp.Models;

public enum ChanceLevel
{
    Low,
    Medium,
    High
}

public static class ChanceLevelExtensions
{
    public static string ToBadgeCssClass(this ChanceLevel chance) => chance switch
    {
        ChanceLevel.High => "bg-success",
        ChanceLevel.Medium => "bg-warning text-dark",
        ChanceLevel.Low => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string ToDisplayName(this ChanceLevel chance) => chance switch
    {
        ChanceLevel.High => "High chance",
        ChanceLevel.Medium => "Medium chance",
        ChanceLevel.Low => "Low chance",
        _ => chance.ToString()
    };
}
