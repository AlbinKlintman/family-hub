using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Applications;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList CompanyOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        // Model binding leaves a null (not an empty string) for a blank indexed
        // form field, e.g. an untouched repeater row -- normalize before use.
        for (var i = 0; i < Input.Descriptions.Count; i++)
        {
            Input.Descriptions[i] ??= string.Empty;
        }
        for (var i = 0; i < Input.Links.Count; i++)
        {
            Input.Links[i] ??= string.Empty;
        }

        if (Input.CompanyId is not null)
        {
            var companyOwned = await context.Companies
                .AnyAsync(c => c.Id == Input.CompanyId && c.UserId == userId);

            if (!companyOwned)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.CompanyId)}", "Company not found.");
            }
        }

        if (Input.ScheduleId is not null)
        {
            var scheduleOwned = await context.Schedules.AnyAsync(s => s.Id == Input.ScheduleId && s.UserId == userId);
            if (!scheduleOwned)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ScheduleId)}", "Schedule not found.");
            }
        }

        for (var i = 0; i < Input.Links.Count; i++)
        {
            var link = Input.Links[i].Trim();
            if (link.Length > 0 && !UrlValidator.IsValid(link))
            {
                // Unkeyed: the summary shows it regardless of which dynamically-added row it came from.
                ModelState.AddModelError(string.Empty, $"Link {i + 1}: enter a valid URL.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var maxSortOrder = await context.JobApplications
            .Where(a => a.UserId == userId && a.Status == ApplicationStatus.Searching)
            .Select(a => (int?)a.SortOrder)
            .MaxAsync() ?? -1;

        var application = new JobApplication
        {
            UserId = userId,
            RoleName = Input.RoleName,
            Descriptions = Input.Descriptions
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .Select(d => new ApplicationDescription { Text = d })
                .ToList(),
            Links = Input.Links
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Select(l => new ApplicationLink { Url = l })
                .ToList(),
            CompanyId = Input.CompanyId,
            ScheduleId = Input.ScheduleId,
            Chance = Input.Chance,
            Status = ApplicationStatus.Searching,
            SortOrder = maxSortOrder + 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.JobApplications.Add(application);
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

        var schedules = await context.Schedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        ScheduleOptions = new SelectList(schedules, nameof(Schedule.Id), nameof(Schedule.Name), Input.ScheduleId);
    }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Role name")]
        public string RoleName { get; set; } = string.Empty;

        public List<string> Descriptions { get; set; } = [""];

        public List<string> Links { get; set; } = [""];

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [Display(Name = "Schedule")]
        public int? ScheduleId { get; set; }

        public ChanceLevel? Chance { get; set; }
    }
}
