using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Archive Vault window (Feature 45, Roadmap-39-100.md) — every archived task
/// (<see cref="Domain.Entities.TaskItem.IsArchived"/>, a per-task action since Phase 8/25) and
/// archived project (<see cref="Domain.Entities.Project.IsArchived"/>, Phase 25), in one place
/// with search and restore. Unlike Trash, archived items were never headed for deletion — this
/// is "keep permanently, out of normal views," so there is no Delete Forever/Empty here.
/// </summary>
public sealed partial class ArchiveViewModel(ITaskService taskService, IProjectService projectService, ILogger<ArchiveViewModel> logger) : ViewModelBase
{
    private IReadOnlyList<Domain.Entities.TaskItem> _allArchivedTasks = [];
    private IReadOnlyList<Domain.Entities.Project> _allArchivedProjects = [];

    public ObservableCollection<ArchivedTaskOption> Tasks { get; } = [];

    public ObservableCollection<ArchivedProjectOption> Projects { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>Raised after a restore — <c>WidgetWindow</c> reloads its own task list in response, the same hand-off Trash's restore uses.</summary>
    public event EventHandler? ItemRestored;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _allArchivedTasks = await taskService.GetArchivedTasksAsync(cancellationToken);
            var projects = await projectService.GetProjectsAsync(cancellationToken);
            _allArchivedProjects = projects.Where(p => p.IsArchived).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load the Archive Vault");
        }
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();

        Tasks.Clear();
        foreach (var task in _allArchivedTasks.Where(t => string.IsNullOrEmpty(search) || t.Title.Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            Tasks.Add(new ArchivedTaskOption(task.Id, task.Title));
        }

        Projects.Clear();
        foreach (var project in _allArchivedProjects.Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            Projects.Add(new ArchivedProjectOption(project.Id, project.Name));
        }
    }

    [RelayCommand]
    private async Task RestoreTaskAsync(Guid taskId)
    {
        try
        {
            await taskService.RestoreTaskAsync(taskId);
            await LoadAsync();
            ItemRestored?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore archived task {TaskId}", taskId);
        }
    }

    [RelayCommand]
    private async Task RestoreProjectAsync(Guid projectId)
    {
        try
        {
            await projectService.SetArchivedAsync(projectId, isArchived: false);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore archived project {ProjectId}", projectId);
        }
    }
}
