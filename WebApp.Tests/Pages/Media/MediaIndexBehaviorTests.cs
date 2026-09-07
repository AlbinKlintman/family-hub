using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Media;

namespace WebApp.Tests.Pages.Media;

public class MediaIndexBehaviorTests
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
    public async Task OnGet_SortsInProgressEntriesFirst()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();

        db.MediaEntries.AddRange(
            new MediaEntry { UserId = owner.Id, Title = "Aardvark Saga", Type = MediaType.Anime, Status = MediaStatus.Completed },
            new MediaEntry { UserId = owner.Id, Title = "Zzz Chronicles", Type = MediaType.Anime, Status = MediaStatus.InProgress },
            new MediaEntry { UserId = owner.Id, Title = "Middle Ground", Type = MediaType.Anime, Status = MediaStatus.PlanToStart });
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);

        await pageModel.OnGetAsync();

        Assert.Equal("Zzz Chronicles", pageModel.Entries[0].Title);
    }

    [Fact]
    public async Task IncrementProgress_Anime_IncrementsEpisode()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var entry = new MediaEntry { UserId = owner.Id, Title = "Test", Type = MediaType.Anime, Episode = 4 };
        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);

        var result = await pageModel.OnPostIncrementProgressAsync(entry.Id);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await db.MediaEntries.FindAsync(entry.Id);
        Assert.Equal(5, reloaded!.Episode);
    }

    [Fact]
    public async Task IncrementProgress_Manga_IncrementsChapterFromNull()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var entry = new MediaEntry { UserId = owner.Id, Title = "Test", Type = MediaType.Manga };
        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);

        await pageModel.OnPostIncrementProgressAsync(entry.Id);

        var reloaded = await db.MediaEntries.FindAsync(entry.Id);
        Assert.Equal(1, reloaded!.Chapter);
    }

    [Fact]
    public async Task IncrementProgress_Book_IncrementsChapter_LeavesPageUnchanged()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var entry = new MediaEntry { UserId = owner.Id, Title = "Test", Type = MediaType.Book, Chapter = 3, Page = 88 };
        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);

        await pageModel.OnPostIncrementProgressAsync(entry.Id);

        var reloaded = await db.MediaEntries.FindAsync(entry.Id);
        Assert.Equal(4, reloaded!.Chapter);
        Assert.Equal(88, reloaded.Page);
    }

    [Fact]
    public async Task IncrementProgress_Movie_LeavesEntryUnchanged()
    {
        var (db, userManager, owner, _) = await BuildContextAsync();
        var entry = new MediaEntry { UserId = owner.Id, Title = "Test", Type = MediaType.Movie, Watched = false };
        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, owner.Id);

        await pageModel.OnPostIncrementProgressAsync(entry.Id);

        var reloaded = await db.MediaEntries.FindAsync(entry.Id);
        Assert.False(reloaded!.Watched);
    }

    [Fact]
    public async Task IncrementProgress_ReturnsNotFound_ForAnotherUsersEntry()
    {
        var (db, userManager, owner, stranger) = await BuildContextAsync();
        var entry = new MediaEntry { UserId = owner.Id, Title = "Test", Type = MediaType.Manga, Chapter = 1 };
        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync();

        var pageModel = BuildPageModel(db, userManager, stranger.Id);

        var result = await pageModel.OnPostIncrementProgressAsync(entry.Id);

        Assert.IsType<NotFoundResult>(result);
        var reloaded = await db.MediaEntries.FindAsync(entry.Id);
        Assert.Equal(1, reloaded!.Chapter);
    }
}
