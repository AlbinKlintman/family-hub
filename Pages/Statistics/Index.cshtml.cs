using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Statistics;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public JobSearchInputModel JobSearchInput { get; set; } = new();

    public string WeightChartDataJson { get; set; } = "[]";
    public string JobSearchChartDataJson { get; set; } = "[]";
    public string WorkoutChartDataJson { get; set; } = "[]";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddJobSearchAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var userId = userManager.GetUserId(User)!;

        var existing = await context.JobSearchLogs
            .FirstOrDefaultAsync(j => j.UserId == userId && j.Date == JobSearchInput.Date);

        if (existing is null)
        {
            context.JobSearchLogs.Add(new JobSearchLog
            {
                UserId = userId,
                Date = JobSearchInput.Date,
                Count = JobSearchInput.Count
            });
        }
        else
        {
            existing.Count = JobSearchInput.Count;
        }

        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var weightEntries = await context.WeightEntries
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Date).ThenBy(w => w.Time)
            .ToListAsync();

        WeightChartDataJson = JsonSerializer.Serialize(weightEntries.Select(w => new
        {
            label = w.Time is null ? w.Date.ToString("MMM d") : $"{w.Date:MMM d} {w.Time:HH:mm}",
            weightKg = w.WeightKg
        }), JsonOptions);

        var jobSearchLogs = await context.JobSearchLogs
            .Where(j => j.UserId == userId)
            .OrderBy(j => j.Date)
            .ToListAsync();

        JobSearchChartDataJson = JsonSerializer.Serialize(jobSearchLogs.Select(j => new
        {
            label = j.Date.ToString("MMM d"),
            count = j.Count
        }), JsonOptions);

        var workoutSets = await context.WorkoutSets
            .Where(s => s.WeightKg != null)
            .Join(context.WorkoutExercises, s => s.WorkoutExerciseId, we => we.Id, (s, we) => new { s, we })
            .Join(context.Workouts.Where(w => w.UserId == userId), x => x.we.WorkoutId, w => w.Id, (x, w) => new { x.s, x.we, w })
            .OrderBy(x => x.w.Date)
            .Select(x => new
            {
                exercise = x.we.Exercise!.Name,
                sessionType = x.w.SessionType.ToString(),
                date = x.w.Date.ToString("MMM d"),
                weightKg = x.s.WeightKg
            })
            .ToListAsync();

        WorkoutChartDataJson = JsonSerializer.Serialize(workoutSets, JsonOptions);
    }

    public class JobSearchInputModel
    {
        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Required]
        [Range(0, 500)]
        [Display(Name = "Jobs searched")]
        public int Count { get; set; }
    }
}
