using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Year Planner — a 12-tile grid, one per month, each showing a task-count
/// summary rather than a full mini-calendar (12 simultaneous 7x6 grids would be either
/// illegibly tiny or need a much taller window than this app's other dialogs; a summary
/// tile answers "how busy was/is this month" at a glance, which is what a year-level view
/// is actually for — day-level detail is what the Month/Week tabs are for).
/// </summary>
public sealed partial class YearViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<YearViewModel> _logger;

    public YearViewModel(ITaskService taskService, TimeProvider timeProvider, ILogger<YearViewModel> logger)
    {
        _taskService = taskService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [ObservableProperty]
    public partial int Year { get; set; }

    public ObservableCollection<YearMonthSummaryViewModel> Months { get; } = [];

    public event EventHandler<DateOnly>? DateSelected;

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task LoadAsync(DateOnly? initialDate = null, CancellationToken cancellationToken = default)
    {
        Year = (initialDate ?? Today()).Year;
        await RefreshMonthsAsync(cancellationToken);
    }

    private async Task RefreshMonthsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);
            var countsByYearMonth = tasks
                .Where(t => !t.IsArchived && t.PlanDate.Year == Year)
                .GroupBy(t => t.PlanDate.Month)
                .ToDictionary(g => g.Key, g => (Total: g.Count(), Completed: g.Count(t => t.IsCompleted)));

            Months.Clear();
            var today = Today();

            for (var month = 1; month <= 12; month++)
            {
                var firstOfMonth = new DateOnly(Year, month, 1);
                var (total, completed) = countsByYearMonth.TryGetValue(month, out var counts) ? counts : (0, 0);
                Months.Add(new YearMonthSummaryViewModel(firstOfMonth, firstOfMonth.Year == today.Year && firstOfMonth.Month == today.Month, total, completed, RaiseDateSelected));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load year tasks for {Year}", Year);
        }
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);

    [RelayCommand]
    private Task PreviousYearAsync() => NavigateToYearAsync(Year - 1);

    [RelayCommand]
    private Task NextYearAsync() => NavigateToYearAsync(Year + 1);

    [RelayCommand]
    private Task GoToCurrentYearAsync() => NavigateToYearAsync(Today().Year);

    private async Task NavigateToYearAsync(int newYear)
    {
        Year = newYear;
        await RefreshMonthsAsync();
    }
}
