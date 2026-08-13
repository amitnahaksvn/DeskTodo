using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 28's Clipboard History — a running list of recent clipboard text, fed by
/// <c>WidgetWindow</c>'s poll timer (see its doc comment for why the actual
/// <c>IClipboard</c> polling lives there, not here — this ViewModel has no Avalonia
/// dependency of its own, matching <c>WidgetViewModel</c>'s own convention). Registered as a
/// DI singleton, not transient — the same "app-wide state that must persist whether or not
/// its window is open" reasoning as <see cref="FocusTimerViewModel"/>, so history keeps
/// accumulating in the background and isn't lost every time the Clipboard History window is
/// closed and reopened.
///
/// Deliberately in-memory only, never persisted to disk: clipboard content can include
/// passwords and other sensitive text someone copied briefly and never meant to keep —
/// writing it into a SQLite file that outlives the running app is a real privacy cost this
/// feature doesn't need to take on to be useful. History resets on every app restart.
/// </summary>
public sealed partial class ClipboardHistoryViewModel : ViewModelBase
{
    // Longer clips are still just skipped, not truncated-and-kept — this feature is "what
    // did I recently copy," not a general scratch-pad/file store, and a single accidental
    // "select all, copy" in some other app shouldn't bloat an otherwise-small in-memory list.
    private const int MaxEntryLength = 5000;
    private const int MaxEntries = 20;

    public ObservableCollection<string> Entries { get; } = [];

    /// <summary>
    /// Called by <c>WidgetWindow</c>'s poll timer with whatever <c>IClipboard.TryGetTextAsync</c>
    /// just returned. No-ops for null/blank/unchanged-since-last-seen/too-long text, so the
    /// same clipboard content doesn't pile up as duplicate entries across repeated polls
    /// while nothing new has actually been copied.
    /// </summary>
    public void AddEntry(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxEntryLength)
        {
            return;
        }

        if (Entries.Count > 0 && Entries[0] == text)
        {
            return;
        }

        Entries.Insert(0, text);
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }

    /// <summary>Raised when the user picks an entry to copy back onto the OS clipboard — <c>ClipboardHistoryWindow</c> handles the actual <c>IClipboard.SetTextAsync</c> call, since that's a platform capability this ViewModel deliberately doesn't reach for directly.</summary>
    public event EventHandler<string>? CopyBackRequested;

    [RelayCommand]
    private void CopyBack(string text) => CopyBackRequested?.Invoke(this, text);

    [RelayCommand]
    private void ClearHistory() => Entries.Clear();
}
