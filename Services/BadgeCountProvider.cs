using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// The "things needing attention right now" counts shown as notification
/// badges (navbar, dashboard tiles, PWA app icon). Deliberately narrower than
/// the dashboard's "due soon" list -- only past-due/due-today items count,
/// since a badge that never clears back to zero stops meaning anything.
/// </summary>
public readonly record struct BadgeCounts(int Notes, int Applications)
{
    public int Total => Notes + Applications;
}

public static class BadgeCountProvider
{
    public static async Task<BadgeCounts> GetCountsAsync(ApplicationDbContext context, string userId, DateOnly today)
    {
        var openNotes = await context.Notes
            .Where(n => n.UserId == userId && !n.IsDone)
            .ToListAsync();
        var notesDue = openNotes.Count(n => IsNoteDue(n, today));

        var applications = await context.JobApplications
            .Where(a => a.UserId == userId)
            .ToListAsync();
        var applicationsDue = applications.Count(a => IsApplicationDue(a, today));

        return new BadgeCounts(notesDue, applicationsDue);
    }

    internal static bool IsNoteDue(Note note, DateOnly today)
    {
        var date = Pages.Notes.IndexModel.SortDate(note);
        return date != DateTime.MaxValue && DateOnly.FromDateTime(date) <= today;
    }

    /// <summary>
    /// A test or interview counts once its date has come and gone but the
    /// card hasn't been moved past that stage yet -- the only two "due
    /// dates" a job application carries.
    /// </summary>
    internal static bool IsApplicationDue(JobApplication application, DateOnly today) => application switch
    {
        { Status: ApplicationStatus.TestScheduled, TestDate: { } testDate } => testDate <= today,
        { Status: ApplicationStatus.InterviewScheduled, InterviewDate: { } interviewDate } => interviewDate <= today,
        _ => false
    };
}
