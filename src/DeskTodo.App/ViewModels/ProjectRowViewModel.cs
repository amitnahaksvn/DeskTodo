using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>A row in <see cref="ProjectsViewModel"/>'s list — owns its own <see cref="ToggleArchivedCommand"/>/<see cref="DeleteCommand"/>, same pattern as <see cref="MilestoneRowViewModel"/>. Task progress ("X/Y done") is computed once from the loaded <see cref="Project.Tasks"/> collection.</summary>
public sealed class ProjectRowViewModel
{
    public ProjectRowViewModel(Project project, IProjectService projectService, ILogger logger, Action requestRefresh)
    {
        Id = project.Id;
        Name = project.Name;
        ColorHex = project.ColorHex;
        IsArchived = project.IsArchived;
        TotalTaskCount = project.Tasks.Count;
        CompletedTaskCount = project.Tasks.Count(t => t.IsCompleted);

        ToggleArchivedCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await projectService.SetArchivedAsync(Id, !IsArchived);
                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to toggle archived state for project {ProjectId}", Id);
            }
        });

        DeleteCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await projectService.DeleteProjectAsync(Id);
                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete project {ProjectId}", Id);
            }
        });
    }

    public Guid Id { get; }

    public string Name { get; }

    public string ColorHex { get; }

    public bool IsArchived { get; }

    public int TotalTaskCount { get; }

    public int CompletedTaskCount { get; }

    public string ProgressDisplay => TotalTaskCount == 0 ? "No linked tasks" : $"{CompletedTaskCount}/{TotalTaskCount} tasks done";

    public string ToggleButtonLabel => IsArchived ? "Unarchive" : "Archive";

    public IAsyncRelayCommand ToggleArchivedCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
