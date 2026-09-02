using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class FocusSessionRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IFocusSessionRepository
{
    public async Task AddAsync(FocusSession session, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FocusSession>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FocusSessions
            .AsNoTracking()
            .Where(s => s.TaskId == taskId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FocusSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FocusSessions
            .AsNoTracking()
            .Include(s => s.Task)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
