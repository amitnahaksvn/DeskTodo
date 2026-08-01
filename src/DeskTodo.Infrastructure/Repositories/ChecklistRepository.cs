using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class ChecklistRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IChecklistRepository
{
    public async Task<IReadOnlyList<ChecklistItem>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ChecklistItems
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChecklistItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ChecklistItems
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<int> GetMaxOrderAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var maxOrder = await context.ChecklistItems
            .Where(c => c.TaskId == taskId)
            .Select(c => (int?)c.Order)
            .MaxAsync(cancellationToken);

        return maxOrder ?? -1;
    }

    public async Task AddAsync(ChecklistItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.ChecklistItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ChecklistItem> items, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.ChecklistItems.AddRange(items);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChecklistItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(item).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await context.ChecklistItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item is null)
        {
            return;
        }

        context.ChecklistItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }
}
