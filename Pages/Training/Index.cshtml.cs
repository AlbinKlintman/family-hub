using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Training;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public WeightInputModel WeightInput { get; set; } = new();

    public List<WeightEntry> RecentWeightEntries { get; set; } = [];
    public List<WorkoutRow> RecentWorkouts { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddWeightAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var userId = userManager.GetUserId(User)!;

        context.WeightEntries.Add(new WeightEntry
        {
            UserId = userId,
            Date = WeightInput.Date,
            Time = WeightInput.Time,
            WeightKg = WeightInput.WeightKg
        });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = userManager.GetUserId(User)!;

        RecentWeightEntries = await context.WeightEntries
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.Time)
            .Take(10)
            .ToListAsync();

        RecentWorkouts = await context.Workouts
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.Id)
            .Take(10)
            .Select(w => new WorkoutRow(w.Id, w.Date, w.Time, w.SessionType, w.Exercises.Count))
            .ToListAsync();
    }

    public class WeightInputModel
    {
        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Display(Name = "Time")]
        public TimeOnly? Time { get; set; }

        [Required]
        [Range(20, 300)]
        [Display(Name = "Weight (kg)")]
        public decimal WeightKg { get; set; }
    }

    public record WorkoutRow(int Id, DateOnly Date, TimeOnly? Time, TrainingSessionType SessionType, int ExerciseCount);
}
