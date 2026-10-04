namespace WebApp.Models;

public enum NoteType
{
    ToDo,
    Laundry,
    WorkShift,
    Fasting
}

public static class NoteTypeExtensions
{
    public static string ToDisplayName(this NoteType type) => type switch
    {
        NoteType.ToDo => "To-Do",
        NoteType.Laundry => "Laundry",
        NoteType.WorkShift => "Work Shift",
        NoteType.Fasting => "Fasting",
        _ => type.ToString()
    };

    public static NoteType GetNoteType(this Note note) => note switch
    {
        LaundryNote => NoteType.Laundry,
        WorkShiftNote => NoteType.WorkShift,
        FastingNote => NoteType.Fasting,
        _ => NoteType.ToDo
    };
}
