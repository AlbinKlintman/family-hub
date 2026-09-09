using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Exercises;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .Include(e => e.MachineWeights)
            .Include(e => e.MachineAddOns)
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = exercise.Name,
            SessionType = exercise.SessionType,
            WeightType = exercise.WeightType,
            MachineWeights = exercise.MachineWeights.Select(w => w.WeightKg.ToString()).ToList(),
            MachineAddOns = exercise.MachineAddOns.Select(a => a.AddOnKg.ToString()).ToList()
        };
        if (Input.MachineWeights.Count == 0)
        {
            Input.MachineWeights.Add("");
        }
        if (Input.MachineAddOns.Count == 0)
        {
            Input.MachineAddOns.Add("");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .Include(e => e.MachineWeights)
            .Include(e => e.MachineAddOns)
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        var weights = ParseWeights(Input.MachineWeights, "Weight");
        var addOns = ParseWeights(Input.MachineAddOns, "Add-on");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        exercise.Name = Input.Name.Trim();
        exercise.SessionType = Input.SessionType;
        exercise.WeightType = Input.WeightType;

        exercise.MachineWeights.Clear();
        foreach (var weight in weights)
        {
            exercise.MachineWeights.Add(new ExerciseMachineWeight { WeightKg = weight });
        }

        exercise.MachineAddOns.Clear();
        foreach (var addOn in addOns)
        {
            exercise.MachineAddOns.Add(new ExerciseMachineAddOn { AddOnKg = addOn });
        }

        await context.SaveChangesAsync();

        return RedirectToPage("/Exercises/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        var hasLogs = await context.WorkoutExercises.AnyAsync(w => w.ExerciseId == Id);
        if (hasLogs)
        {
            ModelState.Clear();
            Input.Name = exercise.Name;
            ModelState.AddModelError(string.Empty, "Can't delete an exercise with logged workouts. Delete its workouts first.");
            return Page();
        }

        context.Exercises.Remove(exercise);
        await context.SaveChangesAsync();

        return RedirectToPage("/Exercises/Index");
    }

    /// <summary>See CreateModel.ParseWeights -- identical normalize/validate rules.</summary>
    private List<decimal> ParseWeights(List<string> values, string fieldLabel)
    {
        var parsed = new List<decimal>();
        for (var i = 0; i < values.Count; i++)
        {
            var trimmed = (values[i] ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (!decimal.TryParse(trimmed, out var value) || value <= 0)
            {
                ModelState.AddModelError(string.Empty, $"{fieldLabel} {i + 1}: enter a valid weight.");
                continue;
            }

            parsed.Add(value);
        }

        return parsed;
    }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Session type")]
        public TrainingSessionType SessionType { get; set; }

        [Display(Name = "Weight type")]
        public ExerciseWeightType WeightType { get; set; }

        public List<string> MachineWeights { get; set; } = [""];

        public List<string> MachineAddOns { get; set; } = [""];
    }
}
