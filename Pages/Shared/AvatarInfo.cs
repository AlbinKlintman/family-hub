using WebApp.Models;

namespace WebApp.Pages.Shared;

/// <summary>Just enough to render an avatar (image, or an accent-colored initial as a fallback) -- shared by Settings and Family.</summary>
public record AvatarInfo(string Username, string? AvatarUrl, FolderColor AccentColor);
