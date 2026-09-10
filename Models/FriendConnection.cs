namespace WebApp.Models;

/// <summary>
/// A connection between two accounts. Declining a pending request, or
/// removing an accepted one, just deletes the row -- there's no need to
/// keep a record of past requests for a small family app, and it keeps
/// re-requesting simple.
/// </summary>
public class FriendConnection
{
    public int Id { get; set; }
    public required string RequesterUserId { get; set; }
    public required string RecipientUserId { get; set; }
    public FriendConnectionStatus Status { get; set; } = FriendConnectionStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>How the requester refers to the recipient, e.g. "Wife" -- private to the requester, never shown to the recipient.</summary>
    public string? RequesterLabelForRecipient { get; set; }

    /// <summary>How the recipient refers to the requester, e.g. "Husband" -- private to the recipient, never shown to the requester.</summary>
    public string? RecipientLabelForRequester { get; set; }
}
