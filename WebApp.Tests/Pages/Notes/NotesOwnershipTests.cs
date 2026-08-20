using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Notes;

namespace WebApp.Tests.Pages.Notes;

public class NotesOwnershipTests
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
    public async Task Edit_OnGet_ReturnsNotFound_ForAnotherUsersNote()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var note = new ToDoNote { UserId = owner.Id, Title = "Owner's note" };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = note.Id;

        var result = await pageModel.OnGetAsync();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_OnPostDelete_ReturnsNotFound_ForAnotherUsersNote_AndLeavesItIntact()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var note = new ToDoNote { UserId = owner.Id, Title = "Owner's note" };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new EditModel(c, u), db, userManager, stranger.Id);
        pageModel.Id = note.Id;

        var result = await pageModel.OnPostDeleteAsync();

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, await db.Notes.CountAsync());
    }

    [Fact]
    public async Task Index_OnPostToggleDone_ReturnsNotFound_ForAnotherUsersNote_AndLeavesItUnchanged()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var note = new ToDoNote { UserId = owner.Id, Title = "Owner's note", IsDone = false };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new IndexModel(c, u), db, userManager, stranger.Id);

        var result = await pageModel.OnPostToggleDoneAsync(note.Id);

        Assert.IsType<NotFoundResult>(result);
        var reloaded = await db.Notes.FindAsync(note.Id);
        Assert.False(reloaded!.IsDone);
    }

    [Fact]
    public async Task Owner_CanToggleDone_OnOwnNote()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var note = new ToDoNote { UserId = owner.Id, Title = "Owner's note", IsDone = false };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel((c, u) => new IndexModel(c, u), db, userManager, owner.Id);

        var result = await pageModel.OnPostToggleDoneAsync(note.Id);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await db.Notes.FindAsync(note.Id);
        Assert.True(reloaded!.IsDone);
    }
}
