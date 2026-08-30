using WebApp.Services;

namespace WebApp.Tests.Services;

public class ReminderProcessingTests
{
    [Fact]
    public async Task MarksSentWithUtcKindDateTime_NotLocalOrUnspecified()
    {
        // Regression test: writing a non-UTC-kind DateTime into a Postgres
        // "timestamp with time zone" column throws at runtime. The sent-at
        // timestamps must always be DateTime.UtcNow, never DateTime.Now,
        // even though the due-date comparison itself uses local time.
        var due = DateTime.Now.AddHours(2);
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        DateTime? captured = null;

        await ReminderBackgroundService.ProcessAsync(due, nowLocal, nowUtc,
            sent24h: null, mark24h: v => captured = v,
            sent1h: null, mark1h: _ => { },
            send: _ => Task.CompletedTask);

        Assert.NotNull(captured);
        Assert.Equal(DateTimeKind.Utc, captured!.Value.Kind);
    }

    [Fact]
    public async Task WithinOneHour_SendsAndMarksBothWindows()
    {
        var due = DateTime.Now.AddMinutes(30);
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        var sentMessages = new List<string>();
        DateTime? marked24h = null;
        DateTime? marked1h = null;

        await ReminderBackgroundService.ProcessAsync(due, nowLocal, nowUtc,
            sent24h: null, mark24h: v => marked24h = v,
            sent1h: null, mark1h: v => marked1h = v,
            send: msg => { sentMessages.Add(msg); return Task.CompletedTask; });

        Assert.Single(sentMessages);
        Assert.Contains("1 hour", sentMessages[0]);
        Assert.NotNull(marked24h);
        Assert.NotNull(marked1h);
    }

    [Fact]
    public async Task WithinTwentyFourHoursButNotOneHour_SendsOnlyTheDayBeforeReminder()
    {
        var due = DateTime.Now.AddHours(10);
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        var sentMessages = new List<string>();
        DateTime? marked24h = null;
        DateTime? marked1h = null;

        await ReminderBackgroundService.ProcessAsync(due, nowLocal, nowUtc,
            sent24h: null, mark24h: v => marked24h = v,
            sent1h: null, mark1h: v => marked1h = v,
            send: msg => { sentMessages.Add(msg); return Task.CompletedTask; });

        Assert.Single(sentMessages);
        Assert.Contains("24 hours", sentMessages[0]);
        Assert.NotNull(marked24h);
        Assert.Null(marked1h);
    }

    [Fact]
    public async Task AlreadySent_DoesNotSendAgain()
    {
        var due = DateTime.Now.AddMinutes(30);
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        var sentMessages = new List<string>();

        await ReminderBackgroundService.ProcessAsync(due, nowLocal, nowUtc,
            sent24h: nowUtc.AddDays(-1), mark24h: _ => { },
            sent1h: nowUtc.AddMinutes(-1), mark1h: _ => { },
            send: msg => { sentMessages.Add(msg); return Task.CompletedTask; });

        Assert.Empty(sentMessages);
    }

    [Fact]
    public async Task AlreadyPastDue_MarksSentWithoutSendingLateReminder()
    {
        var due = DateTime.Now.AddMinutes(-5);
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        var sentMessages = new List<string>();
        DateTime? marked24h = null;
        DateTime? marked1h = null;

        await ReminderBackgroundService.ProcessAsync(due, nowLocal, nowUtc,
            sent24h: null, mark24h: v => marked24h = v,
            sent1h: null, mark1h: v => marked1h = v,
            send: msg => { sentMessages.Add(msg); return Task.CompletedTask; });

        Assert.Empty(sentMessages);
        Assert.NotNull(marked24h);
        Assert.NotNull(marked1h);
    }
}
