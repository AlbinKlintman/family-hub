using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public static class SectionVisibilityProvider
{
    /// <summary>Nothing is hidden for someone without a profile row yet.</summary>
    public static async Task<HashSet<AppSection>> GetHiddenSectionsAsync(ApplicationDbContext context, string userId)
    {
        var hidden = await context.UserProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.HiddenSections)
            .FirstOrDefaultAsync();
        return (hidden ?? []).ToHashSet();
    }
}

/// <summary>Per-request cache of the signed-in person's hidden sections, so the layout and the page filter share one lookup.</summary>
public class SectionVisibility(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
{
    private HashSet<AppSection>? hidden;

    public async Task<HashSet<AppSection>> GetHiddenAsync()
    {
        if (hidden is null)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userId = user is null ? null : userManager.GetUserId(user);
            hidden = userId is null ? [] : await SectionVisibilityProvider.GetHiddenSectionsAsync(context, userId);
        }

        return hidden;
    }

    public async Task<bool> IsVisibleAsync(AppSection section) => !(await GetHiddenAsync()).Contains(section);
}

/// <summary>A hidden section's pages bounce back to Home rather than just disappearing from the navbar -- its data stays untouched either way.</summary>
public class HiddenSectionPageFilter(SectionVisibility sectionVisibility) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var pagePath = context.ActionDescriptor.ViewEnginePath;
        if (string.IsNullOrEmpty(context.ActionDescriptor.AreaName)
            && AppSectionExtensions.SectionForPage(pagePath) is { } section
            && !await sectionVisibility.IsVisibleAsync(section))
        {
            context.Result = new RedirectToPageResult("/Index");
            return;
        }

        await next();
    }
}
