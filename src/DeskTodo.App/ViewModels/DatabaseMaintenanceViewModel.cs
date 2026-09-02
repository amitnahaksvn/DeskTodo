using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Backs the Database Maintenance Center window (Feature 69, Roadmap-39-100.md).</summary>
public sealed partial class DatabaseMaintenanceViewModel(IDatabaseMaintenanceService databaseMaintenanceService, ILogger<DatabaseMaintenanceViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    public partial string DatabaseSizeDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TaskCount { get; set; }

    [ObservableProperty]
    public partial int ProjectCount { get; set; }

    [ObservableProperty]
    public partial int TagCount { get; set; }

    [ObservableProperty]
    public partial int HistoryRecordCount { get; set; }

    [ObservableProperty]
    public partial int VersionCount { get; set; }

    [ObservableProperty]
    public partial int AttachmentCount { get; set; }

    [ObservableProperty]
    public partial string MigrationVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await databaseMaintenanceService.GetStatsAsync(cancellationToken);
            DatabaseSizeDisplay = FormatSize(stats.DatabaseSizeBytes);
            TaskCount = stats.TaskCount;
            ProjectCount = stats.ProjectCount;
            TagCount = stats.TagCount;
            HistoryRecordCount = stats.HistoryRecordCount;
            VersionCount = stats.VersionCount;
            AttachmentCount = stats.AttachmentCount;
            MigrationVersion = stats.MigrationVersion;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load database stats");
            StatusMessage = "Failed to load database stats.";
        }
    }

    [RelayCommand]
    private async Task VacuumAsync()
    {
        IsBusy = true;
        try
        {
            await databaseMaintenanceService.VacuumAsync();
            StatusMessage = "Vacuum complete.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Vacuum failed");
            StatusMessage = "Vacuum failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RebuildIndexesAsync()
    {
        IsBusy = true;
        try
        {
            await databaseMaintenanceService.RebuildIndexesAsync();
            StatusMessage = "Indexes rebuilt.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rebuild indexes failed");
            StatusMessage = "Rebuild indexes failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatSize(long bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB",
        };
}
