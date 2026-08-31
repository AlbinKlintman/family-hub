using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Colleagues;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<ColleagueRow> Colleagues { get; set; } = [];

    [BindProperty]
    [Required]
    [StringLength(200)]
    public string NewColleagueName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadColleaguesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            await LoadColleaguesAsync();
            return Page();
        }

        context.Colleagues.Add(new Colleague { UserId = userId, Name = NewColleagueName.Trim() });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadColleaguesAsync()
    {
        var userId = userManager.GetUserId(User)!;

        Colleagues = await context.Colleagues
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new ColleagueRow(c.Id, c.Name, c.Shifts.Count))
            .ToListAsync();
    }

    public record ColleagueRow(int Id, string Name, int ShiftCount);
}
