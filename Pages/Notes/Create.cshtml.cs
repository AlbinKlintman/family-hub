using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Notes;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList FolderOptions { get; set; } = default!;

    public async Task OnGetAsync(int? folderId)
    {
        Input.FolderId = folderId;
        await LoadFolderOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.NoteType == NoteType.ToDo && string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Note text is required.");
        }

        var userId = userManager.GetUserId(User)!;

        if (Input.FolderId is not null)
        {
            var folderOwned = await context.Folders.AnyAsync(f => f.Id == Input.FolderId && f.UserId == userId);
            if (!folderOwned)
            {
                ModelState.AddModelError(nameof(Input.FolderId), "Folder not found.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadFolderOptionsAsync();
            return Page();
        }

        Note note = Input.NoteType switch
        {
            NoteType.ToDo => new ToDoNote
            {
                UserId = userId,
                Title = Input.Title,
                DueDate = Input.DueDate,
                DueTime = Input.DueTime
            },
            NoteType.Laundry => new LaundryNote
            {
                UserId = userId,
                LaundryType = Input.LaundryType,
                Room = Input.Room,
                Day = Input.Day,
                TimeWindow = Input.TimeWindow
            },
            _ => throw new InvalidOperationException("Unknown note type.")
        };

        note.FolderId = Input.FolderId;
        note.Priority = Input.Priority;

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    private async Task LoadFolderOptionsAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var folders = await context.Folders.Where(f => f.UserId == userId).ToListAsync();
        var flattened = folders.FlattenOrdered();

        FolderOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", Input.FolderId);
    }

    public class InputModel
    {
        [Display(Name = "Folder")]
        public int? FolderId { get; set; }

        public NotePriority? Priority { get; set; }

        [Required]
        [Display(Name = "Type")]
        public NoteType NoteType { get; set; } = NoteType.ToDo;

        [StringLength(2000)]
        public string? Title { get; set; }

        [Display(Name = "Due date")]
        public DateOnly? DueDate { get; set; }

        [Display(Name = "Due time")]
        public TimeOnly? DueTime { get; set; }

        [Display(Name = "Type")]
        public LaundryType LaundryType { get; set; } = LaundryType.NormalClothes;

        [Display(Name = "Room")]
        public LaundryRoom Room { get; set; } = LaundryRoom.Room2Right;

        [Display(Name = "Day")]
        public DateOnly? Day { get; set; }

        [Display(Name = "Time window")]
        public LaundryTimeWindow TimeWindow { get; set; } = LaundryTimeWindow.Afternoon;
    }
}
