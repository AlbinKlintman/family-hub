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

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public NoteType CurrentType { get; private set; }
    public SelectList FolderOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        LoadTypeSpecificFields(note);
        Input.FolderId = note.FolderId;
        Input.Priority = note.Priority;

        await LoadFolderOptionsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        if (note is ToDoNote)
        {
            CurrentType = NoteType.ToDo;
            if (string.IsNullOrWhiteSpace(Input.Title))
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Note text is required.");
            }
        }
        else
        {
            CurrentType = NoteType.Laundry;
        }

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
            await LoadFolderOptionsAsync(userId);
            return Page();
        }

        switch (note)
        {
            case ToDoNote todo:
                todo.Title = Input.Title;
                if (todo.DueDate != Input.DueDate || todo.DueTime != Input.DueTime)
                {
                    todo.Reminder24hSentAtUtc = null;
                    todo.Reminder1hSentAtUtc = null;
                }
                todo.DueDate = Input.DueDate;
                todo.DueTime = Input.DueTime;
                break;
            case LaundryNote laundry:
                laundry.LaundryType = Input.LaundryType;
                laundry.Room = Input.Room;
                if (laundry.Day != Input.Day || laundry.TimeWindow != Input.TimeWindow)
                {
                    laundry.Reminder24hSentAtUtc = null;
                }
                laundry.Day = Input.Day;
                laundry.TimeWindow = Input.TimeWindow;
                break;
        }

        note.FolderId = Input.FolderId;
        note.Priority = Input.Priority;

        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    private async Task LoadFolderOptionsAsync(string userId)
    {
        var folders = await context.Folders.Where(f => f.UserId == userId).ToListAsync();
        var flattened = folders.FlattenOrdered();

        FolderOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", Input.FolderId);
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        context.Notes.Remove(note);
        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    private void LoadTypeSpecificFields(Note note)
    {
        switch (note)
        {
            case ToDoNote todo:
                CurrentType = NoteType.ToDo;
                Input.Title = todo.Title;
                Input.DueDate = todo.DueDate;
                Input.DueTime = todo.DueTime;
                break;
            case LaundryNote laundry:
                CurrentType = NoteType.Laundry;
                Input.LaundryType = laundry.LaundryType;
                Input.Room = laundry.Room;
                Input.Day = laundry.Day;
                Input.TimeWindow = laundry.TimeWindow;
                break;
        }
    }

    public class InputModel
    {
        [Display(Name = "Folder")]
        public int? FolderId { get; set; }

        public NotePriority? Priority { get; set; }

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
