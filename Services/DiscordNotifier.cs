using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Services;

public class DiscordNotifier(IHttpClientFactory httpClientFactory, ApplicationDbContext context, ILogger<DiscordNotifier> logger) : INotificationChannel
{
    public async Task SendAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        var webhookUrl = await context.UserProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.DiscordWebhookUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            // Not configuring Discord is a normal, valid choice per person -- nothing to warn about.
            return;
        }

        var client = httpClientFactory.CreateClient();
        var payload = JsonSerializer.Serialize(new { content = message });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(webhookUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Discord webhook returned {StatusCode}", response.StatusCode);
        }
    }
}
