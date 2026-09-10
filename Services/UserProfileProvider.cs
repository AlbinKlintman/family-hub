using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public static class UserProfileProvider
{
    public static async Task<UserProfile> GetOrCreateAsync(ApplicationDbContext context, string userId, string fallbackUsername)
    {
        var profile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is not null)
        {
            return profile;
        }

        profile = new UserProfile { UserId = userId, Username = fallbackUsername };
        context.UserProfiles.Add(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    /// <summary>The part of the account's email before the @, as a reasonable first-run default before the user picks their own.</summary>
    public static string DefaultUsername(string? emailOrName)
    {
        if (string.IsNullOrWhiteSpace(emailOrName))
        {
            return "user";
        }

        var at = emailOrName.IndexOf('@');
        return at > 0 ? emailOrName[..at] : emailOrName;
    }
}
