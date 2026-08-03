using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Phase 21's Weekly Planner — seven <see cref="CalendarDayViewModel"/> cells (the same cell type <see cref="CalendarViewModel"/>'s month grid uses) for the currently displayed week, each Sunday-to-Saturday. A read-only view over the same task data the grid/calendar already read — no new persistence.</summary>
public sealed partial class WeekViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WeekViewModel> _logger;

    public WeekViewModel(ITaskService taskService, TimeProvider timeProvider, ILogger<WeekViewModel> logger)
    {
        _taskService = taskService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeekTitle))]
    public partial DateOnly WeekStart { get; set; }

    public string WeekTitle
    {
        get
        {
            var end = WeekStart.AddDays(6);
            return WeekStart.Month == end.Month
                ? $"{WeekStart:MMMM d} – {end:d, yyyy}"
                : $"{WeekStart:MMM d} – {end:MMM d, yyyy}";
        }
    }

    public ObservableCollection<CalendarDayViewModel> Days { get; } = [];

    public event EventHandler<DateOnly>? DateSelected;

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task LoadAsync(DateOnly? initialDate = null, CancellationToken cancellationToken = default)
    {
        var target = initialDate ?? Today();
        WeekStart = target.AddDays(-(int)target.DayOfWeek);
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
            var today = Today();

            for (var i = 0; i < 7; i++)
            {
                var date = WeekStart.AddDays(i);
                var (total, completed) = countsByDate.TryGetValue(date, out var counts) ? counts : (0, 0);
                Days.Add(new CalendarDayViewModel(date, isCurrentMonth: true, date == today, total, completed, RaiseDateSelected));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load week tasks for week starting {WeekStart}", WeekStart);
        }
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);

    [RelayCommand]
    private Task PreviousWeekAsync() => NavigateToWeekAsync(WeekStart.AddDays(-7));

    [RelayCommand]
    private Task NextWeekAsync() => NavigateToWeekAsync(WeekStart.AddDays(7));

    [RelayCommand]
    private Task GoToCurrentWeekAsync() => NavigateToWeekAsync(Today().AddDays(-(int)Today().DayOfWeek));

    private async Task NavigateToWeekAsync(DateOnly newWeekStart)
    {
        WeekStart = newWeekStart;
        await RefreshDaysAsync();
    }
}
