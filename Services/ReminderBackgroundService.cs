using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Polls for to-dos, job interviews, and laundry slots crossing the
/// 24-hour/1-hour-before mark and sends a Discord reminder once each.
/// Due date/time fields are naive local values (no timezone stored), so
/// this compares against the server's local clock -- fine as long as the
/// server and the household are in the same timezone.
/// </summary>
public class ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Window24h = TimeSpan.FromHours(24);
    private static readonly TimeSpan Window1h = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await CheckAndNotifyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reminder check failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAndNotifyAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<DiscordNotifier>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // nowLocal is compared against the naive local due date/time the user entered.
        // nowUtc is what actually gets written to the *SentAtUtc columns (timestamptz
        // requires a genuine UTC-kind DateTime -- Npgsql throws otherwise).
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;

        await CheckToDosAsync(db, notifier, userManager, nowLocal, nowUtc, ct);
        await CheckInterviewsAsync(db, notifier, userManager, nowLocal, nowUtc, ct);
        await CheckLaundryAsync(db, notifier, userManager, nowLocal, nowUtc, ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task CheckToDosAsync(ApplicationDbContext db, DiscordNotifier notifier, UserManager<IdentityUser> userManager, DateTime nowLocal, DateTime nowUtc, CancellationToken ct)
    {
        var pending = await db.Notes.OfType<ToDoNote>()
            .Where(t => t.DueDate != null && t.DueTime != null
                     && (t.Reminder24hSentAtUtc == null || t.Reminder1hSentAtUtc == null))
            .ToListAsync(ct);

        foreach (var todo in pending)
        {
            var due = todo.DueDate!.Value.ToDateTime(todo.DueTime!.Value);
            var title = string.IsNullOrWhiteSpace(todo.Title) ? "To-do" : todo.Title;
            var who = await GetUserLabelAsync(userManager, todo.UserId);

            await ProcessAsync(due, nowLocal, nowUtc,
                todo.Reminder24hSentAtUtc, v => todo.Reminder24hSentAtUtc = v,
                todo.Reminder1hSentAtUtc, v => todo.Reminder1hSentAtUtc = v,
                label => notifier.SendAsync($"⏰ **{who}** — \"{title}\" is due {label} (at {due:HH:mm} on {due:ddd, MMM d}).", ct));
        }
    }

    private async Task CheckInterviewsAsync(ApplicationDbContext db, DiscordNotifier notifier, UserManager<IdentityUser> userManager, DateTime nowLocal, DateTime nowUtc, CancellationToken ct)
    {
        var pending = await db.JobApplications
            .Where(a => a.InterviewDate != null && a.InterviewTime != null
                     && (a.InterviewReminder24hSentAtUtc == null || a.InterviewReminder1hSentAtUtc == null))
            .ToListAsync(ct);

        foreach (var application in pending)
        {
            var due = application.InterviewDate!.Value.ToDateTime(application.InterviewTime!.Value);
            var who = await GetUserLabelAsync(userManager, application.UserId);

            await ProcessAsync(due, nowLocal, nowUtc,
                application.InterviewReminder24hSentAtUtc, v => application.InterviewReminder24hSentAtUtc = v,
                application.InterviewReminder1hSentAtUtc, v => application.InterviewReminder1hSentAtUtc = v,
                label => notifier.SendAsync($"📅 **{who}** — interview for \"{application.RoleName}\" is {label} (at {due:HH:mm} on {due:ddd, MMM d}).", ct));
        }
    }

    private async Task CheckLaundryAsync(ApplicationDbContext db, DiscordNotifier notifier, UserManager<IdentityUser> userManager, DateTime nowLocal, DateTime nowUtc, CancellationToken ct)
    {
        var pending = await db.Notes.OfType<LaundryNote>()
            .Where(l => l.Day != null && l.Reminder24hSentAtUtc == null)
            .ToListAsync(ct);

        foreach (var laundry in pending)
        {
            var due = laundry.Day!.Value.ToDateTime(laundry.TimeWindow.ToStartTime());
            var who = await GetUserLabelAsync(userManager, laundry.UserId);

            if (due <= nowLocal)
            {
                laundry.Reminder24hSentAtUtc = nowUtc;
                continue;
            }

            if (due - nowLocal <= Window24h)
            {
                await notifier.SendAsync(
                    $"🧺 **{who}** — laundry ({laundry.LaundryType.ToDisplayName()} · {laundry.Room.ToDisplayName()}) is scheduled for tomorrow, {laundry.TimeWindow.ToDisplayName()} window.",
                    ct);
                laundry.Reminder24hSentAtUtc = nowUtc;
            }
        }
    }

    internal static async Task ProcessAsync(
        DateTime due, DateTime nowLocal, DateTime nowUtc,
        DateTime? sent24h, Action<DateTime> mark24h,
        DateTime? sent1h, Action<DateTime> mark1h,
        Func<string, Task> send)
    {
        if (due <= nowLocal)
        {
            // Already passed without being caught (e.g. the app was down) -- mark done, don't notify late.
            if (sent24h is null) mark24h(nowUtc);
            if (sent1h is null) mark1h(nowUtc);
            return;
        }

        var remaining = due - nowLocal;

        if (sent1h is null && remaining <= Window1h)
        {
            await send("in about 1 hour");
            mark1h(nowUtc);
            if (sent24h is null) mark24h(nowUtc);
            return;
        }

        if (sent24h is null && remaining <= Window24h)
        {
            await send("in about 24 hours");
            mark24h(nowUtc);
        }
    }

    private static async Task<string> GetUserLabelAsync(UserManager<IdentityUser> userManager, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.Email ?? user?.UserName ?? "Family Hub";
    }
}
