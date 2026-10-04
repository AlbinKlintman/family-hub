namespace WebApp.Models;

/// <summary>
/// Marks a Folder as visible to another account. Same shape as ScheduleShare:
/// no per-viewer overlay, just "every note filed in this folder -- or any of
/// its subfolders -- is visible to that person", computed live.
/// </summary>
public class FolderShare
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public required string SharedWithUserId { get; set; }
}
