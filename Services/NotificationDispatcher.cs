namespace WebApp.Services;

/// <summary>
/// Fans a single reminder message out to every configured channel (Discord,
/// Telegram, ...), for the person it's actually about. One channel failing --
/// a bad token, the API being down -- never stops the others from sending,
/// and never surfaces as an exception to the caller (ReminderBackgroundService
/// already treats a failed send as non-fatal; each channel implementation
/// logs its own warnings).
/// </summary>
public class NotificationDispatcher(IEnumerable<INotificationChannel> channels, ILogger<NotificationDispatcher> logger)
{
    public async Task SendAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(userId, message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notification channel {Channel} failed to send.", channel.GetType().Name);
            }
        }
    }
}
