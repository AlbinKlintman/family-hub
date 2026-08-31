namespace WebApp.Models;

public class Schedule
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public FolderColor Color { get; set; } = FolderColor.Blue;

    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Folder> Folders { get; set; } = new List<Folder>();
}
