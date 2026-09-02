using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class DecisionRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IDecisionRepository
{
    public async Task<IReadOnlyList<Decision>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Decisions.AsNoTracking().Include(d => d.Project).OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Decision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Decisions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Decisions.Add(decision);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Decisions.Update(decision);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var decision = await context.Decisions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (decision is not null)
        {
            context.Decisions.Remove(decision);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
