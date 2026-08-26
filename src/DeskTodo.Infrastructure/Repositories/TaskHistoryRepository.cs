using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskHistoryRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskHistoryRepository
{
    public async Task AddAsync(TaskHistory entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.TaskHistories.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskHistory>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskHistories
            .AsNoTracking()
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
