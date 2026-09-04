using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Media;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .Include(m => m.Links)
            .FirstOrDefaultAsync(m => m.Id == Id && m.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Title = entry.Title,
            Type = entry.Type,
            Status = entry.Status,
            Rating = entry.Rating,
            Season = entry.Season,
            Episode = entry.Episode,
            Chapter = entry.Chapter,
            Volume = entry.Volume,
            Watched = entry.Watched,
            Links = entry.Links.Select(l => l.Url).ToList()
        };
        if (Input.Links.Count == 0)
        {
            Input.Links.Add("");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .Include(m => m.Links)
            .FirstOrDefaultAsync(m => m.Id == Id && m.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        for (var i = 0; i < Input.Links.Count; i++)
        {
            Input.Links[i] ??= string.Empty;
        }

        for (var i = 0; i < Input.Links.Count; i++)
        {
            var link = Input.Links[i].Trim();
            if (link.Length > 0 && !UrlValidator.IsValid(link))
            {
                ModelState.AddModelError(string.Empty, $"Link {i + 1}: enter a valid URL.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        entry.Title = Input.Title.Trim();
        entry.Type = Input.Type;
        entry.Status = Input.Status;
        entry.Rating = Input.Rating;
        entry.Season = Input.Season;
        entry.Episode = Input.Episode;
        entry.Chapter = Input.Chapter;
        entry.Volume = Input.Volume;
        entry.Watched = Input.Watched;

        entry.Links.Clear();
        foreach (var url in Input.Links.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            entry.Links.Add(new MediaLink { Url = url });
        }

        await context.SaveChangesAsync();

        return RedirectToPage("/Media/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .FirstOrDefaultAsync(m => m.Id == Id && m.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        context.MediaEntries.Remove(entry);
        await context.SaveChangesAsync();

        return RedirectToPage("/Media/Index");
    }

    public class InputModel
    {
        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public MediaType Type { get; set; }

        [Required]
        public MediaStatus Status { get; set; } = MediaStatus.PlanToStart;

        [Range(1, 10)]
        [Display(Name = "Rating (1-10)")]
        public int? Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int? Season { get; set; }

        [Range(0, int.MaxValue)]
        public int? Episode { get; set; }

        [Range(0, int.MaxValue)]
        public int? Chapter { get; set; }

        [Range(0, int.MaxValue)]
        public int? Volume { get; set; }

        public bool Watched { get; set; }

        public List<string> Links { get; set; } = [""];
    }
}
