using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IGoalService"/>
public sealed class GoalService(IGoalRepository goalRepository) : IGoalService
{
    public async Task<IReadOnlyList<Goal>> GetGoalsAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var goals = await goalRepository.GetAllAsync(cancellationToken);
        return includeArchived ? goals : goals.Where(g => !g.IsArchived).ToList();
    }

    public async Task<Goal> CreateGoalAsync(string name, string? description = null, CancellationToken cancellationToken = default)
    {
        var goal = new Goal { Name = name.Trim(), Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim() };
        await goalRepository.AddAsync(goal, cancellationToken);
        return goal;
    }

    public async Task ArchiveGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdAsync(goalId, cancellationToken) ?? throw new GoalNotFoundException(goalId);
        goal.IsArchived = true;
        await goalRepository.UpdateAsync(goal, cancellationToken);
    }

    public async Task UnarchiveGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdAsync(goalId, cancellationToken) ?? throw new GoalNotFoundException(goalId);
        goal.IsArchived = false;
        await goalRepository.UpdateAsync(goal, cancellationToken);
    }

    public Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default) =>
        goalRepository.DeleteAsync(goalId, cancellationToken);

    public Task MarkCompletedAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default) =>
        goalRepository.AddCompletionAsync(goalId, date, cancellationToken);

    public Task UnmarkCompletedAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default) =>
        goalRepository.RemoveCompletionAsync(goalId, date, cancellationToken);
}
