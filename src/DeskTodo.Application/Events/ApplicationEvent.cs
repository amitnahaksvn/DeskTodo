namespace DeskTodo.Application.Events;

/// <summary>
/// One event on the app's <see cref="IEventBus"/> — Feature 98 (Roadmap-39-100.md). A snapshot,
/// not a command: consumers (Webhooks today; Activity Timeline/Analytics/Notifications/Plugins/
/// Automation/Audit per the spec's own "Consumers" list are all future subscribers the same
/// interface already supports) react to it, they don't mutate anything through it.
/// </summary>
public sealed record ApplicationEvent(string EventType, Guid EntityId, DateTime Timestamp, string Source, string? PayloadJson);

/// <summary>
/// The event type strings this app actually publishes today. Only the <c>Task*</c> ones are
/// wired up (from <see cref="Services.TaskService"/>) — Project/Milestone/FocusSession/Backup
/// events from this feature's own "Example events" list are deliberately not published this pass
/// (see Roadmap-39-100.md's Feature 98 entry), but any of those services can call
/// <c>eventBus.Publish(...)</c> with a constant added here, following the exact same pattern, the
/// moment that's picked up.
/// </summary>
public static class ApplicationEventTypes
{
    public const string TaskCreated = "TaskCreated";
    public const string TaskUpdated = "TaskUpdated";
    public const string TaskCompleted = "TaskCompleted";
    public const string TaskDeleted = "TaskDeleted";
    public const string TaskRestored = "TaskRestored";
}
