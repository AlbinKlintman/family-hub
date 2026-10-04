namespace WebApp.Models;

public class Schedule
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public FolderColor Color { get; set; } = FolderColor.Blue;

    /// <summary>Hides every note (and job application) tagged with this schedule everywhere except the Schedules page -- for everyone it's shared with too.</summary>
    public bool IsHidden { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<ScheduleShare> Shares { get; set; } = new List<ScheduleShare>();
}
