using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class MilestoneRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IMilestoneRepository
{
    public async Task<IReadOnlyList<Milestone>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Milestones
            .AsNoTracking()
            .Include(m => m.Tasks)
            .OrderBy(m => m.TargetDate ?? DateOnly.MaxValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Milestones
            .AsNoTracking()
            .Include(m => m.Tasks)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Milestones.Add(milestone);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(milestone).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var milestone = await context.Milestones.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (milestone is null)
        {
            return;
        }

        context.Milestones.Remove(milestone);
        await context.SaveChangesAsync(cancellationToken);
    }
}
