namespace WebApp.Services;

public interface INotificationChannel
{
    /// <summary>userId is whoever the reminder is about (a note/application's owner) -- each channel looks up that person's own settings, not a single app-wide config.</summary>
    Task SendAsync(string userId, string message, CancellationToken cancellationToken = default);
}
