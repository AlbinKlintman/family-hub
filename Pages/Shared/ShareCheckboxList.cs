namespace WebApp.Pages.Shared;

public record ShareOption(string UserId, string Username, bool IsShared);

/// <summary>A "Share with" checkbox per accepted connection -- shared by the Schedule and Folder create/edit forms.</summary>
public record ShareCheckboxList(string FieldName, IReadOnlyList<ShareOption> Options);
