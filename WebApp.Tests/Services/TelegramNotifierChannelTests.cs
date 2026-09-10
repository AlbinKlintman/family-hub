using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests.Services;

/// <summary>Covers the SendAsync per-user routing; ToTelegramMarkdown itself is covered in TelegramNotifierTests.</summary>
public class TelegramNotifierChannelTests
{
    private class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Should not build an HTTP client when not fully configured.");
    }

    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration BuildConfiguration(string? botToken) => new ConfigurationBuilder()
        .AddInMemoryCollection(botToken is null
            ? []
            : new Dictionary<string, string?> { ["Notifications:TelegramBotToken"] = botToken })
        .Build();

    [Fact]
    public async Task SendAsync_NoBotTokenConfigured_DoesNotThrowOrCallHttp()
    {
        using var db = BuildContext();
        db.UserProfiles.Add(new UserProfile { UserId = "u1", Username = "albin", TelegramChatId = "12345" });
        await db.SaveChangesAsync();

        var notifier = new TelegramNotifier(new ThrowingHttpClientFactory(), BuildConfiguration(botToken: null), db, NullLogger<TelegramNotifier>.Instance);

        await notifier.SendAsync("u1", "hello");
    }

    [Fact]
    public async Task SendAsync_NoChatIdForUser_DoesNotThrowOrCallHttp()
    {
        using var db = BuildContext();
        db.UserProfiles.Add(new UserProfile { UserId = "u1", Username = "albin" });
        await db.SaveChangesAsync();

        var notifier = new TelegramNotifier(new ThrowingHttpClientFactory(), BuildConfiguration("bot-token"), db, NullLogger<TelegramNotifier>.Instance);

        await notifier.SendAsync("u1", "hello");
    }
}
