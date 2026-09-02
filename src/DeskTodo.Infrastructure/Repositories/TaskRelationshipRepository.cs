using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskRelationshipRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskRelationshipRepository
{
    public async Task<IReadOnlyList<TaskRelationship>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskRelationships
            .AsNoTracking()
            .Include(r => r.SourceTask)
            .Include(r => r.TargetTask)
            .Where(r => r.SourceTaskId == taskId || r.TargetTaskId == taskId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskRelationship relationship, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.TaskRelationships.Add(relationship);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var relationship = await context.TaskRelationships.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (relationship is null)
        {
            return;
        }

        context.TaskRelationships.Remove(relationship);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid sourceTaskId, Guid targetTaskId, TaskRelationshipType relationshipType, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskRelationships.AnyAsync(
            r => r.SourceTaskId == sourceTaskId && r.TargetTaskId == targetTaskId && r.RelationshipType == relationshipType,
            cancellationToken);
    }
}
