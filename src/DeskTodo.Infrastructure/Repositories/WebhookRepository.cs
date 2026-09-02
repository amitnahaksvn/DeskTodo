using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class WebhookRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IWebhookRepository
{
    public async Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WebhookSubscriptions.AsNoTracking().OrderBy(w => w.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetEnabledForEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // EventTypes is a JSON-string-converted column (no server-side Contains translation),
        // so the filter runs client-side after materializing the (typically small) enabled set.
        var enabled = await context.WebhookSubscriptions.AsNoTracking().Where(w => w.Enabled).ToListAsync(cancellationToken);
        return enabled.Where(w => w.EventTypes.Contains(eventType)).ToList();
    }

    public async Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task AddAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.WebhookSubscriptions.Add(webhook);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.WebhookSubscriptions.Update(webhook);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var webhook = await context.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (webhook is null)
        {
            return;
        }

        context.WebhookSubscriptions.Remove(webhook);
        await context.SaveChangesAsync(cancellationToken);
    }
}
