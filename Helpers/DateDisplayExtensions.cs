namespace WebApp.Helpers;

public static class DateDisplayExtensions
{
    public static string ToRelativeString(this DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}mo ago";
        return $"{(int)(span.TotalDays / 365)}y ago";
    }

    public static string ToRelativeString(this DateOnly date)
    {
        var days = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - date.DayNumber;
        return days switch
        {
            0 => "today",
            1 => "yesterday",
            < 30 => $"{days}d ago",
            < 365 => $"{days / 30}mo ago",
            _ => $"{days / 365}y ago"
        };
    }
}
