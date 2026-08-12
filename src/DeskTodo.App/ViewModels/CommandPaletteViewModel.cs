using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 28's Command Palette — a searchable list of every action <c>WidgetWindow</c>
/// already exposes via a header icon or the "Add a task…"/search bar (open Grid/Calendar/
/// Planner/Focus Timer/Analytics/Settings, toggle search, jump to today), summoned via
/// Cmd/Ctrl+K. Deliberately has no dependency on <c>WidgetViewModel</c> or any service —
/// <c>WidgetWindow</c> hands over the list of entries (see <see cref="SetEntries"/>) built
/// from its own live <c>WidgetViewModel</c> instance's commands, the same "give the item
/// what it needs directly" pattern <see cref="TaskGridRowViewModel"/> uses for its
/// <c>Categories</c> list.
/// </summary>
public sealed partial class CommandPaletteViewModel : ViewModelBase
{
    private readonly List<CommandPaletteEntry> _allEntries = [];

    public ObservableCollection<CommandPaletteEntry> VisibleEntries { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CommandPaletteEntry? SelectedEntry { get; set; }

    partial void OnSearchTextChanged(string value) => RefreshVisibleEntries();

    public void SetEntries(IEnumerable<CommandPaletteEntry> entries)
    {
        _allEntries.Clear();
        _allEntries.AddRange(entries);
        RefreshVisibleEntries();
    }

    private void RefreshVisibleEntries()
    {
        var search = SearchText.Trim();
        var matches = string.IsNullOrEmpty(search)
            ? _allEntries
            : _allEntries.Where(e => e.Label.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        VisibleEntries.Clear();
        foreach (var entry in matches)
        {
            VisibleEntries.Add(entry);
        }

        SelectedEntry = VisibleEntries.FirstOrDefault();
    }

    /// <summary>Raised once an entry has run (or the palette was cancelled) — the view closes itself in response, matching every other "Requested"/"Saved" hand-off in this app.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Bound to the search box's Enter key and to double-clicking a row — runs <see cref="SelectedEntry"/> if one is set, else the first visible entry.</summary>
    [RelayCommand]
    private void ExecuteSelected()
    {
        var entry = SelectedEntry ?? VisibleEntries.FirstOrDefault();
        if (entry is null)
        {
            return;
        }

        entry.Command.Execute(null);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
