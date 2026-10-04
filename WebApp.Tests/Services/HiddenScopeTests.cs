using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class HiddenScopeTests
{
    private static ToDoNote Note(int? folderId = null, int? scheduleId = null) =>
        new() { UserId = "owner", FolderId = folderId, ScheduleId = scheduleId };

    [Fact]
    public void NothingHidden_HidesNothing()
    {
        var scope = new HiddenScope([], [], []);
        Assert.False(scope.Hides(Note(folderId: 1, scheduleId: 2)));
    }

    [Fact]
    public void HiddenFolderOrSchedule_OnTheOwnersPlacement_Hides()
    {
        var scope = new HiddenScope([1], [2], []);
        Assert.True(scope.Hides(Note(folderId: 1)));
        Assert.True(scope.Hides(Note(scheduleId: 2)));
    }

    [Fact]
    public void ScheduleLinkedThroughTheFolder_Hides()
    {
        var scope = new HiddenScope([], [2], []);
        var note = Note(folderId: 5);
        note.Folder = new Folder { Id = 5, UserId = "owner", Name = "F", ScheduleId = 2 };
        Assert.True(scope.Hides(note));
    }

    [Fact]
    public void HiddenFolder_OnTheViewersOwnOverlay_Hides()
    {
        var scope = new HiddenScope([9], [], []);
        var share = new NoteShare { SharedWithUserId = "viewer", FolderId = 9 };
        Assert.True(scope.Hides(Note(), share));
    }

    [Fact]
    public void HiddenNoteType_Hides()
    {
        var scope = new HiddenScope([], [], [NoteType.Fasting]);
        Assert.True(scope.Hides(new FastingNote { UserId = "owner", Day = new DateOnly(2026, 1, 1) }));
        Assert.False(scope.Hides(Note()));
    }
}
