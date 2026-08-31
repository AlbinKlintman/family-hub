using WebApp.Models;
using WebApp.Pages.Notes;

namespace WebApp.Tests.Pages.Notes;

public class SummaryTextTests
{
    [Fact]
    public void Default_NoFilters_JustCountAndNotes()
    {
        var text = IndexModel.BuildSummaryText(5, noteType: null, showCompleted: false, folderLabel: null, scheduleLabel: null);
        Assert.Equal("5 notes", text);
    }

    [Fact]
    public void SingularCount_UsesSingularNoun()
    {
        var text = IndexModel.BuildSummaryText(1, noteType: null, showCompleted: false, folderLabel: null, scheduleLabel: null);
        Assert.Equal("1 note", text);
    }

    [Fact]
    public void ShowCompleted_PrefixesCompletedPast()
    {
        var text = IndexModel.BuildSummaryText(3, noteType: null, showCompleted: true, folderLabel: null, scheduleLabel: null);
        Assert.Equal("3 completed/past notes", text);
    }

    [Fact]
    public void NoteTypeFilter_UsesTypeSpecificNoun()
    {
        var text = IndexModel.BuildSummaryText(2, NoteType.ToDo, showCompleted: false, folderLabel: null, scheduleLabel: null);
        Assert.Equal("2 to-do notes", text);
    }

    [Fact]
    public void FolderFilter_AppendsFolderClause()
    {
        var text = IndexModel.BuildSummaryText(4, noteType: null, showCompleted: false, folderLabel: "folder \"Work\"", scheduleLabel: null);
        Assert.Equal("4 notes in folder \"Work\"", text);
    }

    [Fact]
    public void NoFolderFilter_AppendsNoFolderClause()
    {
        var text = IndexModel.BuildSummaryText(4, noteType: null, showCompleted: false, folderLabel: "no folder", scheduleLabel: null);
        Assert.Equal("4 notes in no folder", text);
    }

    [Fact]
    public void FolderAndScheduleFilters_JoinedWithAnd()
    {
        var text = IndexModel.BuildSummaryText(2, noteType: null, showCompleted: false, folderLabel: "folder \"Work\"", scheduleLabel: "schedule \"Jobb\"");
        Assert.Equal("2 notes in folder \"Work\" and schedule \"Jobb\"", text);
    }

    [Fact]
    public void AllFiltersTogether_ComposeInExpectedOrder()
    {
        var text = IndexModel.BuildSummaryText(1, NoteType.WorkShift, showCompleted: true, folderLabel: "folder \"Work\"", scheduleLabel: "schedule \"Jobb\"");
        Assert.Equal("1 completed/past work shift note in folder \"Work\" and schedule \"Jobb\"", text);
    }
}
