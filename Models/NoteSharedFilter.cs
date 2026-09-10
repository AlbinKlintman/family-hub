namespace WebApp.Models;

public enum NoteSharedFilter
{
    /// <summary>Notes I own that I've shared with someone (directly, or via a schedule I've shared) -- regardless of done/not-done.</summary>
    ByMe,

    /// <summary>Notes someone else owns that are shared with me -- regardless of done/not-done.</summary>
    WithMe
}
