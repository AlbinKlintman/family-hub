using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Notes;

namespace WebApp.Tests.Pages.Notes;

public class PastDueFilterTests
{
    private static readonly DateTime Today = DateTime.Now.Date;

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

    private static IndexModel BuildPageModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));

        return new IndexModel(db, userManager)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } }
        };
    }

    [Fact]
    public async Task PastDueFilter_ShowsOnlyOverdueNotDoneNotes()
    {
        var (db, userManager, owner) = await BuildContextAsync();

        var overdueNotDone = new ToDoNote { UserId = owner.Id, Title = "Overdue, still open", DueDate = DateOnly.FromDateTime(Today.AddDays(-2)), IsDone = false };
        var overdueDone = new ToDoNote { UserId = owner.Id, Title = "Overdue, but done", DueDate = DateOnly.FromDateTime(Today.AddDays(-1)), IsDone = true };
        var dueToday = new ToDoNote { UserId = owner.Id, Title = "Due today", DueDate = DateOnly.FromDateTime(Today), IsDone = false };
        var dueFuture = new ToDoNote { UserId = owner.Id, Title = "Due in the future", DueDate = DateOnly.FromDateTime(Today.AddDays(3)), IsDone = false };
        var noDueDate = new ToDoNote { UserId = owner.Id, Title = "No due date", IsDone = false };

        db.Notes.AddRange(overdueNotDone, overdueDone, dueToday, dueFuture, noDueDate);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.DueFilter = NoteDueFilter.PastDue;

        await pageModel.OnGetAsync();

        var resultTitles = pageModel.Notes.Select(n => n.Title).ToList();
        Assert.Equal(["Overdue, still open"], resultTitles);
    }

    [Fact]
    public async Task PastDueFilter_IgnoresShowCompletedCheckbox()
    {
        var (db, userManager, owner) = await BuildContextAsync();

        var overdueNotDone = new ToDoNote { UserId = owner.Id, Title = "Overdue, still open", DueDate = DateOnly.FromDateTime(Today.AddDays(-2)), IsDone = false };
        db.Notes.Add(overdueNotDone);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        pageModel.DueFilter = NoteDueFilter.PastDue;
        pageModel.ShowCompleted = true;

        await pageModel.OnGetAsync();

        Assert.Single(pageModel.Notes);
        Assert.Equal("Overdue, still open", pageModel.Notes[0].Title);
    }
}
