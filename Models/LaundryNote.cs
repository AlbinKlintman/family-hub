namespace WebApp.Models;

public class LaundryNote : Note
{
    public LaundryType LaundryType { get; set; } = LaundryType.NormalClothes;
    public LaundryRoom Room { get; set; } = LaundryRoom.Room2Right;
    public LaundryTimeWindow TimeWindow { get; set; } = LaundryTimeWindow.Afternoon;
    public DateOnly? Day { get; set; }
}
