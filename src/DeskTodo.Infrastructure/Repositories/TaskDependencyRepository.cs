using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskDependencyRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskDependencyRepository
{
    public async Task<IReadOnlyList<TaskDependency>> GetBlockersForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskDependencies
            .AsNoTracking()
            .Include(d => d.BlockingTask)
            .Where(d => d.BlockedTaskId == taskId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskDependency dependency, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.TaskDependencies.Add(dependency);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var dependency = await context.TaskDependencies.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dependency is null)
        {
            return;
        }

        context.TaskDependencies.Remove(dependency);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid blockingTaskId, Guid blockedTaskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskDependencies
            .AnyAsync(d => d.BlockingTaskId == blockingTaskId && d.BlockedTaskId == blockedTaskId, cancellationToken);
    }
}
