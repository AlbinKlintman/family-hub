namespace WebApp.Models;

public class FastingNote : Note
{
    public required DateOnly Day { get; set; }
    public FastingLevel Level { get; set; } = FastingLevel.NoFast;
}
