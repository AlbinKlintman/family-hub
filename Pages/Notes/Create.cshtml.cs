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
    public const int MaxColleaguesPerShift = 4;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList FolderOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;
    public MultiSelectList ColleagueOptions { get; set; } = default!;

    public async Task OnGetAsync(int? folderId)
    {
        Input.FolderId = folderId;
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (Input.NoteType == NoteType.ToDo && string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Note text is required.");
        }

        if (Input.NoteType == NoteType.Fasting && Input.FastingDay is null)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.FastingDay)}", "Date is required.");
        }

        if (Input.NoteType == NoteType.ToDo && Input.RecurrenceIntervalUnit is not null && Input.RecurrenceIntervalValue is null or < 1)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.RecurrenceIntervalValue)}", "Enter how often this repeats.");
        }

        if (Input.NoteType == NoteType.WorkShift && Input.ColleagueIds.Count > MaxColleaguesPerShift)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ColleagueIds)}", $"Pick at most {MaxColleaguesPerShift} colleagues.");
        }

        if (Input.FolderId is not null)
        {
            var folderOwned = await context.Folders.AnyAsync(f => f.Id == Input.FolderId && f.UserId == userId);
            if (!folderOwned)
            {
                ModelState.AddModelError(nameof(Input.FolderId), "Folder not found.");
            }
        }

        if (Input.ScheduleId is not null)
        {
            var scheduleOwned = await context.Schedules.AnyAsync(s => s.Id == Input.ScheduleId && s.UserId == userId);
            if (!scheduleOwned)
            {
                ModelState.AddModelError(nameof(Input.ScheduleId), "Schedule not found.");
            }
        }

        List<Colleague> colleagues = [];
        if (Input.NoteType == NoteType.WorkShift && Input.ColleagueIds.Count > 0)
        {
            colleagues = await context.Colleagues
                .Where(c => c.UserId == userId && Input.ColleagueIds.Contains(c.Id))
                .ToListAsync();

            if (colleagues.Count != Input.ColleagueIds.Distinct().Count())
            {
                ModelState.AddModelError(nameof(Input.ColleagueIds), "Colleague not found.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        Note note = Input.NoteType switch
        {
            NoteType.ToDo => new ToDoNote
            {
                UserId = userId,
                Title = Input.Title,
                DueDate = Input.DueDate,
                DueTime = Input.DueTime,
                RecurrenceIntervalValue = Input.RecurrenceIntervalUnit is not null ? Input.RecurrenceIntervalValue : null,
                RecurrenceIntervalUnit = Input.RecurrenceIntervalUnit,
                Reminders = Input.Reminders.Select(r => new NoteReminder { OffsetValue = r.OffsetValue, OffsetUnit = r.OffsetUnit }).ToList()
            },
            NoteType.Laundry => new LaundryNote
            {
                UserId = userId,
                LaundryType = Input.LaundryType,
                Room = Input.Room,
                Day = Input.Day,
                TimeWindow = Input.TimeWindow
            },
            NoteType.WorkShift => new WorkShiftNote
            {
                UserId = userId,
                Day = Input.ShiftDay,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                Location = string.IsNullOrWhiteSpace(Input.Location) ? "Falun" : Input.Location.Trim(),
                Colleagues = colleagues
            },
            NoteType.Fasting => new FastingNote
            {
                UserId = userId,
                Day = Input.FastingDay!.Value,
                Level = Input.FastingLevel
            },
            _ => throw new InvalidOperationException("Unknown note type.")
        };

        note.FolderId = Input.FolderId;
        note.ScheduleId = Input.ScheduleId;
        note.Priority = Input.Priority;

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    private async Task LoadOptionsAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var folders = await context.Folders.Where(f => f.UserId == userId).ToListAsync();
        var flattened = folders.FlattenOrdered();
        FolderOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", Input.FolderId);

        var schedules = await context.Schedules.Where(s => s.UserId == userId).OrderBy(s => s.Name).ToListAsync();
        ScheduleOptions = new SelectList(schedules, nameof(Schedule.Id), nameof(Schedule.Name), Input.ScheduleId);

        var colleagues = await context.Colleagues.Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync();
        ColleagueOptions = new MultiSelectList(colleagues, nameof(Colleague.Id), nameof(Colleague.Name), Input.ColleagueIds);
    }

    public class InputModel
    {
        [Display(Name = "Folder")]
        public int? FolderId { get; set; }

        [Display(Name = "Schedule")]
        public int? ScheduleId { get; set; }

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

        [Display(Name = "Repeat every")]
        public int? RecurrenceIntervalValue { get; set; }

        public TimeUnit? RecurrenceIntervalUnit { get; set; }

        public List<ReminderInput> Reminders { get; set; } = [];

        [Display(Name = "Type")]
        public LaundryType LaundryType { get; set; } = LaundryType.NormalClothes;

        [Display(Name = "Room")]
        public LaundryRoom Room { get; set; } = LaundryRoom.Room2Right;

        [Display(Name = "Day")]
        public DateOnly? Day { get; set; }

        [Display(Name = "Time window")]
        public LaundryTimeWindow TimeWindow { get; set; } = LaundryTimeWindow.Afternoon;

        [Display(Name = "Day")]
        public DateOnly? ShiftDay { get; set; }

        [Display(Name = "Start time")]
        public TimeOnly StartTime { get; set; } = new(7, 0);

        [Display(Name = "End time")]
        public TimeOnly EndTime { get; set; } = new(19, 0);

        [Display(Name = "Location")]
        public string Location { get; set; } = "Falun";

        [Display(Name = "Colleagues")]
        public List<int> ColleagueIds { get; set; } = [];

        [Display(Name = "Date")]
        public DateOnly? FastingDay { get; set; }

        [Display(Name = "Fasting level")]
        public FastingLevel FastingLevel { get; set; } = FastingLevel.NoFast;

        public class ReminderInput
        {
            [Range(1, int.MaxValue)]
            public int OffsetValue { get; set; }
            public TimeUnit OffsetUnit { get; set; } = TimeUnit.Minutes;
        }
    }
}
