using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 25's Projects tab, hosted alongside Goals/Milestones in <c>PlannerWindow</c> —
/// same "compose a small independent sub-ViewModel into the existing tabbed window" reasoning
/// <see cref="PlannerViewModel"/>'s doc comment already lays out, rather than a new header
/// icon/window. New projects cycle through <see cref="ColorPalette"/> rather than exposing a
/// color-picker control (none exists elsewhere in this app to reuse) — matches the app's
/// existing built-in category colors in spirit without adding new UI surface.
/// </summary>
public sealed partial class ProjectsViewModel : ViewModelBase
{
    private static readonly string[] ColorPalette =
    [
        "#6366F1", "#0EA5E9", "#8B5CF6", "#22C55E", "#F59E0B", "#10B981", "#EC4899", "#EF4444",
    ];

    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsViewModel> _logger;

    public ProjectsViewModel(IProjectService projectService, ILogger<ProjectsViewModel> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public ObservableCollection<ProjectRowViewModel> Projects { get; } = [];

    public bool HasNoProjects => Projects.Count == 0;

    [ObservableProperty]
    public partial string NewProjectName { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectService.GetProjectsAsync(cancellationToken);

            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(new ProjectRowViewModel(project, _projectService, _logger, () => _ = LoadAsync(cancellationToken)));
            }

            OnPropertyChanged(nameof(HasNoProjects));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load projects");
        }
    }

    /// <summary>Blank name is a no-op — matches every other "add" row's convention in this app.</summary>
    [RelayCommand]
    private async Task AddProjectAsync()
    {
        var name = NewProjectName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            var colorHex = ColorPalette[Projects.Count % ColorPalette.Length];
            await _projectService.CreateProjectAsync(name, null, colorHex);
            NewProjectName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add project '{Name}'", name);
        }
    }
}
