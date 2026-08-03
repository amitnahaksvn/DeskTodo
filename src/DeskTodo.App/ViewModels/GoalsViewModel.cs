using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Goal Planner — personal, ongoing habit-style targets tracked by a daily
/// streak, distinct from <see cref="MilestonesViewModel"/> (the project-management flavor
/// with a target date that tasks link to). See docs/ARCHITECTURE.md's "Phase 21" section
/// for why these are two separate concepts rather than one.
/// </summary>
public sealed partial class GoalsViewModel : ViewModelBase
{
    private readonly IGoalService _goalService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GoalsViewModel> _logger;

    public GoalsViewModel(IGoalService goalService, TimeProvider timeProvider, ILogger<GoalsViewModel> logger)
    {
        _goalService = goalService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ObservableCollection<GoalRowViewModel> Goals { get; } = [];

    public bool HasNoGoals => Goals.Count == 0;

    [ObservableProperty]
    public partial string NewGoalName { get; set; } = string.Empty;

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var goals = await _goalService.GetGoalsAsync(cancellationToken: cancellationToken);
            var today = Today();

            Goals.Clear();
            foreach (var goal in goals)
            {
                Goals.Add(new GoalRowViewModel(goal, today, _goalService, _logger, () => _ = LoadAsync(cancellationToken)));
            }

            OnPropertyChanged(nameof(HasNoGoals));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load goals");
        }
    }

    /// <summary>Blank name is a no-op — matches every other "add" row's convention in this app.</summary>
    [RelayCommand]
    private async Task AddGoalAsync()
    {
        var name = NewGoalName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            await _goalService.CreateGoalAsync(name);
            NewGoalName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add goal '{Name}'", name);
        }
    }
}
