using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Shared;

namespace WebApp.Pages.Family;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public record ConnectionView(int Id, AvatarInfo Avatar, string? MyLabelForThem);
    public record PendingIncomingView(int Id, AvatarInfo Avatar);
    public record PendingOutgoingView(int Id, string Username);

    public List<ConnectionView> Connections { get; private set; } = [];
    public List<PendingIncomingView> IncomingRequests { get; private set; } = [];
    public List<PendingOutgoingView> OutgoingRequests { get; private set; } = [];

    [BindProperty]
    public string NewRequestUsername { get; set; } = string.Empty;

    [BindProperty]
    public string LabelInput { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSendRequestAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var username = NewRequestUsername.Trim();

        if (username.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Enter a username.");
            await LoadAsync();
            return Page();
        }

        var targetProfile = await context.UserProfiles
            .FirstOrDefaultAsync(p => EF.Functions.ILike(p.Username, username));

        if (targetProfile is null)
        {
            ModelState.AddModelError(string.Empty, $"No user found with the username \"{username}\".");
            await LoadAsync();
            return Page();
        }

        if (targetProfile.UserId == userId)
        {
            ModelState.AddModelError(string.Empty, "You can't send a connection request to yourself.");
            await LoadAsync();
            return Page();
        }

        var alreadyExists = await context.FriendConnections.AnyAsync(f =>
            (f.RequesterUserId == userId && f.RecipientUserId == targetProfile.UserId) ||
            (f.RequesterUserId == targetProfile.UserId && f.RecipientUserId == userId));

        if (alreadyExists)
        {
            ModelState.AddModelError(string.Empty, "You're already connected (or have a pending request) with this user.");
            await LoadAsync();
            return Page();
        }

        context.FriendConnections.Add(new FriendConnection
        {
            RequesterUserId = userId,
            RecipientUserId = targetProfile.UserId
        });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAcceptAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var connection = await context.FriendConnections.FirstOrDefaultAsync(f =>
            f.Id == id && f.RecipientUserId == userId && f.Status == FriendConnectionStatus.Pending);

        if (connection is null)
        {
            return NotFound();
        }

        connection.Status = FriendConnectionStatus.Accepted;
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeclineAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var connection = await context.FriendConnections.FirstOrDefaultAsync(f =>
            f.Id == id && (f.RecipientUserId == userId || f.RequesterUserId == userId) && f.Status == FriendConnectionStatus.Pending);

        if (connection is null)
        {
            return NotFound();
        }

        context.FriendConnections.Remove(connection);
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var connection = await context.FriendConnections.FirstOrDefaultAsync(f =>
            f.Id == id && (f.RecipientUserId == userId || f.RequesterUserId == userId) && f.Status == FriendConnectionStatus.Accepted);

        if (connection is null)
        {
            return NotFound();
        }

        context.FriendConnections.Remove(connection);
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetLabelAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var connection = await context.FriendConnections.FirstOrDefaultAsync(f =>
            f.Id == id && (f.RecipientUserId == userId || f.RequesterUserId == userId) && f.Status == FriendConnectionStatus.Accepted);

        if (connection is null)
        {
            return NotFound();
        }

        var label = string.IsNullOrWhiteSpace(LabelInput) ? null : LabelInput.Trim();

        if (connection.RequesterUserId == userId)
        {
            connection.RequesterLabelForRecipient = label;
        }
        else
        {
            connection.RecipientLabelForRequester = label;
        }

        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var connections = await context.FriendConnections
            .Where(f => f.RequesterUserId == userId || f.RecipientUserId == userId)
            .ToListAsync();

        var otherUserIds = connections
            .Select(f => f.RequesterUserId == userId ? f.RecipientUserId : f.RequesterUserId)
            .Distinct()
            .ToList();

        var profiles = await context.UserProfiles
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);

        AvatarInfo AvatarFor(string otherUserId)
        {
            var profile = profiles.GetValueOrDefault(otherUserId);
            return new AvatarInfo(profile?.Username ?? "(unknown)", profile?.AvatarUrl, profile?.AccentColor ?? FolderColor.Gray);
        }

        Connections = connections
            .Where(f => f.Status == FriendConnectionStatus.Accepted)
            .Select(f =>
            {
                var otherUserId = f.RequesterUserId == userId ? f.RecipientUserId : f.RequesterUserId;
                var myLabel = f.RequesterUserId == userId ? f.RequesterLabelForRecipient : f.RecipientLabelForRequester;
                return new ConnectionView(f.Id, AvatarFor(otherUserId), myLabel);
            })
            .ToList();

        IncomingRequests = connections
            .Where(f => f.Status == FriendConnectionStatus.Pending && f.RecipientUserId == userId)
            .Select(f => new PendingIncomingView(f.Id, AvatarFor(f.RequesterUserId)))
            .ToList();

        OutgoingRequests = connections
            .Where(f => f.Status == FriendConnectionStatus.Pending && f.RequesterUserId == userId)
            .Select(f => new PendingOutgoingView(f.Id, profiles.GetValueOrDefault(f.RecipientUserId)?.Username ?? "(unknown)"))
            .ToList();
    }
}
