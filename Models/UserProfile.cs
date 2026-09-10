namespace WebApp.Models;

/// <summary>
/// App-specific profile data layered on top of the stock IdentityUser rather
/// than a custom ApplicationUser subclass, so registration/login/account
/// management stay untouched. Created lazily the first time a user visits
/// Settings (see UserProfileProvider), not at registration time.
/// </summary>
public class UserProfile
{
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public string? AvatarUrl { get; set; }
    public FolderColor AccentColor { get; set; } = FolderColor.Blue;

    /// <summary>Off by default -- each person opts in for themselves rather than it being shown to everyone.</summary>
    public bool ShowTodaysFastCard { get; set; }

    /// <summary>This person's own Discord webhook -- reminders about their own notes/interviews post here instead of a single app-wide channel.</summary>
    public string? DiscordWebhookUrl { get; set; }

    /// <summary>This person's own Telegram chat id. The bot itself (token) is one shared app-wide bot, configured once by whoever runs the server -- only the chat id is per-user.</summary>
    public string? TelegramChatId { get; set; }
}
