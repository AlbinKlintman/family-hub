using System.Text;
using System.Text.Json;

namespace WebApp.Services;

public class TelegramNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TelegramNotifier> logger) : INotificationChannel
{
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var botToken = configuration["Notifications:TelegramBotToken"];
        var chatId = configuration["Notifications:TelegramChatId"];
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            logger.LogWarning("Notifications:TelegramBotToken/TelegramChatId is not configured; skipping notification.");
            return;
        }

        var client = httpClientFactory.CreateClient();
        var payload = JsonSerializer.Serialize(new
        {
            chat_id = chatId,
            // Reminder messages are written once for Discord's **bold** syntax --
            // Telegram's legacy Markdown mode uses single asterisks instead.
            text = ToTelegramMarkdown(message),
            parse_mode = "Markdown"
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://api.telegram.org/bot{botToken}/sendMessage", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Telegram API returned {StatusCode}", response.StatusCode);
        }
    }

    internal static string ToTelegramMarkdown(string message) => message.Replace("**", "*");
}
