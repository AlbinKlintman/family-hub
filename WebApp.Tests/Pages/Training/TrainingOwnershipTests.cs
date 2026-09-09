using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Training;

namespace WebApp.Tests.Pages.Training;

public class TrainingOwnershipTests
{
    private static async Task<(ApplicationDbContext Db, UserManager<IdentityUser> UserManager, IdentityUser Owner, IdentityUser Stranger)> BuildContextAsync()
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

        var stranger = new IdentityUser { UserName = "stranger@example.com", Email = "stranger@example.com" };
        await userManager.CreateAsync(stranger);

        return (db, userManager, owner, stranger);
    }

    private static TModel BuildPageModel<TModel>(
        Func<ApplicationDbContext, UserManager<IdentityUser>, TModel> factory,
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        string userId) where TModel : PageModel
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));

        var model = factory(db, userManager);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };
        return model;
    }

    [Fact]
    public async Task EditWeight_OnGet_ReturnsNotFound_ForAnotherUsersEntry()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var entry = new WeightEntry { UserId = owner.Id, Date = new DateOnly(2026, 8, 1), WeightKg = 63.9m };
        db.WeightEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditWeightModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = entry.Id;

        var result = await pageModel.OnGetAsync();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditWeight_OnPostDelete_ReturnsNotFound_ForAnotherUsersEntry_AndLeavesItIntact()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var entry = new WeightEntry { UserId = owner.Id, Date = new DateOnly(2026, 8, 1), WeightKg = 63.9m };
        db.WeightEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditWeightModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = entry.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, await db.WeightEntries.CountAsync());
    }

    [Fact]
    public async Task Workout_OnGet_ReturnsNotFound_ForAnotherUsersWorkout()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, SessionType = TrainingSessionType.Legs, Date = new DateOnly(2026, 8, 1) };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new WorkoutModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = workout.Id;

        var result = await pageModel.OnGetAsync();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Workout_OnPostDelete_ReturnsNotFound_ForAnotherUsersWorkout_AndLeavesItIntact()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, SessionType = TrainingSessionType.Legs, Date = new DateOnly(2026, 8, 1) };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new WorkoutModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = workout.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, await db.Workouts.CountAsync());
    }

    [Fact]
    public async Task Workout_OnPostAddExercise_ReturnsNotFound_ForAnotherUsersWorkout()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var workout = new Workout { UserId = owner.Id, SessionType = TrainingSessionType.Legs, Date = new DateOnly(2026, 8, 1) };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new WorkoutModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = workout.Id;

        var result = await pageModel.OnPostAddExerciseAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, await db.WorkoutExercises.CountAsync());
    }
}
