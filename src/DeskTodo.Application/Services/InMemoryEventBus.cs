using DeskTodo.Application.Events;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IEventBus"/>
/// <remarks>
/// A subscriber that throws is logged and skipped, not allowed to break delivery to the other
/// subscribers or bubble into the publisher's own call stack — a single misbehaving webhook or
/// future plugin should never be able to take down whatever just completed a task.
/// </remarks>
public sealed class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    private readonly List<(string? EventType, Action<ApplicationEvent> Handler)> _subscribers = [];
    private readonly object _lock = new();

    public void Publish(ApplicationEvent @event)
    {
        List<Action<ApplicationEvent>> handlers;
        lock (_lock)
        {
            handlers = _subscribers
                .Where(s => s.EventType is null || s.EventType == @event.EventType)
                .Select(s => s.Handler)
                .ToList();
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler(@event);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event bus subscriber threw while handling {EventType}", @event.EventType);
            }
        }
    }

    public IDisposable Subscribe(Action<ApplicationEvent> handler) => SubscribeCore(eventType: null, handler);

    public IDisposable Subscribe(string eventType, Action<ApplicationEvent> handler) => SubscribeCore(eventType, handler);

    private IDisposable SubscribeCore(string? eventType, Action<ApplicationEvent> handler)
    {
        var entry = (eventType, handler);
        lock (_lock)
        {
            _subscribers.Add(entry);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _subscribers.Remove(entry);
            }
        });
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            onDispose();
        }
    }
}
