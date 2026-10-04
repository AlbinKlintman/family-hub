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

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ParentOptions { get; set; } = default!;

    /// <summary>Every accepted connection, and whether this folder is currently shared with them.</summary>
    public List<ShareOption> ShareOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folder = await context.Folders.Include(f => f.Shares).FirstOrDefaultAsync(f => f.Id == Id && f.UserId == userId);
        if (folder is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = folder.Name,
            Color = folder.Color,
            ParentFolderId = folder.ParentFolderId
        };

        await LoadOptionsAsync(userId, folder);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folder = await context.Folders.Include(f => f.Shares).FirstOrDefaultAsync(f => f.Id == Id && f.UserId == userId);
        if (folder is null)
        {
            return NotFound();
        }

        if (Input.ParentFolderId is not null)
        {
            var allFolders = await context.Folders.Where(f => f.UserId == userId).ToListAsync();
            var parentOwned = allFolders.Any(f => f.Id == Input.ParentFolderId);

            if (!parentOwned)
            {
                ModelState.AddModelError(nameof(Input.ParentFolderId), "Folder not found.");
            }
            else if (allFolders.WouldCreateCycle(folder.Id, Input.ParentFolderId.Value))
            {
                ModelState.AddModelError(nameof(Input.ParentFolderId), "A folder can't be moved into itself or one of its own subfolders.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(userId, folder);
            return Page();
        }

        folder.Name = Input.Name.Trim();
        folder.Color = Input.Color;
        folder.ParentFolderId = Input.ParentFolderId;
        await SyncSharesAsync(folder, userId);

        await context.SaveChangesAsync();

        return RedirectToPage("/Folders/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folder = await context.Folders.FirstOrDefaultAsync(f => f.Id == Id && f.UserId == userId);
        if (folder is null)
        {
            return NotFound();
        }

        context.Folders.Remove(folder);
        await context.SaveChangesAsync();

        return RedirectToPage("/Folders/Index");
    }

    /// <summary>Adds/removes FolderShare rows to match what was checked, only ever for actual accepted connections.</summary>
    private async Task SyncSharesAsync(Folder folder, string ownerId)
    {
        var selected = await FriendConnectionProvider.FilterToAcceptedAsync(context, ownerId, Input.ShareWithUserIds);

        foreach (var toRemove in folder.Shares.Where(s => !selected.Contains(s.SharedWithUserId)).ToList())
        {
            folder.Shares.Remove(toRemove);
        }

        var existingIds = folder.Shares.Select(s => s.SharedWithUserId).ToHashSet();
        foreach (var toAdd in selected.Except(existingIds))
        {
            folder.Shares.Add(new FolderShare { SharedWithUserId = toAdd });
        }
    }

    private async Task LoadOptionsAsync(string userId, Folder folder)
    {
        var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
        var sharedWith = folder.Shares.Select(s => s.SharedWithUserId).ToHashSet();
        ShareOptions = friendUsernames
            .Select(kv => new ShareOption(kv.Key, kv.Value, sharedWith.Contains(kv.Key)))
            .OrderBy(x => x.Username)
            .ToList();

        var folders = await context.Folders
            .Where(f => f.UserId == userId && f.Id != Id)
            .ToListAsync();

        var flattened = folders.FlattenOrdered();
        ParentOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", Input.ParentFolderId);
    }

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public FolderColor Color { get; set; } = FolderColor.Blue;

        [Display(Name = "Parent")]
        public int? ParentFolderId { get; set; }

        /// <summary>Which accepted connections this folder (and every note filed in it or its subfolders) should be visible to.</summary>
        public List<string> ShareWithUserIds { get; set; } = [];
    }
}
