namespace DeskTodo.Application.Events;

/// <summary>
/// Feature 98 — a central, in-process publish/subscribe bus so producers (task mutations today)
/// and consumers (webhooks today) don't need to know about each other directly. Deliberately not
/// a persisted event store — this is a live decoupling mechanism, not an audit log (that's
/// <see cref="Domain.Entities.TaskHistory"/>'s job for tasks specifically).
/// </summary>
public interface IEventBus
{
    void Publish(ApplicationEvent @event);

    /// <summary>Subscribes to every event. Dispose the result to unsubscribe.</summary>
    IDisposable Subscribe(Action<ApplicationEvent> handler);

    /// <summary>Subscribes to only events whose <see cref="ApplicationEvent.EventType"/> matches. Dispose the result to unsubscribe.</summary>
    IDisposable Subscribe(string eventType, Action<ApplicationEvent> handler);
}
