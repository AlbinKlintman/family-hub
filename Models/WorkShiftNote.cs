namespace WebApp.Models;

public class WorkShiftNote : Note
{
    public DateOnly? Day { get; set; }
    public TimeOnly StartTime { get; set; } = new(7, 0);
    public TimeOnly EndTime { get; set; } = new(19, 0);
    public string Location { get; set; } = "Falun";

    /// <summary>Up to 4, enforced in the PageModel rather than the schema.</summary>
    public ICollection<Colleague> Colleagues { get; set; } = new List<Colleague>();
}
