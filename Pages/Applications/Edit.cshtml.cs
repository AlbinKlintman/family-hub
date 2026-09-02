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

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList CompanyOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;
    public SelectList StatusOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var application = await context.JobApplications
            .Include(a => a.Descriptions)
            .Include(a => a.Links)
            .FirstOrDefaultAsync(a => a.Id == Id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            RoleName = application.RoleName,
            Descriptions = application.Descriptions.Select(d => d.Text).ToList(),
            Links = application.Links.Select(l => l.Url).ToList(),
            CompanyId = application.CompanyId,
            ScheduleId = application.ScheduleId,
            Chance = application.Chance,
            Status = application.Status,
            InterviewDate = application.InterviewDate,
            InterviewTime = application.InterviewTime
        };

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var application = await context.JobApplications
            .Include(a => a.Descriptions)
            .Include(a => a.Links)
            .FirstOrDefaultAsync(a => a.Id == Id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

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

        application.RoleName = Input.RoleName;

        application.Descriptions.Clear();
        foreach (var text in Input.Descriptions.Select(d => d.Trim()).Where(d => d.Length > 0))
        {
            application.Descriptions.Add(new ApplicationDescription { Text = text });
        }

        application.Links.Clear();
        foreach (var url in Input.Links.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            application.Links.Add(new ApplicationLink { Url = url });
        }

        application.CompanyId = Input.CompanyId;
        application.ScheduleId = Input.ScheduleId;
        application.Chance = Input.Chance;
        if (application.InterviewDate != Input.InterviewDate || application.InterviewTime != Input.InterviewTime)
        {
            application.InterviewReminder24hSentAtUtc = null;
            application.InterviewReminder1hSentAtUtc = null;
        }
        application.InterviewDate = Input.InterviewDate;
        application.InterviewTime = Input.InterviewTime;
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

        var schedules = await context.Schedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        ScheduleOptions = new SelectList(schedules, nameof(Schedule.Id), nameof(Schedule.Name), Input.ScheduleId);

        var statuses = Enum.GetValues<ApplicationStatus>()
            .Select(s => new { Value = s, Text = s.ToDisplayName() });

        StatusOptions = new SelectList(statuses, "Value", "Text", Input.Status);
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

        [Required]
        public ApplicationStatus Status { get; set; }

        [Display(Name = "Interview date")]
        public DateOnly? InterviewDate { get; set; }

        [Display(Name = "Interview time")]
        public TimeOnly? InterviewTime { get; set; }
    }
}
