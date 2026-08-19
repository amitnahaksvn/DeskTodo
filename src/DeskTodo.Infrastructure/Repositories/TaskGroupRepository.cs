using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskGroupRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskGroupRepository
{
    public async Task<IReadOnlyList<TaskGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task AddAsync(TaskGroup group, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.TaskGroups.Add(group);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskGroup group, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(group).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var group = await context.TaskGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (group is null)
        {
            return;
        }

        context.TaskGroups.Remove(group);
        await context.SaveChangesAsync(cancellationToken);
    }
}
