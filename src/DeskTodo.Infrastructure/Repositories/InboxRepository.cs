using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class InboxRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IInboxRepository
{
    public async Task<InboxItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.InboxItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InboxItem>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.InboxItems
            .AsNoTracking()
            .Where(i => i.Status == InboxItemStatus.Unprocessed)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InboxItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.InboxItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InboxItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.InboxItems.Update(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await context.InboxItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is not null)
        {
            context.InboxItems.Remove(item);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
