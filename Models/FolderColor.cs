namespace WebApp.Models;

public enum FolderColor
{
    Blue,
    Green,
    Orange,
    Red,
    Purple,
    Pink,
    Yellow,
    Gray
}

public static class FolderColorExtensions
{
    /// <summary>
    /// Hex values matching the app's iOS-style palette (site.css --ios-* vars)
    /// where one already exists, otherwise the equivalent iOS system color.
    /// </summary>
    public static string ToHex(this FolderColor color) => color switch
    {
        FolderColor.Blue => "#0a84ff",
        FolderColor.Green => "#30d158",
        FolderColor.Orange => "#ff9f0a",
        FolderColor.Red => "#ff453a",
        FolderColor.Purple => "#bf5af2",
        FolderColor.Pink => "#ff375f",
        FolderColor.Yellow => "#ffd60a",
        FolderColor.Gray => "#8e8e93",
        _ => "#8e8e93"
    };

    public static string ToDisplayName(this FolderColor color) => color.ToString();
}
