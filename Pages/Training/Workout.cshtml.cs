using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Training;

/// <summary>
/// The "enter workout" screen: add exercises (filtered to the workout's own
/// session type) and fill in weight/reps per set. Each already-added exercise
/// renders its own set-editing form, using a weight widget that matches that
/// exercise's WeightType -- decided server-side per exercise, so no client-side
/// reactivity is needed to swap input types when the exercise selection changes.
/// </summary>
public class WorkoutModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Workout WorkoutEntity { get; set; } = default!;
    public SelectList ExerciseOptions { get; set; } = default!;

    [BindProperty]
    public AddExerciseInputModel AddExerciseInput { get; set; } = new();

    [BindProperty]
    public UpdateSetsInputModel UpdateSetsInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var workout = await LoadWorkoutAsync();
        if (workout is null)
        {
            return NotFound();
        }

        WorkoutEntity = workout;
        await LoadExerciseOptionsAsync(workout.SessionType);
        return Page();
    }

    public async Task<IActionResult> OnPostAddExerciseAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var workout = await context.Workouts.FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);
        if (workout is null)
        {
            return NotFound();
        }

        var exerciseOwned = await context.Exercises
            .AnyAsync(e => e.Id == AddExerciseInput.ExerciseId && e.UserId == userId && e.SessionType == workout.SessionType);

        if (!ModelState.IsValid || !exerciseOwned)
        {
            return RedirectToPage(new { id = Id });
        }

        var maxSortOrder = await context.WorkoutExercises
            .Where(we => we.WorkoutId == Id)
            .Select(we => (int?)we.SortOrder)
            .MaxAsync() ?? -1;

        context.WorkoutExercises.Add(new WorkoutExercise
        {
            WorkoutId = Id,
            ExerciseId = AddExerciseInput.ExerciseId,
            SortOrder = maxSortOrder + 1,
            Sets = Enumerable.Range(1, AddExerciseInput.SetCount)
                .Select(setNumber => new WorkoutSet { SetNumber = setNumber, Reps = 0, WeightKg = null })
                .ToList()
        });
        await context.SaveChangesAsync();

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUpdateSetsAsync()
    {
        var workoutExercise = await LoadWorkoutExerciseForEditAsync(UpdateSetsInput.WorkoutExerciseId);
        if (workoutExercise is null)
        {
            return NotFound();
        }

        ApplySetEdits(workoutExercise);
        await context.SaveChangesAsync();

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddSetAsync(int workoutExerciseId)
    {
        // UpdateSetsInput carries the same form's other rows -- save those first
        // so clicking "+ Add set" never silently discards unsaved edits.
        var workoutExercise = await LoadWorkoutExerciseForEditAsync(workoutExerciseId);
        if (workoutExercise is null)
        {
            return NotFound();
        }

        ApplySetEdits(workoutExercise);

        var nextSetNumber = workoutExercise.Sets.Count == 0 ? 1 : workoutExercise.Sets.Max(s => s.SetNumber) + 1;
        workoutExercise.Sets.Add(new WorkoutSet { SetNumber = nextSetNumber, Reps = 0, WeightKg = null });
        await context.SaveChangesAsync();

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveLastSetAsync(int workoutExerciseId)
    {
        var workoutExercise = await LoadWorkoutExerciseForEditAsync(workoutExerciseId);
        if (workoutExercise is null)
        {
            return NotFound();
        }

        ApplySetEdits(workoutExercise);

        var lastSet = workoutExercise.Sets.OrderBy(s => s.SetNumber).LastOrDefault();
        if (lastSet is not null)
        {
            context.WorkoutSets.Remove(lastSet);
        }

        await context.SaveChangesAsync();

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int workoutExerciseId)
    {
        var userId = userManager.GetUserId(User)!;

        var workoutExercise = await context.WorkoutExercises
            .Include(we => we.Workout)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout!.UserId == userId);

        if (workoutExercise is null)
        {
            return NotFound();
        }

        context.WorkoutExercises.Remove(workoutExercise);
        await context.SaveChangesAsync();

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var workout = await context.Workouts.FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);
        if (workout is null)
        {
            return NotFound();
        }

        context.Workouts.Remove(workout);
        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    private async Task<WorkoutExercise?> LoadWorkoutExerciseForEditAsync(int workoutExerciseId)
    {
        var userId = userManager.GetUserId(User)!;

        return await context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Exercise)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout!.UserId == userId);
    }

    /// <summary>Applies whatever rows UpdateSetsInput.Sets carries to the matching sets on this exercise -- shared by every handler on the per-exercise form, so no button ever silently discards unsaved edits.</summary>
    private void ApplySetEdits(WorkoutExercise workoutExercise)
    {
        foreach (var row in UpdateSetsInput.Sets)
        {
            var set = workoutExercise.Sets.FirstOrDefault(s => s.Id == row.SetId);
            if (set is null)
            {
                continue;
            }

            set.Reps = row.Reps;

            if (workoutExercise.Exercise!.WeightType == ExerciseWeightType.Machine)
            {
                var baseWeight = ParseDecimalOrNull(row.BaseWeightKg);
                var addOn = ParseDecimalOrNull(row.AddOnKg);
                set.BaseWeightKg = baseWeight;
                set.AddOnKg = addOn;
                set.WeightKg = baseWeight is { } b ? b + (addOn ?? 0) : null;
            }
            else
            {
                set.WeightKg = ParseDecimalOrNull(row.WeightKg);
            }
        }
    }

    private async Task<Workout?> LoadWorkoutAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var workout = await context.Workouts
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise!.MachineWeights)
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise!.MachineAddOns)
            .Include(w => w.Exercises).ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (workout is null)
        {
            return null;
        }

        workout.Exercises = workout.Exercises.OrderBy(we => we.SortOrder).ToList();
        foreach (var workoutExercise in workout.Exercises)
        {
            workoutExercise.Sets = workoutExercise.Sets.OrderBy(s => s.SetNumber).ToList();
        }

        return workout;
    }

    private async Task LoadExerciseOptionsAsync(TrainingSessionType sessionType)
    {
        var userId = userManager.GetUserId(User)!;

        var exercises = await context.Exercises
            .Where(e => e.UserId == userId && e.SessionType == sessionType)
            .OrderBy(e => e.Name)
            .ToListAsync();

        ExerciseOptions = new SelectList(exercises, nameof(Exercise.Id), nameof(Exercise.Name));
    }

    private static decimal? ParseDecimalOrNull(string? value) =>
        decimal.TryParse(value, out var result) ? result : null;

    public class AddExerciseInputModel
    {
        [Required]
        [Display(Name = "Exercise")]
        public int ExerciseId { get; set; }

        [Required]
        [Range(1, 20)]
        [Display(Name = "Sets")]
        public int SetCount { get; set; } = 3;
    }

    public class UpdateSetsInputModel
    {
        public int WorkoutExerciseId { get; set; }
        public List<SetRow> Sets { get; set; } = [];

        public class SetRow
        {
            public int SetId { get; set; }
            public string? WeightKg { get; set; }
            public string? BaseWeightKg { get; set; }
            public string? AddOnKg { get; set; }
            public int Reps { get; set; }
        }
    }
}
