using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="WebhookSubscription"/> rows.</summary>
public interface IWebhookRepository
{
    Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Every enabled webhook subscribed to <paramref name="eventType"/> — what the dispatcher fans an event out to.</summary>
    Task<IReadOnlyList<WebhookSubscription>> GetEnabledForEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
