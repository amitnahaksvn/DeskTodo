using System.Collections.ObjectModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Phase 21's Agenda View — a scrollable list of incomplete tasks across the next <see cref="LookaheadDays"/> days (including today, including any overdue ones), grouped by date with a friendly label ("Today"/"Tomorrow"/day name). Distinct from the grid's flat, unbounded, every-day list (Phase 20) — this is scoped to "what's actually coming up."</summary>
public sealed partial class AgendaViewModel : ViewModelBase
{
    private const int LookaheadDays = 14;

    private readonly ITaskService _taskService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgendaViewModel> _logger;

    public AgendaViewModel(ITaskService taskService, TimeProvider timeProvider, ILogger<AgendaViewModel> logger)
    {
        _taskService = taskService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ObservableCollection<AgendaGroupViewModel> Groups { get; } = [];

    public bool HasNoUpcomingTasks => Groups.Count == 0;

    public event EventHandler<DateOnly>? DateSelected;

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = Today();
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);

            var upcoming = tasks
                .Where(t => !t.IsArchived && !t.IsCompleted && (t.PlanDate <= today.AddDays(LookaheadDays)))
                .OrderBy(t => t.PlanDate)
                .ThenBy(t => t.DayOrder)
                .ToList();

            Groups.Clear();
            foreach (var group in upcoming.GroupBy(t => t.PlanDate).OrderBy(g => g.Key))
            {
                var rows = group.Select(t => new PlannerTaskRowViewModel(t, RaiseDateSelected)).ToList();
                Groups.Add(new AgendaGroupViewModel(group.Key, BuildDateLabel(group.Key, today), rows));
            }

            OnPropertyChanged(nameof(HasNoUpcomingTasks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the agenda view");
        }
    }

    private static string BuildDateLabel(DateOnly date, DateOnly today)
    {
        if (date < today)
        {
            return $"Overdue — {date:ddd, MMM d}";
        }

        if (date == today)
        {
            return "Today";
        }

        if (date == today.AddDays(1))
        {
            return "Tomorrow";
        }

        return date.ToString("dddd, MMM d");
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);
}
