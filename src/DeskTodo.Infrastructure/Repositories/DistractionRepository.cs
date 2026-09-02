using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class DistractionRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IDistractionRepository
{
    public async Task<IReadOnlyList<Distraction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Distractions.AsNoTracking().OrderByDescending(d => d.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Distraction distraction, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Distractions.Add(distraction);
        await context.SaveChangesAsync(cancellationToken);
    }
}
