using WebApp.Models;
using WebApp.Pages.Notes;

namespace WebApp.Tests.Pages.Notes;

public class FastingCalendarTests
{
    [Fact]
    public void Checked_NoExistingEntry_Creates()
    {
        var action = FastingCalendarModel.DecidePaintAction(isChecked: true, FastingLevel.Meat, existingLevel: null);
        Assert.Equal(FastingCalendarModel.PaintAction.Create, action);
    }

    [Fact]
    public void Checked_ExistingEntrySameLevel_IsNoOp()
    {
        var action = FastingCalendarModel.DecidePaintAction(isChecked: true, FastingLevel.Meat, existingLevel: FastingLevel.Meat);
        Assert.Equal(FastingCalendarModel.PaintAction.NoOp, action);
    }

    [Fact]
    public void Checked_ExistingEntryDifferentLevel_Updates()
    {
        var action = FastingCalendarModel.DecidePaintAction(isChecked: true, FastingLevel.Meat, existingLevel: FastingLevel.MeatDairyEggs);
        Assert.Equal(FastingCalendarModel.PaintAction.Update, action);
    }

    [Fact]
    public void Unchecked_ExistingEntrySameLevel_Deletes()
    {
        // This is the "paint off" case: unchecking a day that currently has
        // the level you're painting removes it entirely.
        var action = FastingCalendarModel.DecidePaintAction(isChecked: false, FastingLevel.Meat, existingLevel: FastingLevel.Meat);
        Assert.Equal(FastingCalendarModel.PaintAction.Delete, action);
    }

    [Fact]
    public void Unchecked_ExistingEntryDifferentLevel_IsLeftAlone()
    {
        // A day painted with a different level must never be touched just
        // because it wasn't checked while painting some other level.
        var action = FastingCalendarModel.DecidePaintAction(isChecked: false, FastingLevel.Meat, existingLevel: FastingLevel.MeatDairyEggs);
        Assert.Equal(FastingCalendarModel.PaintAction.NoOp, action);
    }

    [Fact]
    public void Unchecked_NoExistingEntry_IsNoOp()
    {
        var action = FastingCalendarModel.DecidePaintAction(isChecked: false, FastingLevel.Meat, existingLevel: null);
        Assert.Equal(FastingCalendarModel.PaintAction.NoOp, action);
    }
}
