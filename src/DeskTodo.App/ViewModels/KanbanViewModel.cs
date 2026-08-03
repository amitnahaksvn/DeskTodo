using System.Collections.ObjectModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Kanban Board — two columns, To Do and Done, reusing <c>TaskItem.IsCompleted</c>
/// directly rather than introducing a new persisted "status" concept (a three-plus-column
/// board — To Do/In Progress/Done — was the phase's original wishlist shape, but nothing in
/// the domain model distinguishes "not started" from "in progress," and inventing that
/// distinction is a real, separate scoping decision the phase's own notes flag rather than
/// something to bolt on silently here). Shows every non-archived task across every day, the
/// same "all tasks, not just today's" scope as the grid (Phase 20).
/// </summary>
public sealed partial class KanbanViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<KanbanViewModel> _logger;

    public KanbanViewModel(ITaskService taskService, ILogger<KanbanViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    public ObservableCollection<KanbanCardViewModel> ToDoCards { get; } = [];

    public ObservableCollection<KanbanCardViewModel> DoneCards { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);

            ToDoCards.Clear();
            DoneCards.Clear();

            foreach (var task in tasks.Where(t => !t.IsArchived).OrderBy(t => t.PlanDate).ThenBy(t => t.DayOrder))
            {
                var card = new KanbanCardViewModel(task, _taskService, _logger, () => _ = LoadAsync(cancellationToken));
                (task.IsCompleted ? DoneCards : ToDoCards).Add(card);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the kanban board");
        }
    }
}
