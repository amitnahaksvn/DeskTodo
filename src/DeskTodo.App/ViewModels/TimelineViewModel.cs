using System.Collections.ObjectModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Timeline View — every incomplete task with a due date, in chronological
/// order, one row per task with its due date/time as a label. Deliberately a plain ordered
/// list rather than a proportionally-spaced, drawn timeline (tasks positioned along an axis
/// scaled to actual elapsed time between them) — that's a materially bigger UI engineering
/// effort (custom <c>DrawingContext</c>/<c>Canvas</c> layout, axis scaling, overlap
/// handling for same-day tasks) for a view whose actual job — "what's due, in order" — a
/// plain list already delivers. A true scaled timeline is still open if a future pass
/// specifically wants the visual version.
/// </summary>
public sealed partial class TimelineViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TimelineViewModel> _logger;

    public TimelineViewModel(ITaskService taskService, ILogger<TimelineViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    public ObservableCollection<PlannerTaskRowViewModel> Tasks { get; } = [];

    public bool HasNoTasksWithDueDates => Tasks.Count == 0;

    public event EventHandler<DateOnly>? DateSelected;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);
            var withDueDates = tasks
                .Where(t => !t.IsArchived && !t.IsCompleted && t.DueDate.HasValue)
                .OrderBy(t => t.DueDate);

            Tasks.Clear();
            foreach (var task in withDueDates)
            {
                Tasks.Add(new PlannerTaskRowViewModel(task, RaiseDateSelected));
            }

            OnPropertyChanged(nameof(HasNoTasksWithDueDates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the timeline view");
        }
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);
}
