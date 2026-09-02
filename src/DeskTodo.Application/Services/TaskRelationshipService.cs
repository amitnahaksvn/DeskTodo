using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskRelationshipService"/>
public sealed class TaskRelationshipService(ITaskRelationshipRepository relationshipRepository) : ITaskRelationshipService
{
    public Task<IReadOnlyList<TaskRelationship>> GetRelationshipsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        relationshipRepository.GetForTaskAsync(taskId, cancellationToken);

    public async Task<TaskRelationship?> AddRelationshipAsync(Guid sourceTaskId, Guid targetTaskId, TaskRelationshipType relationshipType, CancellationToken cancellationToken = default)
    {
        if (sourceTaskId == targetTaskId)
        {
            return null;
        }

        if (await relationshipRepository.ExistsAsync(sourceTaskId, targetTaskId, relationshipType, cancellationToken))
        {
            return null;
        }

        var relationship = new TaskRelationship
        {
            SourceTaskId = sourceTaskId,
            TargetTaskId = targetTaskId,
            RelationshipType = relationshipType,
        };

        await relationshipRepository.AddAsync(relationship, cancellationToken);
        return relationship;
    }

    public Task RemoveRelationshipAsync(Guid relationshipId, CancellationToken cancellationToken = default) =>
        relationshipRepository.DeleteAsync(relationshipId, cancellationToken);
}
