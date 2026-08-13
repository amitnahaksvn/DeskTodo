using DeskTodo.App.ViewModels;

namespace DeskTodo.Tests.ViewModels;

public class ClipboardHistoryViewModelTests
{
    private readonly ClipboardHistoryViewModel _sut = new();

    [Fact]
    public void AddEntry_PrependsNewText()
    {
        _sut.AddEntry("first");
        _sut.AddEntry("second");

        Assert.Equal(["second", "first"], _sut.Entries);
    }

    [Fact]
    public void AddEntry_WithNullOrWhitespace_IsIgnored()
    {
        _sut.AddEntry(null);
        _sut.AddEntry("   ");
        _sut.AddEntry(string.Empty);

        Assert.Empty(_sut.Entries);
    }

    [Fact]
    public void AddEntry_RepeatingTheMostRecentText_DoesNotDuplicateIt()
    {
        _sut.AddEntry("hello");
        _sut.AddEntry("hello");
        _sut.AddEntry("hello");

        Assert.Single(_sut.Entries);
    }

    [Fact]
    public void AddEntry_WithTextLongerThanTheLimit_IsIgnored()
    {
        _sut.AddEntry(new string('a', 5001));

        Assert.Empty(_sut.Entries);
    }

    [Fact]
    public void AddEntry_CapsHistoryAtTwentyEntries()
    {
        for (var i = 0; i < 25; i++)
        {
            _sut.AddEntry($"entry-{i}");
        }

        Assert.Equal(20, _sut.Entries.Count);
        Assert.Equal("entry-24", _sut.Entries[0]);
        Assert.Equal("entry-5", _sut.Entries[^1]);
    }

    [Fact]
    public void CopyBackCommand_RaisesCopyBackRequested_WithTheGivenText()
    {
        string? requested = null;
        _sut.CopyBackRequested += (_, text) => requested = text;

        _sut.CopyBackCommand.Execute("some text");

        Assert.Equal("some text", requested);
    }

    [Fact]
    public void ClearHistoryCommand_EmptiesTheList()
    {
        _sut.AddEntry("one");
        _sut.AddEntry("two");

        _sut.ClearHistoryCommand.Execute(null);

        Assert.Empty(_sut.Entries);
    }
}
