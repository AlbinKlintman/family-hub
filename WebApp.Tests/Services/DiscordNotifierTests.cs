using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class DiscordNotifierTests
{
    private class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Should not build an HTTP client when no webhook is configured.");
    }

    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SendAsync_NoProfileForUser_DoesNotThrowOrCallHttp()
    {
        using var db = BuildContext();
        var notifier = new DiscordNotifier(new ThrowingHttpClientFactory(), db, NullLogger<DiscordNotifier>.Instance);

        await notifier.SendAsync("no-such-user", "hello");
    }

    [Fact]
    public async Task SendAsync_ProfileWithNoWebhookConfigured_DoesNotThrowOrCallHttp()
    {
        using var db = BuildContext();
        db.UserProfiles.Add(new UserProfile { UserId = "u1", Username = "albin" });
        await db.SaveChangesAsync();

        var notifier = new DiscordNotifier(new ThrowingHttpClientFactory(), db, NullLogger<DiscordNotifier>.Instance);

        await notifier.SendAsync("u1", "hello");
    }
}
