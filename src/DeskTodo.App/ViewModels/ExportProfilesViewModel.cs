using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Export Profiles window (Feature 91, Roadmap-39-100.md) — save a named
/// Format/Project/Date-Range configuration and re-run it later without re-picking every option.
/// </summary>
public sealed partial class ExportProfilesViewModel(
    IExportProfileService profileService,
    IProjectRepository projectRepository,
    ILogger<ExportProfilesViewModel> logger) : ViewModelBase
{
    public ObservableCollection<ExportProfileRow> Profiles { get; } = [];

    public ObservableCollection<ProjectOption> ProjectOptions { get; } = [];

    public IReadOnlyList<ExportFormat> FormatOptions { get; } = Enum.GetValues<ExportFormat>();

    public IReadOnlyList<ExportDateRange> DateRangeOptions { get; } = Enum.GetValues<ExportDateRange>();

    [ObservableProperty]
    public partial string NewProfileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ExportFormat NewProfileFormat { get; set; } = ExportFormat.Csv;

    [ObservableProperty]
    public partial ProjectOption NewProfileProject { get; set; } = AllProjectsOption;

    [ObservableProperty]
    public partial ExportDateRange NewProfileDateRange { get; set; } = ExportDateRange.All;

    [ObservableProperty]
    public partial ExportProfileRow? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    private static readonly ProjectOption AllProjectsOption = new(null, "All Projects");

    /// <summary>The file extension (no dot) matching <see cref="SelectedProfile"/>'s format — the View uses this to suggest a filename/filter for the save dialog.</summary>
    public string SelectedProfileExtension => SelectedProfile?.Format switch
    {
        ExportFormat.Csv => "csv",
        ExportFormat.Json => "json",
        ExportFormat.Markdown => "md",
        ExportFormat.Excel => "xlsx",
        _ => "txt",
    };

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await projectRepository.GetAllAsync(cancellationToken);
            ProjectOptions.Clear();
            ProjectOptions.Add(AllProjectsOption);
            foreach (var project in projects)
            {
                ProjectOptions.Add(new ProjectOption(project.Id, project.Name));
            }

            var projectNameById = projects.ToDictionary(p => p.Id, p => p.Name);
            var profiles = await profileService.GetProfilesAsync(cancellationToken);
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                var projectName = profile.ProjectId is { } id ? projectNameById.GetValueOrDefault(id, "(deleted project)") : "All Projects";
                Profiles.Add(new ExportProfileRow(profile.Id, profile.Name, profile.Format, projectName, profile.DateRange));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load export profiles");
            ErrorMessage = "Couldn't load export profiles.";
        }
    }

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        ErrorMessage = string.Empty;
        var name = NewProfileName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Enter a name for the profile.";
            return;
        }

        try
        {
            await profileService.CreateProfileAsync(name, NewProfileFormat, NewProfileProject.Id, NewProfileDateRange);
            NewProfileName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create export profile '{Name}'", name);
            ErrorMessage = "Couldn't save the profile.";
        }
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(Guid profileId)
    {
        try
        {
            await profileService.DeleteProfileAsync(profileId);
            if (SelectedProfile?.Id == profileId)
            {
                SelectedProfile = null;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete export profile {ProfileId}", profileId);
        }
    }

    /// <summary>Runs <see cref="SelectedProfile"/> against a destination the View already opened (after picking a save location for <see cref="SelectedProfileExtension"/>) — same "View picks the file, ViewModel takes a Stream" split as <c>ImportExportViewModel</c>.</summary>
    public async Task RunSelectedProfileAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (SelectedProfile is not { } profile)
        {
            ErrorMessage = "Pick a profile to run.";
            return;
        }

        try
        {
            var count = await profileService.RunProfileAsync(profile.Id, destination, cancellationToken);
            StatusMessage = count == 1 ? $"Exported 1 task using \"{profile.Name}\"." : $"Exported {count} tasks using \"{profile.Name}\".";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run export profile {ProfileId}", profile.Id);
            ErrorMessage = "Couldn't run that profile.";
        }
    }
}
