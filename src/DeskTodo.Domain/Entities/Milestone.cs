namespace DeskTodo.Domain.Entities;

/// <summary>
/// The project-management flavor of "goal" — a fixed deliverable with a target date that
/// <see cref="TaskItem"/>s can optionally link to (<see cref="TaskItem.MilestoneId"/>).
/// Distinct from <see cref="Goal"/>, the personal habit-style flavor with no fixed end
/// date. Deliberately flat (no parent/child milestone hierarchy) — see docs/ARCHITECTURE.md's
/// "Phase 21" section for why Sprint Planner and Roadmap View both reuse this single
/// entity/view rather than needing their own.
/// </summary>
public sealed class Milestone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? TargetDate { get; set; }

    /// <summary>Explicitly settable, not derived from every linked task being complete or the target date passing — mirrors <see cref="TaskItem.IsCompleted"/>'s "the user says so" reasoning rather than inferring it.</summary>
    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
