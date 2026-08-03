using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>A row in <see cref="GoalsViewModel"/>'s list. Owns its own <see cref="ToggleTodayCommand"/>/<see cref="DeleteCommand"/> via constructor-injected services (same pattern as <see cref="SubtaskRowViewModel"/>/<see cref="KanbanCardViewModel"/>) — <see cref="Goal.GetCurrentStreak"/> is computed once here at row-construction time from the loaded <see cref="Goal.Completions"/>, not re-evaluated live, since the row is rebuilt on every reload anyway.</summary>
public sealed class GoalRowViewModel
{
    public GoalRowViewModel(Goal goal, DateOnly today, IGoalService goalService, ILogger logger, Action requestRefresh)
    {
        Id = goal.Id;
        Name = goal.Name;
        CurrentStreak = goal.GetCurrentStreak(today);
        TotalCompletions = goal.Completions.Count;
        IsCompletedToday = goal.Completions.Any(c => c.CompletedDate == today);

        ToggleTodayCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                if (IsCompletedToday)
                {
                    await goalService.UnmarkCompletedAsync(Id, today);
                }
                else
                {
                    await goalService.MarkCompletedAsync(Id, today);
                }

                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to toggle today's completion for goal {GoalId}", Id);
            }
        });

        DeleteCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await goalService.DeleteGoalAsync(Id);
                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete goal {GoalId}", Id);
            }
        });
    }

    public Guid Id { get; }

    public string Name { get; }

    public int CurrentStreak { get; }

    public int TotalCompletions { get; }

    public bool IsCompletedToday { get; }

    public string ToggleButtonLabel => IsCompletedToday ? "Done today ✓" : "Mark done today";

    public IAsyncRelayCommand ToggleTodayCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
