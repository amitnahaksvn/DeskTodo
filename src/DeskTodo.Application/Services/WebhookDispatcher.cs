using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IWebhookDispatcher"/>
/// <remarks>
/// <see cref="Events.IEventBus.Publish"/> calls every subscriber synchronously, so this handler
/// never awaits inline — it fires the actual (potentially multi-second, retrying) delivery work
/// off to the thread pool instead, so one slow/unreachable webhook endpoint can't stall whatever
/// just completed a task. This class is itself a singleton (it must outlive any one unit of work
/// to stay subscribed for the app's whole lifetime), so it resolves <see cref="IWebhookRepository"/>
/// and <see cref="IWebhookDeliveryClient"/> — both scoped, like every other repository/service in
/// this app — through a fresh <see cref="IServiceScope"/> per event rather than injecting them
/// directly, which would otherwise capture a scoped instance for the singleton's entire lifetime.
/// </remarks>
public sealed class WebhookDispatcher(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
{
    private IDisposable? _subscription;

    public void Start() => _subscription ??= eventBus.Subscribe(OnEvent);

    private void OnEvent(ApplicationEvent @event) => _ = HandleAsync(@event);

    private async Task HandleAsync(ApplicationEvent @event)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var webhookRepository = scope.ServiceProvider.GetRequiredService<IWebhookRepository>();
            var deliveryClient = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryClient>();

            var webhooks = await webhookRepository.GetEnabledForEventTypeAsync(@event.EventType);
            foreach (var webhook in webhooks)
            {
                await deliveryClient.DeliverAsync(webhook, @event.EventType, @event.EntityId, @event.PayloadJson);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch event {EventType} to webhooks", @event.EventType);
        }
    }
}
