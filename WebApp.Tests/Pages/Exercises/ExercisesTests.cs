using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Exercises;

namespace WebApp.Tests.Pages.Exercises;

public class ExercisesTests
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
    public async Task Edit_OnPostDelete_ReturnsNotFound_ForAnotherUsersExercise()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var exercise = new Exercise { UserId = owner.Id, Name = "Deadlift" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = exercise.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, await db.Exercises.CountAsync());
    }

    [Fact]
    public async Task Delete_IsBlocked_WhenExerciseHasLoggedWorkouts()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var exercise = new Exercise { UserId = owner.Id, Name = "Deadlift" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        db.WorkoutLogs.Add(new WorkoutLog { UserId = owner.Id, ExerciseId = exercise.Id, SessionType = TrainingSessionType.Legs, WeightKg = 120m, Date = new DateOnly(2026, 8, 1) });
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditModel(c, u), db, userManager, owner.Id);
        pageModel.Id = exercise.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(pageModel.ModelState.IsValid);
        Assert.Equal(1, await db.Exercises.CountAsync());
    }

    [Fact]
    public async Task Delete_Succeeds_WhenExerciseHasNoLoggedWorkouts()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var exercise = new Exercise { UserId = owner.Id, Name = "Deadlift" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditModel(c, u), db, userManager, owner.Id);
        pageModel.Id = exercise.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(0, await db.Exercises.CountAsync());
    }
}
