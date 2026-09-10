using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Services;

public class TelegramNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context, ILogger<TelegramNotifier> logger) : INotificationChannel
{
    public async Task SendAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        // The bot itself is one shared app-wide bot (configured once by whoever runs the
        // server) -- only the chat id is per-person, since nobody but the person themselves
        // can know it.
        var botToken = configuration["Notifications:TelegramBotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            logger.LogWarning("Notifications:TelegramBotToken is not configured; skipping notification.");
            return;
        }

        var chatId = await context.UserProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.TelegramChatId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(chatId))
        {
            // Not configuring Telegram is a normal, valid choice per person -- nothing to warn about.
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
