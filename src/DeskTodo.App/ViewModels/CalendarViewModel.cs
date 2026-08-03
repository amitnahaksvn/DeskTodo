using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the month-grid calendar view — Phase 21's Calendar View, and (since a month grid
/// already shows a full month's shape) the Monthly Planner deliverable too, rather than a
/// separate near-duplicate screen. The rest of Phase 21's non-speculative views (Week,
/// Year, Agenda, Timeline, Kanban, Matrix) live in <see cref="PlannerViewModel"/>. A
/// read-only *view* over the same <see cref="Domain.Entities.TaskItem"/> data the grid
/// already reads (<see cref="ITaskService.GetAllTasksAsync"/>) — no new persistence, no new
/// entity.
/// </summary>
public sealed partial class CalendarViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CalendarViewModel> _logger;

    public CalendarViewModel(ITaskService taskService, TimeProvider timeProvider, ILogger<CalendarViewModel> logger)
    {
        _taskService = taskService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    public partial DateOnly DisplayedMonth { get; set; }

    public string MonthTitle => DisplayedMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy");

    /// <summary>Always exactly 42 cells (a fixed 7x6 grid, laid out via a 7-column <c>UniformGrid</c> in the view) — leading/trailing days from adjacent months pad it out (see <see cref="CalendarDayViewModel.IsCurrentMonth"/>) so the grid never reflows height between months with 4 vs. 6 visible weeks.</summary>
    public ObservableCollection<CalendarDayViewModel> Days { get; } = [];

    /// <summary>Raised when a day cell is clicked — <c>WidgetWindow</c> handles this by navigating the widget's own <c>SelectedDate</c> to it and closing this window, the same "View owns the dialog hand-off" pattern as <see cref="WidgetViewModel.GridViewRequested"/>.</summary>
    public event EventHandler<DateOnly>? DateSelected;

    public event EventHandler? CloseRequested;

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task LoadAsync(DateOnly? initialMonth = null, CancellationToken cancellationToken = default)
    {
        var target = initialMonth ?? Today();
        DisplayedMonth = new DateOnly(target.Year, target.Month, 1);
        await RefreshDaysAsync(cancellationToken);
    }

    private async Task RefreshDaysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);
            var countsByDate = tasks
                .Where(t => !t.IsArchived)
                .GroupBy(t => t.PlanDate)
                .ToDictionary(g => g.Key, g => (Total: g.Count(), Completed: g.Count(t => t.IsCompleted)));

            Days.Clear();

            var firstOfMonth = DisplayedMonth;
            var firstCell = firstOfMonth.AddDays(-(int)firstOfMonth.DayOfWeek);
            var today = Today();

            for (var i = 0; i < 42; i++)
            {
                var date = firstCell.AddDays(i);
                var (total, completed) = countsByDate.TryGetValue(date, out var counts) ? counts : (0, 0);
                Days.Add(new CalendarDayViewModel(date, date.Month == firstOfMonth.Month, date == today, total, completed, RaiseDateSelected));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load calendar tasks for {Month}", DisplayedMonth);
        }
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);

    [RelayCommand]
    private Task PreviousMonthAsync() => NavigateToMonthAsync(DisplayedMonth.AddMonths(-1));

    [RelayCommand]
    private Task NextMonthAsync() => NavigateToMonthAsync(DisplayedMonth.AddMonths(1));

    [RelayCommand]
    private Task GoToCurrentMonthAsync() => NavigateToMonthAsync(new DateOnly(Today().Year, Today().Month, 1));

    private async Task NavigateToMonthAsync(DateOnly newMonth)
    {
        DisplayedMonth = newMonth;
        await RefreshDaysAsync();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
