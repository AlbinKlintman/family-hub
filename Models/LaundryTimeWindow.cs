namespace WebApp.Models;

public enum LaundryTimeWindow
{
    Morning,
    Midday,
    Afternoon,
    Evening
}

public static class LaundryTimeWindowExtensions
{
    public static string ToDisplayName(this LaundryTimeWindow window) => window switch
    {
        LaundryTimeWindow.Morning => "7:00 – 10:00",
        LaundryTimeWindow.Midday => "10:00 – 13:00",
        LaundryTimeWindow.Afternoon => "13:00 – 17:00",
        LaundryTimeWindow.Evening => "17:00 – 21:00",
        _ => window.ToString()
    };
}
