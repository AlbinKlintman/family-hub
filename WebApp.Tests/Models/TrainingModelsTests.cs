using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Tests.Models;

public class TrainingModelsTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task WeightEntry_RoundTrips_WithOptionalTime()
    {
        await using var db = NewContext();
        db.WeightEntries.Add(new WeightEntry { UserId = "user-1", Date = new DateOnly(2026, 8, 2), Time = new TimeOnly(7, 33), WeightKg = 63.3m });
        db.WeightEntries.Add(new WeightEntry { UserId = "user-1", Date = new DateOnly(2026, 8, 2), Time = new TimeOnly(20, 25), WeightKg = 64.3m });
        await db.SaveChangesAsync();

        var entries = await db.WeightEntries.OrderBy(w => w.Time).ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal(63.3m, entries[0].WeightKg);
        Assert.Equal(new TimeOnly(7, 33), entries[0].Time);
    }

    [Fact]
    public async Task Exercise_And_WorkoutLog_RoundTrip_ThroughRelationship()
    {
        await using var db = NewContext();
        var exercise = new Exercise { UserId = "user-1", Name = "Bench Press" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        db.WorkoutLogs.Add(new WorkoutLog
        {
            UserId = "user-1",
            ExerciseId = exercise.Id,
            SessionType = TrainingSessionType.Push,
            WeightKg = 80m,
            Date = new DateOnly(2026, 8, 10)
        });
        await db.SaveChangesAsync();

        var loaded = await db.WorkoutLogs.Include(w => w.Exercise).SingleAsync();

        Assert.Equal("Bench Press", loaded.Exercise!.Name);
        Assert.Equal(TrainingSessionType.Push, loaded.SessionType);
        Assert.Equal(80m, loaded.WeightKg);
    }

    [Fact]
    public async Task JobSearchLog_HasUniqueDatePerUser()
    {
        await using var db = NewContext();
        db.JobSearchLogs.Add(new JobSearchLog { UserId = "user-1", Date = new DateOnly(2026, 8, 5), Count = 12 });
        await db.SaveChangesAsync();

        var existing = await db.JobSearchLogs.FirstOrDefaultAsync(j => j.UserId == "user-1" && j.Date == new DateOnly(2026, 8, 5));
        Assert.NotNull(existing);

        existing!.Count = 20;
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.JobSearchLogs.CountAsync());
        Assert.Equal(20, (await db.JobSearchLogs.SingleAsync()).Count);
    }
}
