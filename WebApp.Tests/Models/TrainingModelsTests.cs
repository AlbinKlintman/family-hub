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
    public async Task Workout_Exercise_And_Sets_RoundTrip_ThroughRelationships()
    {
        await using var db = NewContext();
        var exercise = new Exercise { UserId = "user-1", Name = "Bench Press", SessionType = TrainingSessionType.Push, WeightType = ExerciseWeightType.FreeWeight };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workout = new Workout { UserId = "user-1", Date = new DateOnly(2026, 8, 10), SessionType = TrainingSessionType.Push };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        db.WorkoutExercises.Add(new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            Sets =
            {
                new WorkoutSet { SetNumber = 1, WeightKg = 80m, Reps = 8 },
                new WorkoutSet { SetNumber = 2, WeightKg = 82.5m, Reps = 6 }
            }
        });
        await db.SaveChangesAsync();

        var loaded = await db.WorkoutExercises.Include(we => we.Exercise).Include(we => we.Sets).SingleAsync();

        Assert.Equal("Bench Press", loaded.Exercise!.Name);
        Assert.Equal(2, loaded.Sets.Count);
        Assert.Equal(80m, loaded.Sets.Single(s => s.SetNumber == 1).WeightKg);
        Assert.Equal(6, loaded.Sets.Single(s => s.SetNumber == 2).Reps);
    }

    [Fact]
    public async Task MachineExercise_TracksWeightStackAndAddOns()
    {
        await using var db = NewContext();
        var exercise = new Exercise
        {
            UserId = "user-1",
            Name = "Leg Press",
            SessionType = TrainingSessionType.Legs,
            WeightType = ExerciseWeightType.Machine,
            MachineWeights = { new ExerciseMachineWeight { WeightKg = 40m }, new ExerciseMachineWeight { WeightKg = 60m } },
            MachineAddOns = { new ExerciseMachineAddOn { AddOnKg = 5m }, new ExerciseMachineAddOn { AddOnKg = 10m } }
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var loaded = await db.Exercises.Include(e => e.MachineWeights).Include(e => e.MachineAddOns).SingleAsync();

        Assert.Equal([40m, 60m], loaded.MachineWeights.Select(w => w.WeightKg).OrderBy(w => w));
        Assert.Equal([5m, 10m], loaded.MachineAddOns.Select(a => a.AddOnKg).OrderBy(a => a));
    }

    [Fact]
    public async Task Exercise_SeatPositionSettings_AreOptional_AndCanBeSetIndependently()
    {
        await using var db = NewContext();
        db.Exercises.Add(new Exercise { UserId = "user-1", Name = "Leg Press", SessionType = TrainingSessionType.Legs, WeightType = ExerciseWeightType.Machine });
        db.Exercises.Add(new Exercise { UserId = "user-1", Name = "Lat Pulldown", SessionType = TrainingSessionType.Pull, WeightType = ExerciseWeightType.Machine, SeatHeightPosition = 8m });
        db.Exercises.Add(new Exercise { UserId = "user-1", Name = "Leg Extension", SessionType = TrainingSessionType.Legs, WeightType = ExerciseWeightType.Machine, SeatForwardPosition = 5m, SeatHeightPosition = 3.5m });
        await db.SaveChangesAsync();

        var noSettings = await db.Exercises.SingleAsync(e => e.Name == "Leg Press");
        var heightOnly = await db.Exercises.SingleAsync(e => e.Name == "Lat Pulldown");
        var both = await db.Exercises.SingleAsync(e => e.Name == "Leg Extension");

        Assert.Null(noSettings.SeatForwardPosition);
        Assert.Null(noSettings.SeatHeightPosition);

        Assert.Null(heightOnly.SeatForwardPosition);
        Assert.Equal(8m, heightOnly.SeatHeightPosition);

        Assert.Equal(5m, both.SeatForwardPosition);
        Assert.Equal(3.5m, both.SeatHeightPosition);
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
