using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Feature 96's Webhook Engine use cases — CRUD for subscriptions plus delivery history and a manual "Send Test".</summary>
public interface IWebhookService
{
    Task<IReadOnlyList<WebhookSubscription>> GetWebhooksAsync(CancellationToken cancellationToken = default);

    Task<WebhookSubscription?> GetWebhookAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WebhookSubscription> CreateWebhookAsync(string name, string url, IReadOnlyList<string> eventTypes, string? secret, CancellationToken cancellationToken = default);

    Task UpdateWebhookAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default);

    Task DeleteWebhookAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<WebhookDeliveryLog>> GetDeliveryHistoryAsync(Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>Fires a synthetic "Test" event at this one webhook right now, regardless of which real event types it's subscribed to — lets a user verify a URL/secret/headers before relying on it.</summary>
    Task<WebhookDeliveryLog> SendTestDeliveryAsync(Guid webhookId, CancellationToken cancellationToken = default);
}
