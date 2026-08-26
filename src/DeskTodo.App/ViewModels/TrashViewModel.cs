using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Trash window (Roadmap-39-100.md's Feature 46) — every soft-deleted task
/// (<see cref="Domain.Entities.TaskItem.IsDeleted"/>, set by the existing
/// <see cref="ITaskService.DeleteTaskAsync"/> this app has had since Phase 8), with Restore
/// and Delete Permanently per row, plus Empty Trash. Deliberately does not implement
/// time-based auto-purge retention (7/30/90 days) — this pass is manual-only; see this
/// feature's own notes in IMPLEMENTATION.md for why that's a documented scope cut, not an
/// oversight.
/// </summary>
public sealed partial class TrashViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TrashViewModel> _logger;

    public TrashViewModel(ITaskService taskService, ILogger<TrashViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    public ObservableCollection<TrashedTaskOption> DeletedTasks { get; } = [];

    /// <summary>Raised after a restore actually happens — <c>WidgetWindow</c> reloads its own task list in response, the same hand-off every other cross-window mutation in this app uses.</summary>
    public event EventHandler? TaskRestored;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _taskService.GetDeletedTasksAsync(cancellationToken);
            DeletedTasks.Clear();
            foreach (var task in deleted)
            {
                var deletedAtDisplay = task.DeletedAt is { } deletedAt
                    ? deletedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")
                    : string.Empty;
                DeletedTasks.Add(new TrashedTaskOption(task.Id, task.Title, deletedAtDisplay));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load deleted tasks for the Trash window");
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(Guid taskId)
    {
        try
        {
            await _taskService.RestoreTaskAsync(taskId);
            await LoadAsync();
            TaskRestored?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore task {TaskId} from Trash", taskId);
        }
    }

    /// <summary>
    /// The actual permanent delete — confirmation happens in <c>TrashWindow</c>'s code-behind
    /// (via <c>ConfirmDialogWindow</c>) before this ever runs, the same "ViewModel doesn't own
    /// a Window to show a dialog from" split every other destructive action in this app uses
    /// (see <c>WidgetWindow.OnDeleteTaskClick</c>).
    /// </summary>
    [RelayCommand]
    private async Task DeleteForeverAsync(Guid taskId)
    {
        try
        {
            await _taskService.PermanentlyDeleteTaskAsync(taskId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete task {TaskId}", taskId);
        }
    }

    [RelayCommand]
    private async Task EmptyTrashAsync()
    {
        try
        {
            await _taskService.EmptyTrashAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to empty the Trash");
        }
    }
}
