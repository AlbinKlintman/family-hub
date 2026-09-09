using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Training;

namespace WebApp.Tests.Pages.Training;

public class WorkoutModelTests
{
    private static async Task<(ApplicationDbContext Db, UserManager<IdentityUser> UserManager, IdentityUser Owner)> BuildContextAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddLogging();
        services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        var provider = services.BuildServiceProvider();

        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

        var owner = new IdentityUser { UserName = "owner@example.com", Email = "owner@example.com" };
        await userManager.CreateAsync(owner);

        return (db, userManager, owner);
    }

    private static WorkoutModel BuildPageModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));

        return new WorkoutModel(db, userManager)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } }
        };
    }

    [Fact]
    public async Task AddExercise_CreatesTheRequestedNumberOfSets()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, Date = new DateOnly(2026, 8, 1), SessionType = TrainingSessionType.Pull };
        var exercise = new Exercise { UserId = owner.Id, Name = "Row", SessionType = TrainingSessionType.Pull, WeightType = ExerciseWeightType.FreeWeight };
        db.Workouts.Add(workout);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.AddExerciseInput = new WorkoutModel.AddExerciseInputModel { ExerciseId = exercise.Id, SetCount = 4 };

        await pageModel.OnPostAddExerciseAsync();

        var workoutExercise = await db.WorkoutExercises.Include(we => we.Sets).SingleAsync();
        Assert.Equal([1, 2, 3, 4], workoutExercise.Sets.Select(s => s.SetNumber).OrderBy(n => n));
    }

    [Fact]
    public async Task AddExercise_MismatchedSessionType_DoesNotAddIt()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, Date = new DateOnly(2026, 8, 1), SessionType = TrainingSessionType.Pull };
        var legExercise = new Exercise { UserId = owner.Id, Name = "Squat", SessionType = TrainingSessionType.Legs, WeightType = ExerciseWeightType.FreeWeight };
        db.Workouts.Add(workout);
        db.Exercises.Add(legExercise);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.AddExerciseInput = new WorkoutModel.AddExerciseInputModel { ExerciseId = legExercise.Id, SetCount = 3 };

        await pageModel.OnPostAddExerciseAsync();

        Assert.Equal(0, await db.WorkoutExercises.CountAsync());
    }

    [Fact]
    public async Task OnGet_ExerciseOptions_CarryTheExercisesSeatPositionSettings()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, Date = new DateOnly(2026, 8, 1), SessionType = TrainingSessionType.Legs };
        db.Workouts.Add(workout);
        db.Exercises.Add(new Exercise { UserId = owner.Id, Name = "Leg Extension", SessionType = TrainingSessionType.Legs, WeightType = ExerciseWeightType.Machine, SeatForwardPosition = 5m, SeatHeightPosition = 8m });
        db.Exercises.Add(new Exercise { UserId = owner.Id, Name = "Squat", SessionType = TrainingSessionType.Legs, WeightType = ExerciseWeightType.FreeWeight });
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;

        await pageModel.OnGetAsync();

        var legExtension = pageModel.ExerciseOptions.Single(o => o.Name == "Leg Extension");
        var squat = pageModel.ExerciseOptions.Single(o => o.Name == "Squat");
        Assert.Equal(5m, legExtension.SeatForwardPosition);
        Assert.Equal(8m, legExtension.SeatHeightPosition);
        Assert.Null(squat.SeatForwardPosition);
        Assert.Null(squat.SeatHeightPosition);
    }

    [Fact]
    public async Task UpdateSets_FreeWeightExercise_SavesTypedWeight()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var (workout, workoutExercise) = await SeedWorkoutExerciseAsync(db, owner.Id, ExerciseWeightType.FreeWeight);

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.UpdateSetsInput = new WorkoutModel.UpdateSetsInputModel
        {
            WorkoutExerciseId = workoutExercise.Id,
            Sets = [new WorkoutModel.UpdateSetsInputModel.SetRow { SetId = workoutExercise.Sets.First().Id, WeightKg = "82.5", Reps = 6 }]
        };

        await pageModel.OnPostUpdateSetsAsync();

        var set = await db.WorkoutSets.SingleAsync();
        Assert.Equal(82.5m, set.WeightKg);
        Assert.Equal(6, set.Reps);
    }

    [Fact]
    public async Task UpdateSets_MachineExercise_CombinesBaseAndAddOn()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var (workout, workoutExercise) = await SeedWorkoutExerciseAsync(db, owner.Id, ExerciseWeightType.Machine);

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.UpdateSetsInput = new WorkoutModel.UpdateSetsInputModel
        {
            WorkoutExerciseId = workoutExercise.Id,
            Sets = [new WorkoutModel.UpdateSetsInputModel.SetRow { SetId = workoutExercise.Sets.First().Id, BaseWeightKg = "40", AddOnKg = "10", Reps = 10 }]
        };

        await pageModel.OnPostUpdateSetsAsync();

        var set = await db.WorkoutSets.SingleAsync();
        Assert.Equal(40m, set.BaseWeightKg);
        Assert.Equal(10m, set.AddOnKg);
        Assert.Equal(50m, set.WeightKg);
    }

    [Fact]
    public async Task UpdateSets_MachineExercise_NoAddOnSelected_WeightIsJustBase()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var (workout, workoutExercise) = await SeedWorkoutExerciseAsync(db, owner.Id, ExerciseWeightType.Machine);

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.UpdateSetsInput = new WorkoutModel.UpdateSetsInputModel
        {
            WorkoutExerciseId = workoutExercise.Id,
            Sets = [new WorkoutModel.UpdateSetsInputModel.SetRow { SetId = workoutExercise.Sets.First().Id, BaseWeightKg = "60", AddOnKg = "", Reps = 8 }]
        };

        await pageModel.OnPostUpdateSetsAsync();

        var set = await db.WorkoutSets.SingleAsync();
        Assert.Equal(60m, set.WeightKg);
        Assert.Null(set.AddOnKg);
    }

    [Fact]
    public async Task AddSet_AppendsNextSetNumber_AndKeepsUnsavedEditsFromTheSameForm()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var (workout, workoutExercise) = await SeedWorkoutExerciseAsync(db, owner.Id, ExerciseWeightType.FreeWeight, setCount: 2);
        var firstSetId = workoutExercise.Sets.First().Id;

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.UpdateSetsInput = new WorkoutModel.UpdateSetsInputModel
        {
            WorkoutExerciseId = workoutExercise.Id,
            Sets = [new WorkoutModel.UpdateSetsInputModel.SetRow { SetId = firstSetId, WeightKg = "100", Reps = 5 }]
        };

        await pageModel.OnPostAddSetAsync(workoutExercise.Id);

        var reloaded = await db.WorkoutExercises.Include(we => we.Sets).SingleAsync();
        Assert.Equal(3, reloaded.Sets.Count);
        Assert.Equal([1, 2, 3], reloaded.Sets.Select(s => s.SetNumber).OrderBy(n => n));
        Assert.Equal(100m, reloaded.Sets.Single(s => s.Id == firstSetId).WeightKg);
    }

    [Fact]
    public async Task RemoveLastSet_RemovesOnlyTheHighestSetNumber()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var (workout, workoutExercise) = await SeedWorkoutExerciseAsync(db, owner.Id, ExerciseWeightType.FreeWeight, setCount: 3);

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.Id = workout.Id;
        pageModel.UpdateSetsInput = new WorkoutModel.UpdateSetsInputModel { WorkoutExerciseId = workoutExercise.Id, Sets = [] };

        await pageModel.OnPostRemoveLastSetAsync(workoutExercise.Id);

        var reloaded = await db.WorkoutExercises.Include(we => we.Sets).SingleAsync();
        Assert.Equal([1, 2], reloaded.Sets.Select(s => s.SetNumber).OrderBy(n => n));
    }

    private static async Task<(Workout Workout, WorkoutExercise WorkoutExercise)> SeedWorkoutExerciseAsync(
        ApplicationDbContext db, string userId, ExerciseWeightType weightType, int setCount = 1)
    {
        var workout = new Workout { UserId = userId, Date = new DateOnly(2026, 8, 1), SessionType = TrainingSessionType.Push };
        var exercise = new Exercise { UserId = userId, Name = "Test Exercise", SessionType = TrainingSessionType.Push, WeightType = weightType };
        db.Workouts.Add(workout);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            Sets = Enumerable.Range(1, setCount).Select(n => new WorkoutSet { SetNumber = n, Reps = 0 }).ToList()
        };
        db.WorkoutExercises.Add(workoutExercise);
        await db.SaveChangesAsync();

        return (workout, workoutExercise);
    }
}
