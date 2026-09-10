using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Notes;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public const int MaxColleaguesPerShift = CreateModel.MaxColleaguesPerShift;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>Where to return to after Save/Delete/Cancel -- the Notes list URL, filters and all.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public NoteType CurrentType { get; private set; }
    public SelectList FolderOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;
    public MultiSelectList ColleagueOptions { get; set; } = default!;

    /// <summary>False when the current user only has this note shared with them -- they can only set their own Folder/Schedule/Priority, not the note's actual content.</summary>
    public bool IsOwner { get; private set; }

    public string? OwnerUsername { get; private set; }

    /// <summary>Owner-only: every accepted connection, and whether this note is currently shared with them.</summary>
    public List<(string UserId, string Username, bool IsShared)> ShareOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        SanitizeReturnUrl();

        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes
            .Include(n => n.Shares)
            .FirstOrDefaultAsync(n => n.Id == Id && (n.UserId == userId || n.Shares.Any(s => s.SharedWithUserId == userId)));
        if (note is null)
        {
            return NotFound();
        }

        IsOwner = note.UserId == userId;

        if (note is WorkShiftNote workShift)
        {
            await context.Entry(workShift).Collection(w => w.Colleagues).LoadAsync();
        }

        if (note is ToDoNote todoForLoad)
        {
            await context.Entry(todoForLoad).Collection(t => t.Reminders).LoadAsync();
        }

        LoadTypeSpecificFields(note);

        if (IsOwner)
        {
            Input.FolderId = note.FolderId;
            Input.ScheduleId = note.ScheduleId;
            Input.Priority = note.Priority;
            await LoadShareOptionsAsync(userId, note);
        }
        else
        {
            OwnerUsername = (await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == note.UserId))?.Username;
            var myShare = note.Shares.First(s => s.SharedWithUserId == userId);
            Input.FolderId = myShare.FolderId;
            Input.ScheduleId = myShare.ScheduleId;
            Input.Priority = myShare.Priority;
        }

        await LoadOptionsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SanitizeReturnUrl();

        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes
            .Include(n => n.Shares)
            .FirstOrDefaultAsync(n => n.Id == Id && (n.UserId == userId || n.Shares.Any(s => s.SharedWithUserId == userId)));
        if (note is null)
        {
            return NotFound();
        }

        IsOwner = note.UserId == userId;

        if (!IsOwner)
        {
            return await SaveViewerOverlayAsync(note, userId);
        }

        if (note is ToDoNote todoForSave)
        {
            await context.Entry(todoForSave).Collection(t => t.Reminders).LoadAsync();
        }

        CurrentType = note switch
        {
            ToDoNote => NoteType.ToDo,
            LaundryNote => NoteType.Laundry,
            WorkShiftNote => NoteType.WorkShift,
            FastingNote => NoteType.Fasting,
            _ => throw new InvalidOperationException("Unknown note type.")
        };

        if (note is ToDoNote && string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Note text is required.");
        }

        if (note is FastingNote && Input.FastingDay is null)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.FastingDay)}", "Date is required.");
        }

        if (note is ToDoNote && Input.RecurrenceIntervalUnit is not null && Input.RecurrenceIntervalValue is null or < 1)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.RecurrenceIntervalValue)}", "Enter how often this repeats.");
        }

        var colleagueIds = Input.ColleagueIds ?? [];

        if (note is WorkShiftNote && colleagueIds.Count > MaxColleaguesPerShift)
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
        if (note is WorkShiftNote && colleagueIds.Count > 0)
        {
            colleagues = await context.Colleagues
                .Where(c => c.UserId == userId && colleagueIds.Contains(c.Id))
                .ToListAsync();

            if (colleagues.Count != colleagueIds.Distinct().Count())
            {
                ModelState.AddModelError(nameof(Input.ColleagueIds), "Colleague not found.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadShareOptionsAsync(userId, note);
            await LoadOptionsAsync(userId);
            return Page();
        }

        switch (note)
        {
            case ToDoNote todo:
                todo.Title = Input.Title;
                todo.DueDate = Input.DueDate;
                todo.DueTime = Input.DueTime;
                todo.RecurrenceIntervalValue = Input.RecurrenceIntervalUnit is not null ? Input.RecurrenceIntervalValue : null;
                todo.RecurrenceIntervalUnit = Input.RecurrenceIntervalUnit;
                todo.Reminders.Clear();
                foreach (var reminderInput in Input.Reminders)
                {
                    todo.Reminders.Add(new NoteReminder { OffsetValue = reminderInput.OffsetValue, OffsetUnit = reminderInput.OffsetUnit });
                }
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
            case WorkShiftNote workShift:
                await context.Entry(workShift).Collection(w => w.Colleagues).LoadAsync();
                workShift.Day = Input.ShiftDay;
                workShift.StartTime = Input.StartTime;
                workShift.EndTime = Input.EndTime;
                workShift.Location = string.IsNullOrWhiteSpace(Input.Location) ? "Falun" : Input.Location.Trim();
                workShift.Colleagues.Clear();
                foreach (var colleague in colleagues)
                {
                    workShift.Colleagues.Add(colleague);
                }
                break;
            case FastingNote fasting:
                if (fasting.Day != Input.FastingDay!.Value)
                {
                    fasting.Reminder24hSentAtUtc = null;
                }
                fasting.Day = Input.FastingDay!.Value;
                fasting.Level = Input.FastingLevel;
                break;
        }

        note.FolderId = Input.FolderId;
        note.ScheduleId = Input.ScheduleId;
        note.Priority = Input.Priority;

        await SyncSharesAsync(note, userId);

        await context.SaveChangesAsync();

        return GetPostEditRedirect();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        SanitizeReturnUrl();

        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        context.Notes.Remove(note);
        await context.SaveChangesAsync();

        return GetPostEditRedirect();
    }

    private async Task<IActionResult> SaveViewerOverlayAsync(Note note, string userId)
    {
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

        if (!ModelState.IsValid)
        {
            LoadTypeSpecificFields(note);
            OwnerUsername = (await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == note.UserId))?.Username;
            await LoadOptionsAsync(userId);
            return Page();
        }

        var share = note.Shares.First(s => s.SharedWithUserId == userId);
        share.FolderId = Input.FolderId;
        share.ScheduleId = Input.ScheduleId;
        share.Priority = Input.Priority;

        await context.SaveChangesAsync();
        return GetPostEditRedirect();
    }

    /// <summary>Adds/removes NoteShare rows to match what the owner checked, but only ever for their actual accepted connections -- the posted ids are never trusted blindly.</summary>
    private async Task SyncSharesAsync(Note note, string ownerId)
    {
        var selected = new HashSet<string>(Input.ShareWithUserIds ?? []);
        var acceptedFriendIds = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, ownerId)).Keys;
        selected.IntersectWith(acceptedFriendIds);

        foreach (var toRemove in note.Shares.Where(s => !selected.Contains(s.SharedWithUserId)).ToList())
        {
            note.Shares.Remove(toRemove);
        }

        var existingIds = note.Shares.Select(s => s.SharedWithUserId).ToHashSet();
        foreach (var toAdd in selected.Except(existingIds))
        {
            note.Shares.Add(new NoteShare { SharedWithUserId = toAdd });
        }
    }

    private async Task LoadShareOptionsAsync(string userId, Note note)
    {
        var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
        var sharedWith = note.Shares.Select(s => s.SharedWithUserId).ToHashSet();

        ShareOptions = friendUsernames
            .Select(kv => (kv.Key, kv.Value, sharedWith.Contains(kv.Key)))
            .OrderBy(x => x.Value)
            .ToList();
    }

    /// <summary>
    /// ReturnUrl is validated as a local URL in SanitizeReturnUrl before this is
    /// ever called. Uses LocalRedirect/RedirectToPage's fragment overload rather
    /// than Url.Page(), which needs URL-generation services not present when
    /// unit-testing a PageModel directly (only a real request pipeline has them).
    /// </summary>
    private IActionResult GetPostEditRedirect()
    {
        var fragment = $"note-{Id}";
        return ReturnUrl is not null
            ? LocalRedirect($"{ReturnUrl}#{fragment}")
            : RedirectToPage("/Notes/Index", pageHandler: null, routeValues: null, fragment: fragment);
    }

    private void SanitizeReturnUrl()
    {
        if (ReturnUrl is not null && !Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }
    }

    private async Task LoadOptionsAsync(string userId)
    {
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

    private void LoadTypeSpecificFields(Note note)
    {
        switch (note)
        {
            case ToDoNote todo:
                CurrentType = NoteType.ToDo;
                Input.Title = todo.Title;
                Input.DueDate = todo.DueDate;
                Input.DueTime = todo.DueTime;
                Input.RecurrenceIntervalValue = todo.RecurrenceIntervalValue;
                Input.RecurrenceIntervalUnit = todo.RecurrenceIntervalUnit;
                Input.Reminders = todo.Reminders
                    .Select(r => new InputModel.ReminderInput { OffsetValue = r.OffsetValue, OffsetUnit = r.OffsetUnit })
                    .ToList();
                break;
            case LaundryNote laundry:
                CurrentType = NoteType.Laundry;
                Input.LaundryType = laundry.LaundryType;
                Input.Room = laundry.Room;
                Input.Day = laundry.Day;
                Input.TimeWindow = laundry.TimeWindow;
                break;
            case WorkShiftNote workShift:
                CurrentType = NoteType.WorkShift;
                Input.ShiftDay = workShift.Day;
                Input.StartTime = workShift.StartTime;
                Input.EndTime = workShift.EndTime;
                Input.Location = workShift.Location;
                Input.ColleagueIds = workShift.Colleagues.Select(c => c.Id).ToList();
                break;
            case FastingNote fasting:
                CurrentType = NoteType.Fasting;
                Input.FastingDay = fasting.Day;
                Input.FastingLevel = fasting.Level;
                break;
        }
    }

    public class InputModel
    {
        [Display(Name = "Folder")]
        public int? FolderId { get; set; }

        [Display(Name = "Schedule")]
        public int? ScheduleId { get; set; }

        public NotePriority? Priority { get; set; }

        [StringLength(20000)]
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

        /// <summary>Optional -- nullable so the multi-select isn't treated as implicitly required by the non-nullable-reference-type tag helper convention.</summary>
        [Display(Name = "Colleagues")]
        public List<int>? ColleagueIds { get; set; } = [];

        [Display(Name = "Date")]
        public DateOnly? FastingDay { get; set; }

        [Display(Name = "Fasting level")]
        public FastingLevel FastingLevel { get; set; } = FastingLevel.NoFast;

        /// <summary>Owner-only: which accepted connections this note should be shared with.</summary>
        public List<string> ShareWithUserIds { get; set; } = [];

        public class ReminderInput
        {
            [Range(1, int.MaxValue)]
            public int OffsetValue { get; set; }
            public TimeUnit OffsetUnit { get; set; } = TimeUnit.Minutes;
        }
    }
}
