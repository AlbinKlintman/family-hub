namespace WebApp.Models;

public class Colleague
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }

    public ICollection<WorkShiftNote> Shifts { get; set; } = new List<WorkShiftNote>();
}
