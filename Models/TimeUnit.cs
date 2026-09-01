namespace WebApp.Models;

public enum TimeUnit
{
    Minutes,
    Hours,
    Days
}

public static class TimeUnitExtensions
{
    public static TimeSpan ToTimeSpan(this TimeUnit unit, int value) => unit switch
    {
        TimeUnit.Minutes => TimeSpan.FromMinutes(value),
        TimeUnit.Hours => TimeSpan.FromHours(value),
        TimeUnit.Days => TimeSpan.FromDays(value),
        _ => TimeSpan.Zero
    };

    public static string ToDisplayName(this TimeUnit unit) => unit switch
    {
        TimeUnit.Minutes => "minutes",
        TimeUnit.Hours => "hours",
        TimeUnit.Days => "days",
        _ => unit.ToString()
    };
}
