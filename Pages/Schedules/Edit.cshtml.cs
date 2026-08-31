using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Schedules;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        Input = new InputModel { Name = schedule.Name, Color = schedule.Color };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        schedule.Name = Input.Name.Trim();
        schedule.Color = Input.Color;
        await context.SaveChangesAsync();

        return RedirectToPage("/Schedules/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        context.Schedules.Remove(schedule);
        await context.SaveChangesAsync();

        return RedirectToPage("/Schedules/Index");
    }

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public FolderColor Color { get; set; } = FolderColor.Blue;
    }
}
