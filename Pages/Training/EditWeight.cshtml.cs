using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Pages.Training;

public class EditWeightModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.WeightEntries
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Date = entry.Date,
            Time = entry.Time,
            WeightKg = entry.WeightKg
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.WeightEntries
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        entry.Date = Input.Date;
        entry.Time = Input.Time;
        entry.WeightKg = Input.WeightKg;

        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.WeightEntries
            .FirstOrDefaultAsync(w => w.Id == Id && w.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        context.WeightEntries.Remove(entry);
        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Time")]
        public TimeOnly? Time { get; set; }

        [Required]
        [Range(20, 300)]
        [Display(Name = "Weight (kg)")]
        public decimal WeightKg { get; set; }
    }
}
