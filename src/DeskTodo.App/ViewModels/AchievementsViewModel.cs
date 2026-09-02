using System.Collections.ObjectModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Backs the Achievements window (Feature 62, Roadmap-39-100.md) — read-only, like Task History.</summary>
public sealed partial class AchievementsViewModel(IAchievementService achievementService, ILogger<AchievementsViewModel> logger) : ViewModelBase
{
    public ObservableCollection<Achievement> Achievements { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var achievements = await achievementService.GetAchievementsAsync(cancellationToken);
            Achievements.Clear();
            foreach (var achievement in achievements)
            {
                Achievements.Add(achievement);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load achievements");
        }
    }
}
