using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class WebhookDeliveryLogRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IWebhookDeliveryLogRepository
{
    public async Task<IReadOnlyList<WebhookDeliveryLog>> GetForWebhookAsync(Guid webhookId, int limit = 20, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WebhookDeliveryLogs
            .AsNoTracking()
            .Where(l => l.WebhookId == webhookId)
            .OrderByDescending(l => l.AttemptedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WebhookDeliveryLog log, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.WebhookDeliveryLogs.Add(log);
        await context.SaveChangesAsync(cancellationToken);
    }
}
