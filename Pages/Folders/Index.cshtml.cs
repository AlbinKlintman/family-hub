using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Folders;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<Folder> RootFolders { get; set; } = [];
    public ILookup<int?, Folder> ByParent { get; set; } = Enumerable.Empty<Folder>().ToLookup(f => (int?)null);
    public Dictionary<int, int> NoteCounts { get; set; } = [];

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string NewFolderName { get; set; } = string.Empty;

    [BindProperty]
    public FolderColor NewFolderColor { get; set; } = FolderColor.Blue;

    [BindProperty]
    public int? NewFolderParentId { get; set; }

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

        context.Folders.Add(new Folder
        {
            UserId = userId,
            Name = NewFolderName.Trim(),
            Color = NewFolderColor,
            ParentFolderId = NewFolderParentId
        });
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

        var flattened = folders.FlattenOrdered();
        ParentOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", NewFolderParentId);
    }
}
