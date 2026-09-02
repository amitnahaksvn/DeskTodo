using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class BulkEditRuleRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IBulkEditRuleRepository
{
    public async Task<IReadOnlyList<BulkEditRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.BulkEditRules
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<BulkEditRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.BulkEditRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(BulkEditRule rule, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.BulkEditRules.Add(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var rule = await context.BulkEditRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule is null)
        {
            return;
        }

        context.BulkEditRules.Remove(rule);
        await context.SaveChangesAsync(cancellationToken);
    }
}
