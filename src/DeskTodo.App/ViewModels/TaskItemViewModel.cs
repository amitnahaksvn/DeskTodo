using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Display/interaction wrapper around a single <see cref="TaskItem"/> for
/// the widget's task list. Owns persisting its own completion toggle so the
/// view can bind to it directly without the parent <see cref="WidgetViewModel"/>
/// needing a command per row.
/// </summary>
public sealed partial class TaskItemViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskItemViewModel> _logger;

    public TaskItemViewModel(TaskItem task, ITaskService taskService, ILogger<TaskItemViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;

        Id = task.Id;
        DisplayNumber = task.DayOrder + 1;
        Title = task.Title;
        PriorityColorHex = GetPriorityColorHex(task.Priority);
        CategoryColorHex = task.Category?.ColorHex;
        IsCompleted = task.IsCompleted;
    }

    public Guid Id { get; }

    public int DisplayNumber { get; }

    public string Title { get; }

    public string PriorityColorHex { get; }

    public string? CategoryColorHex { get; }

    // Display-only: setting this never triggers persistence (see ToggleCompleteAsync
    // below). Using an On<Property>Changed hook for that instead would also fire from
    // this very constructor's assignment above, since CommunityToolkit.Mvvm's generated
    // setter invokes it unconditionally — silently re-persisting each task's own
    // just-loaded state back to the database on every load.
    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    /// <summary>Bound to the row's CheckBox <c>Command</c> (not <c>IsChecked</c> two-way) so only a genuine user click persists anything.</summary>
    [RelayCommand]
    private async Task ToggleCompleteAsync()
    {
        var newValue = !IsCompleted;

        try
        {
            if (newValue)
            {
                await _taskService.CompleteTaskAsync(Id);
            }
            else
            {
                await _taskService.ReopenTaskAsync(Id);
            }

            IsCompleted = newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle completion for task {TaskId}", Id);
        }
    }

    private static string GetPriorityColorHex(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "#94A3B8",
        TaskPriority.Medium => "#3B82F6",
        TaskPriority.High => "#F97316",
        TaskPriority.Critical => "#EF4444",
        _ => "#94A3B8",
    };
}
