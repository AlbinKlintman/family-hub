namespace WebApp.Services;

public interface INotificationChannel
{
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}
