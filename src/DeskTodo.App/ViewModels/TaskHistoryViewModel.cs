using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Task History window (Roadmap-39-100.md's Feature 42) — a single task's audit
/// timeline, most recent entry first. Read-only: unlike Trash, there is nothing here to
/// act on, only to look at.
/// </summary>
public sealed partial class TaskHistoryViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskHistoryViewModel> _logger;

    public TaskHistoryViewModel(ITaskService taskService, ILogger<TaskHistoryViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [ObservableProperty]
    public partial string TaskTitle { get; set; } = string.Empty;

    public ObservableCollection<TaskHistoryEntryOption> Entries { get; } = [];

    public async Task LoadAsync(Guid taskId, string taskTitle, CancellationToken cancellationToken = default)
    {
        TaskTitle = taskTitle;

        try
        {
            var history = await _taskService.GetTaskHistoryAsync(taskId, cancellationToken);
            Entries.Clear();
            foreach (var entry in history)
            {
                Entries.Add(TaskHistoryEntryOption.FromEntity(entry));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history for task {TaskId}", taskId);
        }
    }
}
