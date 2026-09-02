using System.Collections.ObjectModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="ActivityEntry"/> as shown in <see cref="ActivityTimelineViewModel"/>'s feed.</summary>
public sealed record ActivityTimelineEntryOption(string Category, string Description, string TimestampDisplay);

/// <summary>Backs the Activity Timeline window (Feature 61, Roadmap-39-100.md) — read-only, like Task History.</summary>
public sealed partial class ActivityTimelineViewModel(IActivityTimelineService activityTimelineService, ILogger<ActivityTimelineViewModel> logger) : ViewModelBase
{
    public ObservableCollection<ActivityTimelineEntryOption> Entries { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await activityTimelineService.GetRecentActivityAsync(cancellationToken: cancellationToken);
            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(new ActivityTimelineEntryOption(entry.Category, entry.Description, entry.Timestamp.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load the Activity Timeline");
        }
    }
}
