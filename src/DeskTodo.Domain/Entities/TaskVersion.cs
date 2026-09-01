using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Feature 44 (Roadmap-39-100.md) — a full field snapshot of a <see cref="TaskItem"/> taken
/// immediately before a change is saved. Unlike <see cref="TaskHistory"/> (which records a
/// single before/after field diff per change), a <see cref="TaskVersion"/> captures the whole
/// task shape at that point in time, so an earlier version can be restored wholesale.
/// </summary>
/// <remarks>
/// <see cref="TaskId"/> is optional and <c>SetNull</c> on hard delete, the same pattern as
/// <see cref="TaskHistory.TaskId"/> — a task's version history survives Feature 46's "Delete
/// Forever"/"Empty Trash", it just becomes unreachable from the UI at that point.
/// </remarks>
public sealed class TaskVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    /// <summary>1-based, increasing per task — the order versions were captured in.</summary>
    public int VersionNumber { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; }

    public Guid? CategoryId { get; set; }

    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }

    public string? ColorHex { get; set; }

    public int? EstimatedMinutes { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
