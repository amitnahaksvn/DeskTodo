using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A single cell in <see cref="CalendarViewModel"/>'s month grid. Owns its own
/// <see cref="SelectCommand"/> via a constructor-injected callback — the same "give the
/// item what it needs directly" pattern <see cref="SubtaskRowViewModel"/>/<see cref="BlockerChip"/>/
/// <see cref="RecentTaskOption"/> already use — rather than the cell's XAML reaching for an
/// ambient <c>$parent[ItemsControl]</c> binding to invoke a command on the parent ViewModel.
/// </summary>
public sealed class CalendarDayViewModel
{
    public CalendarDayViewModel(DateOnly date, bool isCurrentMonth, bool isToday, int totalCount, int completedCount, Action<DateOnly> requestSelect)
    {
        Date = date;
        DayNumber = date.Day;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
        TotalCount = totalCount;
        CompletedCount = completedCount;
        SelectCommand = new RelayCommand(() => requestSelect(date));
    }

    public DateOnly Date { get; }

    public int DayNumber { get; }

    /// <summary>False for the leading/trailing days of adjacent months that pad the grid to a full 6-week rectangle — shown dimmed, not omitted, so the grid stays a stable 7x6 shape every month.</summary>
    public bool IsCurrentMonth { get; }

    public bool IsToday { get; }

    public int TotalCount { get; }

    public int CompletedCount { get; }

    public bool HasTasks => TotalCount > 0;

    /// <summary>"—" for a day with no tasks, "checked/total" otherwise — mirrors <see cref="TaskGridRowViewModel.ProgressDisplay"/>'s same shape.</summary>
    public string CountDisplay => TotalCount == 0 ? "—" : $"{CompletedCount}/{TotalCount}";

    public IRelayCommand SelectCommand { get; }
}
