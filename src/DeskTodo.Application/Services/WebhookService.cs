using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IWebhookService"/>
public sealed class WebhookService(
    IWebhookRepository webhookRepository,
    IWebhookDeliveryLogRepository deliveryLogRepository,
    IWebhookDeliveryClient deliveryClient) : IWebhookService
{
    public Task<IReadOnlyList<WebhookSubscription>> GetWebhooksAsync(CancellationToken cancellationToken = default) =>
        webhookRepository.GetAllAsync(cancellationToken);

    public Task<WebhookSubscription?> GetWebhookAsync(Guid id, CancellationToken cancellationToken = default) =>
        webhookRepository.GetByIdAsync(id, cancellationToken);

    public async Task<WebhookSubscription> CreateWebhookAsync(string name, string url, IReadOnlyList<string> eventTypes, string? secret, CancellationToken cancellationToken = default)
    {
        var webhook = new WebhookSubscription
        {
            Name = name.Trim(),
            Url = url.Trim(),
            EventTypes = eventTypes.ToList(),
            Secret = string.IsNullOrWhiteSpace(secret) ? null : secret.Trim(),
        };

        await webhookRepository.AddAsync(webhook, cancellationToken);
        return webhook;
    }

    public Task UpdateWebhookAsync(WebhookSubscription webhook, CancellationToken cancellationToken = default) =>
        webhookRepository.UpdateAsync(webhook, cancellationToken);

    public Task DeleteWebhookAsync(Guid id, CancellationToken cancellationToken = default) =>
        webhookRepository.DeleteAsync(id, cancellationToken);

    public Task<IReadOnlyList<WebhookDeliveryLog>> GetDeliveryHistoryAsync(Guid webhookId, CancellationToken cancellationToken = default) =>
        deliveryLogRepository.GetForWebhookAsync(webhookId, cancellationToken: cancellationToken);

    public async Task<WebhookDeliveryLog> SendTestDeliveryAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        var webhook = await webhookRepository.GetByIdAsync(webhookId, cancellationToken)
            ?? throw new InvalidOperationException($"Webhook {webhookId} was not found.");

        return await deliveryClient.DeliverAsync(webhook, "Test", Guid.Empty, payloadJson: null, cancellationToken);
    }
}
