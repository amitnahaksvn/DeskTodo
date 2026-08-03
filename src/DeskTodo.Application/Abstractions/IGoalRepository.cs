using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="Goal"/>. Each method is a self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.</summary>
public interface IGoalRepository
{
    /// <summary>Includes each goal's <see cref="Goal.Completions"/> — needed for <see cref="Goal.GetCurrentStreak"/>.</summary>
    Task<IReadOnlyList<Goal>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Goal goal, CancellationToken cancellationToken = default);

    Task UpdateAsync(Goal goal, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the goal doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds today's completion if it isn't already logged — idempotent, since the unique (GoalId, CompletedDate) index means a duplicate call is a no-op, not an error.</summary>
    Task AddCompletionAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>No-ops if that day was never marked done — how "undo today's check-off" is implemented.</summary>
    Task RemoveCompletionAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default);
}
