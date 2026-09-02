using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>
/// Actually performs one webhook delivery (with retry/backoff/signing/timeout) and records the
/// outcome — shared by <see cref="WebhookDispatcher"/> (event-triggered) and
/// <see cref="IWebhookService.SendTestDeliveryAsync"/> (manual "Send Test"), so delivery and
/// bookkeeping logic exists in exactly one place.
/// </summary>
public interface IWebhookDeliveryClient
{
    Task<WebhookDeliveryLog> DeliverAsync(WebhookSubscription webhook, string eventType, Guid entityId, string? payloadJson, CancellationToken cancellationToken = default);
}
