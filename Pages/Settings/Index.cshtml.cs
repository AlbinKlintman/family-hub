using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Settings;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var profile = await UserProfileProvider.GetOrCreateAsync(context, userId, UserProfileProvider.DefaultUsername(User.Identity?.Name));

        Input = new InputModel
        {
            Username = profile.Username,
            AvatarUrl = profile.AvatarUrl,
            AccentColor = profile.AccentColor,
            ShowTodaysFastCard = profile.ShowTodaysFastCard,
            DiscordWebhookUrl = profile.DiscordWebhookUrl,
            TelegramChatId = profile.TelegramChatId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (Input.AvatarUrl is { Length: > 0 } avatarUrl && !UrlValidator.IsValid(avatarUrl))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.AvatarUrl)}", "Enter a valid URL.");
        }

        if (Input.DiscordWebhookUrl is { Length: > 0 } webhookUrl && !UrlValidator.IsValid(webhookUrl))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.DiscordWebhookUrl)}", "Enter a valid URL.");
        }

        var usernameTaken = await context.UserProfiles
            .AnyAsync(p => p.UserId != userId && p.Username == Input.Username);
        if (usernameTaken)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Username)}", "That username is already taken.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var profile = await UserProfileProvider.GetOrCreateAsync(context, userId, Input.Username);
        profile.Username = Input.Username.Trim();
        profile.AvatarUrl = string.IsNullOrWhiteSpace(Input.AvatarUrl) ? null : Input.AvatarUrl.Trim();
        profile.AccentColor = Input.AccentColor;
        profile.ShowTodaysFastCard = Input.ShowTodaysFastCard;
        profile.DiscordWebhookUrl = string.IsNullOrWhiteSpace(Input.DiscordWebhookUrl) ? null : Input.DiscordWebhookUrl.Trim();
        profile.TelegramChatId = string.IsNullOrWhiteSpace(Input.TelegramChatId) ? null : Input.TelegramChatId.Trim();

        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    public class InputModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Avatar image URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Accent color")]
        public FolderColor AccentColor { get; set; } = FolderColor.Blue;

        [Display(Name = "Show \"Today's fast\" card on the dashboard")]
        public bool ShowTodaysFastCard { get; set; }

        [Display(Name = "Discord webhook URL")]
        public string? DiscordWebhookUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Telegram chat ID")]
        public string? TelegramChatId { get; set; }
    }
}
