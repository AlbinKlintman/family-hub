namespace WebApp.Models;

public class ToDoNote : Note
{
    public DateOnly? DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }

    public int? RecurrenceIntervalValue { get; set; }
    public TimeUnit? RecurrenceIntervalUnit { get; set; }

    public ICollection<NoteReminder> Reminders { get; set; } = new List<NoteReminder>();
}
