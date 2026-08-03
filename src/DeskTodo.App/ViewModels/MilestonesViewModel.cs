using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 21's Sprint Planner and Roadmap View — both served by this one chronologically-
/// ordered milestone list (near-term milestones at the top read as "what's coming up
/// next"/Sprint Planner; scrolling down reads as the full timeline/Roadmap View), rather
/// than two separate screens over what's fundamentally the same underlying data shape. See
/// docs/ARCHITECTURE.md's "Phase 21" section for the full reasoning.
/// </summary>
public sealed partial class MilestonesViewModel : ViewModelBase
{
    private readonly IMilestoneService _milestoneService;
    private readonly ILogger<MilestonesViewModel> _logger;

    public MilestonesViewModel(IMilestoneService milestoneService, ILogger<MilestonesViewModel> logger)
    {
        _milestoneService = milestoneService;
        _logger = logger;
    }

    public ObservableCollection<MilestoneRowViewModel> Milestones { get; } = [];

    public bool HasNoMilestones => Milestones.Count == 0;

    [ObservableProperty]
    public partial string NewMilestoneTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? NewMilestoneTargetDate { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var milestones = await _milestoneService.GetMilestonesAsync(cancellationToken);

            Milestones.Clear();
            foreach (var milestone in milestones)
            {
                Milestones.Add(new MilestoneRowViewModel(milestone, _milestoneService, _logger, () => _ = LoadAsync(cancellationToken)));
            }

            OnPropertyChanged(nameof(HasNoMilestones));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load milestones");
        }
    }

    /// <summary>Blank title is a no-op — matches every other "add" row's convention in this app.</summary>
    [RelayCommand]
    private async Task AddMilestoneAsync()
    {
        var title = NewMilestoneTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            var targetDate = NewMilestoneTargetDate.HasValue ? DateOnly.FromDateTime(NewMilestoneTargetDate.Value.DateTime) : (DateOnly?)null;
            await _milestoneService.CreateMilestoneAsync(title, null, targetDate);
            NewMilestoneTitle = string.Empty;
            NewMilestoneTargetDate = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add milestone '{Title}'", title);
        }
    }
}
