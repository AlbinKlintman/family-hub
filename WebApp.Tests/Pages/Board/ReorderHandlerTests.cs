using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Board;

namespace WebApp.Tests.Pages.Board;

public class ReorderHandlerTests
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
    public async Task Reorder_OwnCard_UpdatesStatusAndSortOrder_AndReturnsAppliedDateWhenNewlyApplied()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var card = new JobApplication { UserId = owner.Id, RoleName = "Dev", Status = ApplicationStatus.Searching, SortOrder = 0 };
        db.JobApplications.Add(card);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        var result = await pageModel.OnPostReorderAsync(new IndexModel.ReorderRequest(card.Id, ApplicationStatus.Applied, [card.Id]));

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(ApplicationStatus.Applied, card.Status);
        Assert.Equal(0, card.SortOrder);
        Assert.NotNull(json.Value);
    }

    [Fact]
    public async Task Reorder_CardBelongingToAnotherUser_ReturnsNotFound_AndLeavesItUntouched()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var attacker = new IdentityUser { UserName = "attacker@example.com", Email = "attacker@example.com" };
        await userManager.CreateAsync(attacker);

        var card = new JobApplication { UserId = owner.Id, RoleName = "Dev", Status = ApplicationStatus.InterviewScheduled, SortOrder = 3 };
        db.JobApplications.Add(card);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, attacker.Id);
        var result = await pageModel.OnPostReorderAsync(new IndexModel.ReorderRequest(card.Id, ApplicationStatus.Rejected, [card.Id]));

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(ApplicationStatus.InterviewScheduled, card.Status);
        Assert.Equal(3, card.SortOrder);
    }

    [Fact]
    public async Task Reorder_OrderedIdsContainingUnownedCard_ReturnsBadRequest_AndLeavesDataUntouched()
    {
        var (db, userManager, owner) = await BuildContextAsync();
        var otherUser = new IdentityUser { UserName = "other@example.com", Email = "other@example.com" };
        await userManager.CreateAsync(otherUser);

        var ownCard = new JobApplication { UserId = owner.Id, RoleName = "Dev", Status = ApplicationStatus.Searching, SortOrder = 0 };
        var otherCard = new JobApplication { UserId = otherUser.Id, RoleName = "PM", Status = ApplicationStatus.Searching, SortOrder = 0 };
        db.JobApplications.AddRange(ownCard, otherCard);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);
        var result = await pageModel.OnPostReorderAsync(
            new IndexModel.ReorderRequest(ownCard.Id, ApplicationStatus.Applied, [ownCard.Id, otherCard.Id]));

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(ApplicationStatus.Searching, ownCard.Status);
        Assert.Equal(ApplicationStatus.Searching, otherCard.Status);
    }
}
