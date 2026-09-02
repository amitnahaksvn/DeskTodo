using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Distraction Log window (Feature 64, Roadmap-39-100.md) — logs interruptions as
/// "this just happened and took N minutes," rather than a running start/stop timer (this app's
/// existing Focus Timer already owns the "is a session currently running" concept; a second
/// independent stopwatch here would be a confusing second source of truth for the same idea).
/// </summary>
public sealed partial class DistractionLogViewModel(IDistractionRepository distractionRepository, TimeProvider timeProvider, ILogger<DistractionLogViewModel> logger) : ViewModelBase
{
    public IReadOnlyList<DistractionCategory> Categories { get; } = Enum.GetValues<DistractionCategory>();

    public ObservableCollection<DistractionOption> Distractions { get; } = [];

    [ObservableProperty]
    public partial DistractionCategory SelectedCategory { get; set; } = DistractionCategory.Phone;

    [ObservableProperty]
    public partial int DurationMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SummaryDisplay { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var distractions = await distractionRepository.GetAllAsync(cancellationToken);
            Distractions.Clear();
            foreach (var distraction in distractions)
            {
                Distractions.Add(new DistractionOption(
                    distraction.Category.ToString(),
                    distraction.DurationMinutes ?? 0,
                    distraction.Notes,
                    distraction.StartedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")));
            }

            SummaryDisplay = BuildSummary(distractions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load the Distraction Log");
        }
    }

    /// <summary>The spec's own analytics list: count, total time, most common category, average duration.</summary>
    private static string BuildSummary(IReadOnlyList<Distraction> distractions)
    {
        if (distractions.Count == 0)
        {
            return "No interruptions logged yet";
        }

        var totalMinutes = distractions.Sum(d => d.DurationMinutes ?? 0);
        var averageMinutes = totalMinutes / (double)distractions.Count;
        var mostCommon = distractions
            .GroupBy(d => d.Category)
            .OrderByDescending(g => g.Count())
            .First().Key;

        return $"{distractions.Count} interruption(s), {totalMinutes}m total, avg {averageMinutes:0.#}m, most common: {mostCommon}";
    }

    [RelayCommand]
    private async Task LogDistractionAsync()
    {
        var duration = Math.Max(1, DurationMinutes);
        var endedAt = timeProvider.GetUtcNow().UtcDateTime;
        var distraction = new Distraction
        {
            StartedAt = endedAt.AddMinutes(-duration),
            Category = SelectedCategory,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        };
        distraction.End(endedAt);

        try
        {
            await distractionRepository.AddAsync(distraction);
            DurationMinutes = 5;
            Notes = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log a distraction");
        }
    }
}
