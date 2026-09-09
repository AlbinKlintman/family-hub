using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Exercises;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var weights = ParseWeights(Input.MachineWeights, "Weight");
        var addOns = ParseWeights(Input.MachineAddOns, "Add-on");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var exercise = new Exercise
        {
            UserId = userId,
            Name = Input.Name.Trim(),
            SessionType = Input.SessionType,
            WeightType = Input.WeightType,
            SeatForwardPosition = Input.SeatForwardPosition,
            SeatHeightPosition = Input.SeatHeightPosition,
            MachineWeights = weights.Select(w => new ExerciseMachineWeight { WeightKg = w }).ToList(),
            MachineAddOns = addOns.Select(a => new ExerciseMachineAddOn { AddOnKg = a }).ToList()
        };

        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        return RedirectToPage("/Exercises/Index");
    }

    /// <summary>
    /// Model binding leaves a null (not an empty string) for a blank indexed
    /// form field, e.g. an untouched repeater row -- normalize before use.
    /// Blank rows are skipped silently; a non-blank row that isn't a valid
    /// positive number becomes a ModelState error, same pattern as link validation.
    /// </summary>
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
        public TrainingSessionType SessionType { get; set; } = TrainingSessionType.Push;

        [Display(Name = "Weight type")]
        public ExerciseWeightType WeightType { get; set; } = ExerciseWeightType.FreeWeight;

        [Range(0, 999)]
        [Display(Name = "Seat/pad position (forward)")]
        public decimal? SeatForwardPosition { get; set; }

        [Range(0, 999)]
        [Display(Name = "Seat/pad height")]
        public decimal? SeatHeightPosition { get; set; }

        public List<string> MachineWeights { get; set; } = [""];

        public List<string> MachineAddOns { get; set; } = [""];
    }
}
