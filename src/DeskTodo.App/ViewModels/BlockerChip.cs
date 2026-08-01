using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A task blocking the task being edited, as shown/removed in the editor's "Blocked by"
/// chip list. Owns its own removal (mirrors <see cref="TagChip"/>/<see cref="ChecklistItemRowViewModel"/>).
/// </summary>
public sealed partial class BlockerChip : ObservableObject
{
    private readonly ITaskDependencyService _dependencyService;
    private readonly ILogger _logger;
    private readonly Action<BlockerChip> _requestRemove;

    public BlockerChip(Guid dependencyId, Guid blockingTaskId, string blockingTaskTitle, bool blockingTaskIsCompleted, ITaskDependencyService dependencyService, ILogger logger, Action<BlockerChip> requestRemove)
    {
        DependencyId = dependencyId;
        BlockingTaskId = blockingTaskId;
        Title = blockingTaskTitle;
        IsComplete = blockingTaskIsCompleted;
        _dependencyService = dependencyService;
        _logger = logger;
        _requestRemove = requestRemove;
    }

    public Guid DependencyId { get; }

    public Guid BlockingTaskId { get; }

    public string Title { get; }

    /// <summary>Whether the blocking task is already done — once true, this chip no longer contributes to the task being blocked.</summary>
    public bool IsComplete { get; }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        try
        {
            await _dependencyService.RemoveBlockerAsync(DependencyId);
            _requestRemove(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove blocker dependency {DependencyId}", DependencyId);
        }
    }
}
