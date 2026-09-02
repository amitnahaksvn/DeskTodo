using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>Feature 48's Task Relationships Graph use cases.</summary>
public interface ITaskRelationshipService
{
    /// <summary>The 1-hop relationship neighborhood of one task — deliberately not recursive, so the graph can never load unboundedly (this feature's own "avoid infinite graph loading" requirement).</summary>
    Task<IReadOnlyList<TaskRelationship>> GetRelationshipsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>No-ops (returns null) for a self-relationship or one that already exists between these two tasks with this type.</summary>
    Task<TaskRelationship?> AddRelationshipAsync(Guid sourceTaskId, Guid targetTaskId, TaskRelationshipType relationshipType, CancellationToken cancellationToken = default);

    Task RemoveRelationshipAsync(Guid relationshipId, CancellationToken cancellationToken = default);
}
