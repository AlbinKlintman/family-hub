namespace WebApp.Models;

public class ToDoNote : Note
{
    public DateOnly? DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }

    public DateTime? Reminder1hSentAtUtc { get; set; }
}
