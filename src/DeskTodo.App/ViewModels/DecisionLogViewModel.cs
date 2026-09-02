using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Backs the Decision Log window (Feature 57, Roadmap-39-100.md) — searchable, independent of ordinary tasks.</summary>
public sealed partial class DecisionLogViewModel(IDecisionService decisionService, ILogger<DecisionLogViewModel> logger) : ViewModelBase
{
    private IReadOnlyList<DecisionOption> _allDecisions = [];

    public ObservableCollection<DecisionOption> Decisions { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewContext { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewDecisionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewAlternatives { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewReason { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var decisions = await decisionService.GetDecisionsAsync(cancellationToken);
            _allDecisions = decisions.Select(d => new DecisionOption(
                d.Id, d.Title, d.Context, d.DecisionText, d.Alternatives, d.Reason,
                d.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt"))).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load the Decision Log");
        }
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        Decisions.Clear();
        foreach (var decision in _allDecisions.Where(d =>
                     string.IsNullOrEmpty(search) ||
                     d.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     d.DecisionText.Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            Decisions.Add(decision);
        }
    }

    [RelayCommand]
    private async Task RecordAsync()
    {
        var title = NewTitle.Trim();
        var decisionText = NewDecisionText.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(decisionText))
        {
            return;
        }

        try
        {
            await decisionService.RecordDecisionAsync(title, NewContext, decisionText, NewAlternatives, NewReason, projectId: null);
            NewTitle = string.Empty;
            NewContext = string.Empty;
            NewDecisionText = string.Empty;
            NewAlternatives = string.Empty;
            NewReason = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record a decision");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid decisionId)
    {
        try
        {
            await decisionService.DeleteDecisionAsync(decisionId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete decision {DecisionId}", decisionId);
        }
    }
}
