namespace DeskTodo.Application.Services;

/// <summary>One entry in Feature 61's Activity Timeline — already display-ready, same "no converter needed" approach as <c>TaskHistoryEntryOption</c>.</summary>
public sealed record ActivityEntry(DateTime Timestamp, string Category, string Description);

/// <summary>
/// Feature 61 (Roadmap-39-100.md) — Activity Timeline. The spec calls for this to consume an
/// Event Bus (Feature 98), not yet built; until then this aggregates directly from each
/// feature's own already-persisted history (<see cref="Domain.Entities.TaskHistory"/> via
/// <see cref="Abstractions.ITaskHistoryRepository"/>, completed <see cref="Domain.Entities.FocusSession"/>s,
/// and <see cref="Domain.Entities.GoalCompletion"/>s) — a read-only query-time merge, the same
/// "no new persistence" approach Phase 21's Agenda/Timeline views already use. Revisit once
/// Feature 98 lands: a real-time event-driven feed would replace this polling aggregation.
/// </summary>
public interface IActivityTimelineService
{
    /// <summary>The most recent activity across tasks, focus sessions and goals, newest first.</summary>
    Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(int limit = 100, CancellationToken cancellationToken = default);
}
