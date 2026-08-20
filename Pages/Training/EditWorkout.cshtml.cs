using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Training;

public class EditWorkoutModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ExerciseOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var log = await context.WorkoutLogs
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (log is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            ExerciseId = log.ExerciseId,
            SessionType = log.SessionType,
            WeightKg = log.WeightKg,
            Reps = log.Reps,
            Sets = log.Sets,
            Date = log.Date
        };

        await LoadExerciseOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var log = await context.WorkoutLogs
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (log is null)
        {
            return NotFound();
        }

        var exerciseOwned = await context.Exercises
            .AnyAsync(e => e.Id == Input.ExerciseId && e.UserId == userId);

        if (!exerciseOwned)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ExerciseId)}", "Exercise not found.");
        }

        if (!ModelState.IsValid)
        {
            await LoadExerciseOptionsAsync();
            return Page();
        }

        log.ExerciseId = Input.ExerciseId;
        log.SessionType = Input.SessionType;
        log.WeightKg = Input.WeightKg;
        log.Reps = Input.Reps;
        log.Sets = Input.Sets;
        log.Date = Input.Date;

        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var log = await context.WorkoutLogs
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (log is null)
        {
            return NotFound();
        }

        context.WorkoutLogs.Remove(log);
        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    private async Task LoadExerciseOptionsAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var exercises = await context.Exercises
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Name)
            .ToListAsync();

        ExerciseOptions = new SelectList(exercises, nameof(Exercise.Id), nameof(Exercise.Name), Input.ExerciseId);
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Exercise")]
        public int ExerciseId { get; set; }

        [Display(Name = "Session type")]
        public TrainingSessionType SessionType { get; set; }

        [Required]
        [Range(1, 500)]
        [Display(Name = "Weight (kg)")]
        public decimal WeightKg { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Reps")]
        public int Reps { get; set; }

        [Required]
        [Range(1, 20)]
        [Display(Name = "Sets")]
        public int Sets { get; set; } = 3;

        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; }
    }
}
