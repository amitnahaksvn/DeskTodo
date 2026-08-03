using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Habit-goal use cases: create/archive/delete a goal, and mark/unmark it done for a given day.</summary>
public interface IGoalService
{
    Task<IReadOnlyList<Goal>> GetGoalsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<Goal> CreateGoalAsync(string name, string? description = null, CancellationToken cancellationToken = default);

    Task ArchiveGoalAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task UnarchiveGoalAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Marks the goal done for <paramref name="date"/> — idempotent (see <see cref="Abstractions.IGoalRepository.AddCompletionAsync"/>).</summary>
    Task MarkCompletedAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Undoes a completion — how a mis-click gets corrected.</summary>
    Task UnmarkCompletedAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default);
}
