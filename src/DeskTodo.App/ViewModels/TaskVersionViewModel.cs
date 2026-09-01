using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Task Versions window (Feature 44, Roadmap-39-100.md) — a single task's snapshot
/// history, most recent first, with a "Restore" action per row.
/// </summary>
public sealed partial class TaskVersionViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskVersionViewModel> _logger;
    private Guid _taskId;

    public TaskVersionViewModel(ITaskService taskService, ILogger<TaskVersionViewModel> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [ObservableProperty]
    public partial string TaskTitle { get; set; } = string.Empty;

    /// <summary>Raised after a successful restore, so the window hosting this (and the caller who opened it) knows to reload the task's fields.</summary>
    public event EventHandler? Restored;

    public ObservableCollection<TaskVersionOption> Versions { get; } = [];

    public async Task LoadAsync(Guid taskId, string taskTitle, CancellationToken cancellationToken = default)
    {
        _taskId = taskId;
        TaskTitle = taskTitle;
        await RefreshAsync(cancellationToken);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var versions = await _taskService.GetTaskVersionsAsync(_taskId, cancellationToken);
            Versions.Clear();
            foreach (var version in versions)
            {
                Versions.Add(TaskVersionOption.FromEntity(version));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load versions for task {TaskId}", _taskId);
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(TaskVersionOption? version)
    {
        if (version is null)
        {
            return;
        }

        try
        {
            await _taskService.RestoreTaskVersionAsync(_taskId, version.Id);
            await RefreshAsync(CancellationToken.None);
            Restored?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore version {VersionId} for task {TaskId}", version.Id, _taskId);
        }
    }
}
