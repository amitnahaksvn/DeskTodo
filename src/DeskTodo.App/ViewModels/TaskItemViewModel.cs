using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Display/interaction wrapper around a single <see cref="TaskItem"/> for
/// the widget's task list. Owns persisting its own state changes (complete,
/// rename, pin, archive, delete, duplicate) so the view can bind to it
/// directly without <see cref="WidgetViewModel"/> needing a command per row.
/// Delete/duplicate/archive change *which* tasks belong on today's list, so
/// they call back into the constructor's <c>requestListRefresh</c> callback
/// rather than trying to surgically patch the parent's collection themselves.
/// </summary>
public sealed partial class TaskItemViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskItemViewModel> _logger;
    private readonly Action _requestListRefresh;

    public TaskItemViewModel(TaskItem task, ITaskService taskService, ILogger<TaskItemViewModel> logger, Action requestListRefresh)
    {
        _taskService = taskService;
        _logger = logger;
        _requestListRefresh = requestListRefresh;

        Id = task.Id;
        DisplayNumber = task.DayOrder + 1;
        PriorityColorHex = GetPriorityColorHex(task.Priority);
        CategoryColorHex = task.Category?.ColorHex;
        Title = task.Title;
        IsCompleted = task.IsCompleted;
        IsPinned = task.IsPinned;
    }

    public Guid Id { get; }

    public int DisplayNumber { get; }

    public string PriorityColorHex { get; }

    public string? CategoryColorHex { get; }

    // These display-only properties never trigger persistence from their setters — an
    // On<Property>Changed hook would also fire from this class's own constructor
    // assignments above, silently re-persisting each task's own just-loaded state on
    // every load. Every mutation instead goes through an explicit [RelayCommand] below.
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string EditingTitle { get; set; } = string.Empty;

    /// <summary>Bound to the row's CheckBox <c>Command</c> (not <c>IsChecked</c> two-way) so only a genuine user click persists anything.</summary>
    [RelayCommand]
    private async Task ToggleCompleteAsync()
    {
        var newValue = !IsCompleted;

        if (await TryAsync(() => newValue ? _taskService.CompleteTaskAsync(Id) : _taskService.ReopenTaskAsync(Id), "toggle completion for"))
        {
            IsCompleted = newValue;
        }
    }

    /// <summary>Enters inline title-edit mode (bound to the title's double-tap gesture).</summary>
    [RelayCommand]
    private void BeginEdit()
    {
        EditingTitle = Title;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    /// <summary>Confirms the inline edit (Enter key). A blank title cancels instead of saving.</summary>
    [RelayCommand]
    private async Task CommitEditAsync()
    {
        var newTitle = EditingTitle.Trim();

        if (string.IsNullOrEmpty(newTitle) || newTitle == Title)
        {
            IsEditing = false;
            return;
        }

        if (await TryAsync(() => _taskService.RenameTaskAsync(Id, newTitle), "rename"))
        {
            Title = newTitle;
        }

        IsEditing = false;
    }

    [RelayCommand]
    private async Task TogglePinAsync()
    {
        var newValue = !IsPinned;

        if (await TryAsync(() => newValue ? _taskService.PinTaskAsync(Id) : _taskService.UnpinTaskAsync(Id), "toggle pin for"))
        {
            IsPinned = newValue;
        }
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (await TryAsync(() => _taskService.ArchiveTaskAsync(Id), "archive"))
        {
            _requestListRefresh();
        }
    }

    [RelayCommand]
    private async Task DuplicateAsync()
    {
        if (await TryAsync(() => _taskService.DuplicateTaskAsync(Id), "duplicate"))
        {
            _requestListRefresh();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (await TryAsync(() => _taskService.DeleteTaskAsync(Id), "delete"))
        {
            _requestListRefresh();
        }
    }

    private async Task<bool> TryAsync(Func<Task> action, string verbPhrase)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {VerbPhrase} task {TaskId}", verbPhrase, Id);
            return false;
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
