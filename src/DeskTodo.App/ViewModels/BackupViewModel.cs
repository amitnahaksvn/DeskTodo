using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Backup Manager window (Features 67 and 68, Roadmap-39-100.md) — create/list/delete
/// local backups, and preview what restoring one would change before committing to it.
/// </summary>
public sealed partial class BackupViewModel(IBackupService backupService, ILogger<BackupViewModel> logger) : ViewModelBase
{
    public ObservableCollection<BackupOption> Backups { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Set by <see cref="SimulateRestoreAsync"/> — the view shows this before the user confirms an actual restore.</summary>
    [ObservableProperty]
    public partial string? SimulationSummary { get; set; }

    [ObservableProperty]
    public partial BackupOption? SelectedBackup { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var backups = await backupService.GetBackupsAsync(cancellationToken);
            Backups.Clear();
            foreach (var backup in backups)
            {
                Backups.Add(BackupOption.FromInfo(backup));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load backups");
            StatusMessage = "Failed to load backups.";
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        try
        {
            await backupService.CreateBackupAsync();
            StatusMessage = "Backup created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create backup");
            StatusMessage = "Failed to create backup.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBackupAsync(BackupOption? backup)
    {
        if (backup is null)
        {
            return;
        }

        try
        {
            await backupService.DeleteBackupAsync(backup.FilePath);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete backup {FilePath}", backup.FilePath);
            StatusMessage = "Failed to delete backup.";
        }
    }

    [RelayCommand]
    private async Task SimulateRestoreAsync(BackupOption? backup)
    {
        if (backup is null)
        {
            return;
        }

        try
        {
            SelectedBackup = backup;
            var result = await backupService.SimulateRestoreAsync(backup.FilePath);
            SimulationSummary =
                $"{result.TotalTasksInBackup} task(s) in this backup — {result.TasksToAdd} would be added, " +
                $"{result.TasksToUpdate} would be updated, {result.TasksToRemove} would be removed." +
                (result.SampleChanges.Count > 0 ? "\n" + string.Join("\n", result.SampleChanges) : string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to simulate restore for {FilePath}", backup.FilePath);
            SimulationSummary = "Failed to preview this backup.";
        }
    }

    [RelayCommand]
    private async Task ConfirmRestoreAsync()
    {
        if (SelectedBackup is not { } backup)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await backupService.RestoreAsync(backup.FilePath);
            StatusMessage = "Restore complete. Restart DeskTodo to see the restored data.";
            SimulationSummary = null;
            SelectedBackup = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore backup {FilePath}", backup.FilePath);
            StatusMessage = "Failed to restore backup.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelRestorePreview()
    {
        SelectedBackup = null;
        SimulationSummary = null;
    }
}
