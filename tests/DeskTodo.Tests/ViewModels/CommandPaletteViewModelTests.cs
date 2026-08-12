using CommunityToolkit.Mvvm.Input;
using DeskTodo.App.ViewModels;

namespace DeskTodo.Tests.ViewModels;

public class CommandPaletteViewModelTests
{
    private readonly CommandPaletteViewModel _sut = new();

    private static CommandPaletteEntry MakeEntry(string label, out bool[] executed)
    {
        var flag = new bool[1];
        var entry = new CommandPaletteEntry(label, new RelayCommand(() => flag[0] = true));
        executed = flag;
        return entry;
    }

    [Fact]
    public void SetEntries_PopulatesVisibleEntries_AndSelectsTheFirst()
    {
        var gridEntry = MakeEntry("Open Grid View", out _);
        var settingsEntry = MakeEntry("Open Settings", out _);

        _sut.SetEntries([gridEntry, settingsEntry]);

        Assert.Equal(2, _sut.VisibleEntries.Count);
        Assert.Equal(gridEntry, _sut.SelectedEntry);
    }

    [Fact]
    public void SearchText_FiltersEntriesByLabel_CaseInsensitively()
    {
        var gridEntry = MakeEntry("Open Grid View", out _);
        var settingsEntry = MakeEntry("Open Settings", out _);
        _sut.SetEntries([gridEntry, settingsEntry]);

        _sut.SearchText = "grid";

        Assert.Single(_sut.VisibleEntries);
        Assert.Equal(gridEntry, _sut.VisibleEntries[0]);
    }

    [Fact]
    public void SearchText_WithNoMatches_LeavesVisibleEntriesEmpty_AndSelectedEntryNull()
    {
        _sut.SetEntries([MakeEntry("Open Grid View", out _)]);

        _sut.SearchText = "nonexistent";

        Assert.Empty(_sut.VisibleEntries);
        Assert.Null(_sut.SelectedEntry);
    }

    [Fact]
    public void ExecuteSelectedCommand_RunsTheSelectedEntrysCommand_AndRaisesCloseRequested()
    {
        var entry = MakeEntry("Open Settings", out var executed);
        _sut.SetEntries([entry]);
        var closed = false;
        _sut.CloseRequested += (_, _) => closed = true;

        _sut.ExecuteSelectedCommand.Execute(null);

        Assert.True(executed[0]);
        Assert.True(closed);
    }

    [Fact]
    public void ExecuteSelectedCommand_WithNoSelection_FallsBackToTheFirstVisibleEntry()
    {
        var entry = MakeEntry("Open Grid View", out var executed);
        _sut.SetEntries([entry]);
        _sut.SelectedEntry = null;

        _sut.ExecuteSelectedCommand.Execute(null);

        Assert.True(executed[0]);
    }

    [Fact]
    public void ExecuteSelectedCommand_WithNoEntries_DoesNotThrow_AndDoesNotRaiseCloseRequested()
    {
        var closed = false;
        _sut.CloseRequested += (_, _) => closed = true;

        _sut.ExecuteSelectedCommand.Execute(null);

        Assert.False(closed);
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequested_WithoutExecutingAnything()
    {
        var entry = MakeEntry("Open Settings", out var executed);
        _sut.SetEntries([entry]);
        var closed = false;
        _sut.CloseRequested += (_, _) => closed = true;

        _sut.CancelCommand.Execute(null);

        Assert.True(closed);
        Assert.False(executed[0]);
    }
}
