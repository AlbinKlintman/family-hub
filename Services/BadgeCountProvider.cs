using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// The "things needing attention right now" count shown as a notification
/// badge (PWA app icon + in-page navbar). Deliberately narrower than the
/// dashboard's "due soon" list -- only past-due/due-today items count,
/// since a badge that never clears back to zero stops meaning anything.
/// </summary>
public static class BadgeCountProvider
{
    public static async Task<int> GetCountAsync(ApplicationDbContext context, string userId, DateOnly today)
    {
        var openNotes = await context.Notes
            .Where(n => n.UserId == userId && !n.IsDone)
            .ToListAsync();

        var notesDue = openNotes.Count(n =>
        {
            var date = Pages.Notes.IndexModel.SortDate(n);
            return date != DateTime.MaxValue && DateOnly.FromDateTime(date) <= today;
        });

        // Interview date has come and gone but the card hasn't been moved to
        // Interview Done yet -- the only other "due date" a job application carries.
        var interviewsDue = await context.JobApplications.CountAsync(a =>
            a.UserId == userId &&
            a.Status == ApplicationStatus.InterviewScheduled &&
            a.InterviewDate != null && a.InterviewDate <= today);

        return notesDue + interviewsDue;
    }
}
