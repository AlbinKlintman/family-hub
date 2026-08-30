namespace WebApp.Models;

public enum NotePriority
{
    Low,
    Medium,
    High
}

public static class NotePriorityExtensions
{
    public static string ToBadgeCssClass(this NotePriority priority) => priority switch
    {
        NotePriority.High => "bg-danger",
        NotePriority.Medium => "bg-warning text-dark",
        NotePriority.Low => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string ToDisplayName(this NotePriority priority) => priority switch
    {
        NotePriority.High => "High priority",
        NotePriority.Medium => "Medium priority",
        NotePriority.Low => "Low priority",
        _ => priority.ToString()
    };
}
