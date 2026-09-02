using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="WebhookDeliveryLog"/> rows.</summary>
public interface IWebhookDeliveryLogRepository
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<WebhookDeliveryLog>> GetForWebhookAsync(Guid webhookId, int limit = 20, CancellationToken cancellationToken = default);

    Task AddAsync(WebhookDeliveryLog log, CancellationToken cancellationToken = default);
}
