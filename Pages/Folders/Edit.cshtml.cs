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

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ParentOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folder = await context.Folders.FirstOrDefaultAsync(f => f.Id == Id && f.UserId == userId);
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

        await LoadParentOptionsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folder = await context.Folders.FirstOrDefaultAsync(f => f.Id == Id && f.UserId == userId);
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
            await LoadParentOptionsAsync(userId);
            return Page();
        }

        folder.Name = Input.Name.Trim();
        folder.Color = Input.Color;
        folder.ParentFolderId = Input.ParentFolderId;

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

    private async Task LoadParentOptionsAsync(string userId)
    {
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
    }
}
