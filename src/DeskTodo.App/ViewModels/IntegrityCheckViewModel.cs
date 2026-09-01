using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Backs the Data Integrity Check window (Feature 70, Roadmap-39-100.md).</summary>
public sealed partial class IntegrityCheckViewModel(IDataIntegrityService dataIntegrityService, ILogger<IntegrityCheckViewModel> logger) : ViewModelBase
{
    public ObservableCollection<IntegrityIssue> Issues { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasChecked { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public bool HasAutoRepairableIssues => Issues.Any(i => i.IsAutoRepairable);

    [RelayCommand]
    private async Task RunCheckAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var issues = await dataIntegrityService.CheckAsync();
            Issues.Clear();
            foreach (var issue in issues)
            {
                Issues.Add(issue);
            }

            HasChecked = true;
            OnPropertyChanged(nameof(HasAutoRepairableIssues));
            StatusMessage = issues.Count == 0 ? "No issues found." : $"Found {issues.Count} issue(s).";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data integrity check failed");
            StatusMessage = "Check failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RepairAllAsync()
    {
        IsBusy = true;
        try
        {
            var fixedCount = await dataIntegrityService.RepairAsync(Issues.ToList());
            StatusMessage = $"Fixed {fixedCount} issue(s).";
            await RunCheckAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data integrity repair failed");
            StatusMessage = "Repair failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
