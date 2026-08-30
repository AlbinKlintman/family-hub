using System.Text;
using System.Text.Json;

namespace WebApp.Services;

public class DiscordNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DiscordNotifier> logger)
{
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var webhookUrl = configuration["Notifications:DiscordWebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Notifications:DiscordWebhookUrl is not configured; skipping notification.");
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
