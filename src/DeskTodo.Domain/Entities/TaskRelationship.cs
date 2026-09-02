using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// One directed edge in Feature 48's Task Relationships Graph (Roadmap-39-100.md) — e.g.
/// "<see cref="SourceTaskId"/> is a Duplicate Of <see cref="TargetTaskId"/>". A separate table
/// from <see cref="TaskDependency"/> (Phase 19) rather than an extension of it: this one is
/// purely informational, with no completion-guard behavior — see <see cref="TaskRelationshipType"/>.
/// </summary>
public sealed class TaskRelationship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid SourceTaskId { get; set; }

    public TaskItem? SourceTask { get; set; }

    public required Guid TargetTaskId { get; set; }

    public TaskItem? TargetTask { get; set; }

    public required TaskRelationshipType RelationshipType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
