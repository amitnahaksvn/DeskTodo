using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="TaskRelationship"/> rows.</summary>
public interface ITaskRelationshipRepository
{
    /// <summary>Every relationship where <paramref name="taskId"/> is either the source or the target — the graph's 1-hop neighborhood.</summary>
    Task<IReadOnlyList<TaskRelationship>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task AddAsync(TaskRelationship relationship, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the relationship doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid sourceTaskId, Guid targetTaskId, TaskRelationshipType relationshipType, CancellationToken cancellationToken = default);
}
