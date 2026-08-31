namespace WebApp.Models;

public class Folder
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public FolderColor Color { get; set; } = FolderColor.Blue;

    public int? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> Subfolders { get; set; } = new List<Folder>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();

    /// <summary>When set, notes filed in this folder also surface under this schedule on the Calendar.</summary>
    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }
}
