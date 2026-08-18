using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Applications;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList CompanyOptions { get; set; } = default!;
    public SelectList StatusOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var application = await context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == Id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            RoleName = application.RoleName,
            Description = application.Description,
            Link = application.Link,
            CompanyId = application.CompanyId,
            Chance = application.Chance,
            Status = application.Status,
            InterviewDate = application.InterviewDate
        };

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var application = await context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == Id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        if (Input.CompanyId is not null)
        {
            var companyOwned = await context.Companies
                .AnyAsync(c => c.Id == Input.CompanyId && c.UserId == userId);

            if (!companyOwned)
            {
                ModelState.AddModelError(nameof(Input.CompanyId), "Company not found.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        application.RoleName = Input.RoleName;
        application.Description = Input.Description;
        application.Link = Input.Link;
        application.CompanyId = Input.CompanyId;
        application.Chance = Input.Chance;
        application.InterviewDate = Input.InterviewDate;
        application.SetStatus(Input.Status);

        await context.SaveChangesAsync();

        return RedirectToPage("/Board/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var application = await context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == Id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        context.JobApplications.Remove(application);
        await context.SaveChangesAsync();

        return RedirectToPage("/Board/Index");
    }

    private async Task LoadOptionsAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var companies = await context.Companies
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        CompanyOptions = new SelectList(companies, nameof(Company.Id), nameof(Company.Name), Input.CompanyId);

        var statuses = Enum.GetValues<ApplicationStatus>()
            .Select(s => new { Value = s, Text = s.ToDisplayName() });

        StatusOptions = new SelectList(statuses, "Value", "Text", Input.Status);
    }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Url]
        public string? Link { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        public ChanceLevel? Chance { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; }

        [Display(Name = "Interview date")]
        public DateOnly? InterviewDate { get; set; }
    }
}
