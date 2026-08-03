using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Eisenhower Matrix — a 2x2 grid derived purely from existing fields, exactly
/// as the phase's own approach note suggested: no new persistence. "Important" is
/// <see cref="TaskPriority.High"/>/<see cref="TaskPriority.Critical"/>; "Urgent" is overdue
/// or due within <see cref="UrgentWithinDays"/> days (a task with no due date is never
/// urgent — there's nothing pressing about it by definition). Shows every non-archived,
/// incomplete task across every day.
/// </summary>
public sealed partial class MatrixViewModel : ViewModelBase
{
    private const int UrgentWithinDays = 2;

    private readonly ITaskService _taskService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MatrixViewModel> _logger;

    public MatrixViewModel(ITaskService taskService, TimeProvider timeProvider, ILogger<MatrixViewModel> logger)
    {
        _taskService = taskService;
        _timeProvider = timeProvider;
        _logger = logger;

        UrgentImportant = new MatrixQuadrantViewModel("Urgent & Important", "Do first");
        NotUrgentImportant = new MatrixQuadrantViewModel("Important, Not Urgent", "Schedule");
        UrgentNotImportant = new MatrixQuadrantViewModel("Urgent, Not Important", "Delegate");
        NotUrgentNotImportant = new MatrixQuadrantViewModel("Neither", "Eliminate / someday");
    }

    public MatrixQuadrantViewModel UrgentImportant { get; }

    public MatrixQuadrantViewModel NotUrgentImportant { get; }

    public MatrixQuadrantViewModel UrgentNotImportant { get; }

    public MatrixQuadrantViewModel NotUrgentNotImportant { get; }

    public event EventHandler<DateOnly>? DateSelected;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);

            UrgentImportant.Tasks.Clear();
            NotUrgentImportant.Tasks.Clear();
            UrgentNotImportant.Tasks.Clear();
            NotUrgentNotImportant.Tasks.Clear();

            foreach (var task in tasks.Where(t => !t.IsArchived && !t.IsCompleted).OrderBy(t => t.DueDate ?? DateTime.MaxValue))
            {
                var important = task.Priority is TaskPriority.High or TaskPriority.Critical;
                var urgent = IsUrgent(task, today);
                var row = new PlannerTaskRowViewModel(task, RaiseDateSelected);

                var quadrant = (urgent, important) switch
                {
                    (true, true) => UrgentImportant,
                    (false, true) => NotUrgentImportant,
                    (true, false) => UrgentNotImportant,
                    (false, false) => NotUrgentNotImportant,
                };
                quadrant.Tasks.Add(row);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the Eisenhower matrix");
        }
    }

    private static bool IsUrgent(TaskItem task, DateOnly today)
    {
        if (task.DueDate is not { } due)
        {
            return false;
        }

        var dueDate = DateOnly.FromDateTime(due);
        return dueDate <= today.AddDays(UrgentWithinDays);
    }

    private void RaiseDateSelected(DateOnly date) => DateSelected?.Invoke(this, date);
}
