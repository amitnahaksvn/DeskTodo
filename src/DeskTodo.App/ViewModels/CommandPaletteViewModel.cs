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
/// <remarks>
/// Feature 40 (Roadmap-39-100.md) describes a fuller, registry-based version of this same
/// palette (a central <c>IAppCommand</c> registry each module registers into, with categories,
/// shortcut display, and "disabled — here's why" explanations) — this closes part of that gap
/// (fuzzy search, recent commands) without the registry rewrite: <c>WidgetWindow</c> still
/// builds the entry list directly from <c>WidgetViewModel</c>'s own commands, which has worked
/// fine as this list has grown past 40 entries. The registry/category/CanExecute-explanation
/// pieces are deliberately not built in this pass.
/// </remarks>
public sealed partial class CommandPaletteViewModel : ViewModelBase
{
    private const int MaxRecentCommands = 5;

    private readonly List<CommandPaletteEntry> _allEntries = [];

    /// <summary>Session-only — the last few executed entries, most recent first. Resets on restart, the same "not worth persisting" reasoning <c>WidgetViewModel.RecentlyViewed</c> already documents.</summary>
    private readonly List<CommandPaletteEntry> _recentEntries = [];

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

        // Empty search: recent commands first (if any), then everything else in its given
        // order — recent entries aren't duplicated into the "everything else" tail.
        if (string.IsNullOrEmpty(search))
        {
            var recentLabels = _recentEntries.Select(e => e.Label).ToHashSet();
            ReplaceVisibleEntries(_recentEntries.Concat(_allEntries.Where(e => !recentLabels.Contains(e.Label))));
            return;
        }

        // Substring matches rank above fuzzy (subsequence) matches, since a literal
        // substring hit is almost always what the user meant.
        var substringMatches = _allEntries.Where(e => e.Label.Contains(search, StringComparison.OrdinalIgnoreCase));
        var fuzzyMatches = _allEntries.Except(substringMatches).Where(e => IsFuzzyMatch(e.Label, search));
        ReplaceVisibleEntries(substringMatches.Concat(fuzzyMatches));
    }

    private void ReplaceVisibleEntries(IEnumerable<CommandPaletteEntry> entries)
    {
        VisibleEntries.Clear();
        foreach (var entry in entries)
        {
            VisibleEntries.Add(entry);
        }

        SelectedEntry = VisibleEntries.FirstOrDefault();
    }

    /// <summary>True when every character of <paramref name="query"/> appears in <paramref name="label"/>, in order (not necessarily contiguous) — the same subsequence-matching approach VS Code/Sublime's "fuzzy" command palettes use.</summary>
    private static bool IsFuzzyMatch(string label, string query)
    {
        var queryIndex = 0;
        foreach (var c in label)
        {
            if (queryIndex < query.Length && char.ToLowerInvariant(c) == char.ToLowerInvariant(query[queryIndex]))
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
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

        _recentEntries.RemoveAll(e => e.Label == entry.Label);
        _recentEntries.Insert(0, entry);
        while (_recentEntries.Count > MaxRecentCommands)
        {
            _recentEntries.RemoveAt(_recentEntries.Count - 1);
        }

        entry.Command.Execute(null);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
