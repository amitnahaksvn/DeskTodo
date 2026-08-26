using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Feature 42 (Roadmap-39-100.md) — a single audit-timeline entry for a <see cref="TaskItem"/>.
/// Written once, at the moment the change happens, and never edited afterward.
/// </summary>
/// <remarks>
/// Deliberately scoped down from the original spec: no <c>Source</c>/<c>Actor</c>/<c>Metadata</c>
/// fields (this is a single-user desktop app — there is no "who" to distinguish), and only a
/// fixed subset of task actions are recorded (see <c>TaskService</c>'s call sites) — high-frequency,
/// low-signal actions like Pin/Unpin/Favorite/Snooze/AddActualMinutes are deliberately excluded
/// to keep the timeline readable rather than flooded, the same kind of scope cut as Feature 46's
/// (Trash) skipped auto-purge retention policy.
///
/// <see cref="TaskId"/> is optional and <c>SetNull</c> on delete (same pattern as
/// <see cref="FocusSession.TaskId"/>) so a task's history survives that task being permanently
/// removed via <c>ITaskRepository.RemoveAsync</c> (Feature 46's "Delete Forever"/"Empty Trash") —
/// it just becomes unreachable from the UI at that point, since there is no surviving task to open
/// a history view from.
/// </remarks>
public sealed class TaskHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public required TaskHistoryAction Action { get; set; }

    /// <summary>Which field changed — only set when <see cref="Action"/> is <see cref="TaskHistoryAction.Renamed"/> or <see cref="TaskHistoryAction.Updated"/>.</summary>
    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
