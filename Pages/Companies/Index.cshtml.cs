using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Companies;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<CompanyRow> Companies { get; set; } = new();

    [BindProperty]
    [Required]
    [StringLength(200)]
    public string NewCompanyName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadCompaniesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            await LoadCompaniesAsync();
            return Page();
        }

        context.Companies.Add(new Company { UserId = userId, Name = NewCompanyName.Trim() });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadCompaniesAsync()
    {
        var userId = userManager.GetUserId(User)!;

        Companies = await context.Companies
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyRow(c.Id, c.Name, c.JobApplications.Count))
            .ToListAsync();
    }

    public record CompanyRow(int Id, string Name, int ApplicationCount);
}
