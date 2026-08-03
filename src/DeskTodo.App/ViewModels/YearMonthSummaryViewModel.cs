using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>One tile in <see cref="YearViewModel"/>'s 12-month grid — a summary, not a mini-calendar (see <see cref="YearViewModel"/>'s doc comment for why). Clicking a tile jumps the widget to that month's 1st, the same "click to navigate" interaction every planner tab uses.</summary>
public sealed class YearMonthSummaryViewModel
{
    public YearMonthSummaryViewModel(DateOnly firstOfMonth, bool isCurrentMonth, int totalCount, int completedCount, Action<DateOnly> requestSelect)
    {
        FirstOfMonth = firstOfMonth;
        MonthName = firstOfMonth.ToString("MMMM");
        IsCurrentMonth = isCurrentMonth;
        TotalCount = totalCount;
        CompletedCount = completedCount;
        SelectCommand = new RelayCommand(() => requestSelect(firstOfMonth));
    }

    public DateOnly FirstOfMonth { get; }

    public string MonthName { get; }

    public bool IsCurrentMonth { get; }

    public int TotalCount { get; }

    public int CompletedCount { get; }

    public bool HasTasks => TotalCount > 0;

    public string CountDisplay => TotalCount == 0 ? "No tasks" : $"{CompletedCount}/{TotalCount} done";

    public IRelayCommand SelectCommand { get; }
}
