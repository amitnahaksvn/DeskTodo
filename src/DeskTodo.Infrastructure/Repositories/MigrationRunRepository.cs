using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class MigrationRunRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IMigrationRunRepository
{
    public async Task<IReadOnlyList<MigrationRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.MigrationRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MigrationRun run, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.MigrationRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);
    }
}
