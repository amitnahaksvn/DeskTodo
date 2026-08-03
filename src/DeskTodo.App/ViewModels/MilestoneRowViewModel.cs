using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>A row in <see cref="MilestonesViewModel"/>'s list — owns its own <see cref="ToggleCompleteCommand"/>/<see cref="DeleteCommand"/>, same pattern as <see cref="GoalRowViewModel"/>. Task progress ("X/Y done") is computed once from the loaded <see cref="Milestone.Tasks"/> collection, mirroring <see cref="TaskGridRowViewModel.ProgressDisplay"/>'s same shape.</summary>
public sealed class MilestoneRowViewModel
{
    public MilestoneRowViewModel(Milestone milestone, IMilestoneService milestoneService, ILogger logger, Action requestRefresh)
    {
        Id = milestone.Id;
        Title = milestone.Title;
        TargetDate = milestone.TargetDate;
        IsCompleted = milestone.IsCompleted;
        TotalTaskCount = milestone.Tasks.Count;
        CompletedTaskCount = milestone.Tasks.Count(t => t.IsCompleted);

        ToggleCompleteCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await milestoneService.SetCompletedAsync(Id, !IsCompleted);
                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to toggle completion for milestone {MilestoneId}", Id);
            }
        });

        DeleteCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await milestoneService.DeleteMilestoneAsync(Id);
                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete milestone {MilestoneId}", Id);
            }
        });
    }

    public Guid Id { get; }

    public string Title { get; }

    public DateOnly? TargetDate { get; }

    public bool IsCompleted { get; }

    public int TotalTaskCount { get; }

    public int CompletedTaskCount { get; }

    public string TargetDateDisplay => TargetDate?.ToString("MMM d, yyyy") ?? "No target date";

    public string ProgressDisplay => TotalTaskCount == 0 ? "No linked tasks" : $"{CompletedTaskCount}/{TotalTaskCount} tasks done";

    public string ToggleButtonLabel => IsCompleted ? "Reopen" : "Mark complete";

    public IAsyncRelayCommand ToggleCompleteCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
