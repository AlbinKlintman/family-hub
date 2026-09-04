using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Media;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<MediaEntry> Entries { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public MediaType? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public MediaStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public string SummaryText { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var query = context.MediaEntries
            .Include(m => m.Links)
            .Where(m => m.UserId == userId);

        if (Type is not null)
        {
            query = query.Where(m => m.Type == Type);
        }

        if (Status is not null)
        {
            query = query.Where(m => m.Status == Status);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(m => EF.Functions.ILike(m.Title, $"%{term}%"));
        }

        Entries = await query
            .OrderBy(m => m.Title)
            .ToListAsync();

        SummaryText = BuildSummaryText(Entries.Count, Type, Status);
    }

    internal static string BuildSummaryText(int count, MediaType? type, MediaStatus? status)
    {
        var noun = count == 1 ? "entry" : "entries";
        var parts = new List<string?> { type?.ToDisplayName(), status?.ToDisplayName() }
            .Where(p => p is not null)
            .ToList();
        var suffix = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";

        return $"{count} {noun}{suffix}";
    }

    /// <summary>e.g. "S2 E5" for anime/series, "Ch. 12 (Vol. 3)" for manga, "Watched"/"Not watched" for a movie -- null if nothing has been recorded yet.</summary>
    internal static string? ProgressText(MediaEntry entry) => entry.Type switch
    {
        MediaType.Anime or MediaType.Series when entry.Season is not null || entry.Episode is not null =>
            string.Join(" ", new[]
            {
                entry.Season is { } s ? $"S{s}" : null,
                entry.Episode is { } e ? $"E{e}" : null
            }.Where(p => p is not null)),
        MediaType.Manga when entry.Chapter is not null || entry.Volume is not null =>
            string.Join(" ", new[]
            {
                entry.Chapter is { } c ? $"Ch. {c}" : null,
                entry.Volume is { } v ? $"(Vol. {v})" : null
            }.Where(p => p is not null)),
        MediaType.Movie => entry.Watched ? "Watched" : "Not watched",
        _ => null
    };
}
