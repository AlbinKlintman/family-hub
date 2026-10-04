using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Pages.Shared;
using WebApp.Services;

namespace WebApp.Pages.Folders;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<Folder> RootFolders { get; set; } = [];
    public ILookup<int?, Folder> ByParent { get; set; } = Enumerable.Empty<Folder>().ToLookup(f => (int?)null);
    public Dictionary<int, int> NoteCounts { get; set; } = [];

    /// <summary>
    /// Folders whose notes someone else can see: shared directly, nested inside
    /// a shared folder, or linked to a schedule I've shared.
    /// </summary>
    public HashSet<int> SharedFolderIds { get; set; } = [];

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string NewFolderName { get; set; } = string.Empty;

    [BindProperty]
    public FolderColor NewFolderColor { get; set; } = FolderColor.Blue;

    [BindProperty]
    public int? NewFolderParentId { get; set; }

    /// <summary>Which accepted connections the new folder should be shared with straight away.</summary>
    [BindProperty]
    public List<string> NewFolderShareWithUserIds { get; set; } = [];

    public List<ShareOption> ShareOptions { get; private set; } = [];

    public SelectList ParentOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (NewFolderParentId is not null)
        {
            var parentOwned = await context.Folders.AnyAsync(f => f.Id == NewFolderParentId && f.UserId == userId);
            if (!parentOwned)
            {
                ModelState.AddModelError(nameof(NewFolderParentId), "Folder not found.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var folder = new Folder
        {
            UserId = userId,
            Name = NewFolderName.Trim(),
            Color = NewFolderColor,
            ParentFolderId = NewFolderParentId
        };
        foreach (var shareWith in await FriendConnectionProvider.FilterToAcceptedAsync(context, userId, NewFolderShareWithUserIds))
        {
            folder.Shares.Add(new FolderShare { SharedWithUserId = shareWith });
        }

        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folders = await context.Folders
            .Where(f => f.UserId == userId)
            .ToListAsync();

        ByParent = folders.ToLookup(f => f.ParentFolderId);
        RootFolders = ByParent[null].OrderBy(f => f.Name).ToList();

        NoteCounts = await context.Notes
            .Where(n => n.UserId == userId && n.FolderId != null)
            .GroupBy(n => n.FolderId!.Value)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FolderId, x => x.Count);

        var sharedScheduleIds = await context.Schedules
            .Where(s => s.UserId == userId && s.Shares.Any())
            .Select(s => s.Id)
            .ToListAsync();

        var directlySharedFolderIds = await context.Folders
            .Where(f => f.UserId == userId && f.Shares.Any())
            .Select(f => f.Id)
            .ToListAsync();

        SharedFolderIds = await NoteVisibilityProvider.WithDescendantsAsync(context, directlySharedFolderIds);
        SharedFolderIds.UnionWith(folders.Where(f => f.ScheduleId is { } id && sharedScheduleIds.Contains(id)).Select(f => f.Id));

        var selected = NewFolderShareWithUserIds.ToHashSet();
        ShareOptions = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId))
            .Select(kv => new ShareOption(kv.Key, kv.Value, selected.Contains(kv.Key)))
            .OrderBy(x => x.Username)
            .ToList();

        var flattened = folders.FlattenOrdered();
        ParentOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", NewFolderParentId);
    }
}
