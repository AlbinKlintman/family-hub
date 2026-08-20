using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Tests.Models;

public class NoteTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ToDoNote_RoundTrips_ThroughSharedNotesSet()
    {
        await using var db = NewContext();
        db.Notes.Add(new ToDoNote
        {
            UserId = "user-1",
            Title = "Buy milk",
            DueDate = new DateOnly(2026, 9, 1),
            DueTime = new TimeOnly(18, 0)
        });
        await db.SaveChangesAsync();

        var loaded = await db.Notes.OfType<ToDoNote>().SingleAsync();

        Assert.Equal("Buy milk", loaded.Title);
        Assert.Equal(new DateOnly(2026, 9, 1), loaded.DueDate);
        Assert.Equal(new TimeOnly(18, 0), loaded.DueTime);
        Assert.False(loaded.IsDone);
    }

    [Fact]
    public async Task LaundryNote_RoundTrips_ThroughSharedNotesSet()
    {
        await using var db = NewContext();
        db.Notes.Add(new LaundryNote
        {
            UserId = "user-1",
            LaundryType = LaundryType.BedLinenAndTowels,
            Room = LaundryRoom.Room1Left,
            Day = new DateOnly(2026, 9, 3),
            TimeWindow = LaundryTimeWindow.Midday
        });
        await db.SaveChangesAsync();

        var loaded = await db.Notes.OfType<LaundryNote>().SingleAsync();

        Assert.Equal(LaundryType.BedLinenAndTowels, loaded.LaundryType);
        Assert.Equal(LaundryRoom.Room1Left, loaded.Room);
        Assert.Equal(new DateOnly(2026, 9, 3), loaded.Day);
        Assert.Equal(LaundryTimeWindow.Midday, loaded.TimeWindow);
    }

    [Fact]
    public void LaundryNote_UsesExpectedDefaults()
    {
        var note = new LaundryNote { UserId = "user-1" };

        Assert.Equal(LaundryType.NormalClothes, note.LaundryType);
        Assert.Equal(LaundryRoom.Room2Right, note.Room);
        Assert.Equal(LaundryTimeWindow.Afternoon, note.TimeWindow);
    }

    [Fact]
    public async Task Notes_OfDifferentTypes_AreDistinguishableThroughBaseSet()
    {
        await using var db = NewContext();
        db.Notes.Add(new ToDoNote { UserId = "user-1", Title = "Todo" });
        db.Notes.Add(new LaundryNote { UserId = "user-1" });
        await db.SaveChangesAsync();

        var all = await db.Notes.Where(n => n.UserId == "user-1").ToListAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, n => n is ToDoNote);
        Assert.Contains(all, n => n is LaundryNote);
    }
}
