using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class CalendarEventProviderTests
{
    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetEventsForRangeAsync_DoneToDo_MarksEventAsDone()
    {
        using var db = BuildContext();
        var dueDate = new DateOnly(2026, 6, 15);
        db.Notes.Add(new ToDoNote { UserId = "u1", Title = "Buy milk", DueDate = dueDate, IsDone = true });
        await db.SaveChangesAsync();

        var events = await CalendarEventProvider.GetEventsForRangeAsync(db, "u1", dueDate, dueDate);

        Assert.True(events[dueDate].Single().IsDone);
    }

    [Fact]
    public async Task GetEventsForRangeAsync_OpenToDo_IsNotDone()
    {
        using var db = BuildContext();
        var dueDate = new DateOnly(2026, 6, 15);
        db.Notes.Add(new ToDoNote { UserId = "u1", Title = "Buy milk", DueDate = dueDate, IsDone = false });
        await db.SaveChangesAsync();

        var events = await CalendarEventProvider.GetEventsForRangeAsync(db, "u1", dueDate, dueDate);

        Assert.False(events[dueDate].Single().IsDone);
    }

    [Fact]
    public async Task GetEventsForRangeAsync_DoneLaundryNote_MarksEventAsDone()
    {
        using var db = BuildContext();
        var day = new DateOnly(2026, 6, 15);
        db.Notes.Add(new LaundryNote { UserId = "u1", Day = day, LaundryType = LaundryType.NormalClothes, Room = LaundryRoom.Room2Right, TimeWindow = LaundryTimeWindow.Afternoon, IsDone = true });
        await db.SaveChangesAsync();

        var events = await CalendarEventProvider.GetEventsForRangeAsync(db, "u1", day, day);

        Assert.True(events[day].Single().IsDone);
    }

    [Fact]
    public async Task GetEventsForRangeAsync_DoneWorkShiftNote_MarksEventAsDone()
    {
        using var db = BuildContext();
        var day = new DateOnly(2026, 6, 15);
        db.Notes.Add(new WorkShiftNote
        {
            UserId = "u1",
            Day = day,
            Location = "Warehouse",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            IsDone = true
        });
        await db.SaveChangesAsync();

        var events = await CalendarEventProvider.GetEventsForRangeAsync(db, "u1", day, day);

        Assert.True(events[day].Single().IsDone);
    }

    [Fact]
    public async Task GetEventsForRangeAsync_DoneFastingNote_MarksEventAsDone()
    {
        using var db = BuildContext();
        var day = new DateOnly(2026, 6, 15);
        db.Notes.Add(new FastingNote { UserId = "u1", Day = day, Level = FastingLevel.Meat, IsDone = true });
        await db.SaveChangesAsync();

        var events = await CalendarEventProvider.GetEventsForRangeAsync(db, "u1", day, day);

        Assert.True(events[day].Single().IsDone);
    }
}
