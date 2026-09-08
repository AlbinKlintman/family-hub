using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class NotificationDispatcherTests
{
    private class RecordingChannel : INotificationChannel
    {
        public List<string> Received { get; } = [];

        public Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            Received.Add(message);
            return Task.CompletedTask;
        }
    }

    private class ThrowingChannel : INotificationChannel
    {
        public Task SendAsync(string message, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated channel failure.");
    }

    [Fact]
    public async Task SendAsync_DeliversToEveryChannel()
    {
        var first = new RecordingChannel();
        var second = new RecordingChannel();
        var dispatcher = new NotificationDispatcher([first, second], NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.SendAsync("hello");

        Assert.Equal(["hello"], first.Received);
        Assert.Equal(["hello"], second.Received);
    }

    [Fact]
    public async Task SendAsync_OneChannelThrows_OthersStillReceiveTheMessage()
    {
        var working = new RecordingChannel();
        var dispatcher = new NotificationDispatcher([new ThrowingChannel(), working], NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.SendAsync("still delivered");

        Assert.Equal(["still delivered"], working.Received);
    }

    [Fact]
    public async Task SendAsync_NoChannelsConfigured_DoesNotThrow()
    {
        var dispatcher = new NotificationDispatcher([], NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.SendAsync("nowhere to go");
    }
}
