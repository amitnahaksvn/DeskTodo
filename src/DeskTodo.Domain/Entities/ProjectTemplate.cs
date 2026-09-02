using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Roadmap-39-100.md Feature 86 — a reusable "standard project" shape (e.g. "Software
/// Release Kit"): a named set of tasks and milestones with relative day-offsets rather than
/// fixed dates, so instantiating the template ("Create") on any chosen start date produces a
/// real <see cref="Entities.Project"/> with real <see cref="TaskItem"/>s and
/// <see cref="Entities.Milestone"/>s whose dates are computed from that start date. Distinct
/// from <see cref="TaskTemplate"/>/<see cref="TaskGroup"/> (Phase 17/38), which produce
/// standalone tasks with no project/milestone structure or date math.
/// </summary>
public sealed class ProjectTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Ordered set of tasks this template creates, each with a day-offset from the chosen start date.</summary>
    public List<ProjectTemplateTaskItem> TaskItems { get; set; } = [];

    /// <summary>Ordered set of milestones this template creates, each with a day-offset from the chosen start date.</summary>
    public List<ProjectTemplateMilestoneItem> MilestoneItems { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One task within a <see cref="ProjectTemplate"/>. Not a standalone EF entity — stored as
/// part of its owning template's JSON column (see <c>ProjectTemplateConfiguration</c>),
/// the same "not a second place a shape is defined" reasoning <see cref="TaskGroup"/> uses
/// for its own list, just embedded rather than referenced since these don't exist independently.
/// </summary>
public sealed record ProjectTemplateTaskItem
{
    // A record (value equality) rather than a plain class — these are compared by
    // ProjectTemplateConfiguration's EF Core value comparer, which needs content equality
    // to detect real changes instead of flagging every roundtrip as modified.
    public required string Title { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>1-based day the task starts on, relative to the instantiated project's chosen start date (day 1 = the start date itself).</summary>
    public int DayOffsetStart { get; set; } = 1;

    /// <summary>How many days the task spans (e.g. "Development: Day 2–7" is offset 2, duration 6). The task's computed due date is <c>StartDate + (DayOffsetStart - 1) + (DurationDays - 1)</c> days.</summary>
    public int DurationDays { get; set; } = 1;
}

/// <summary>One milestone within a <see cref="ProjectTemplate"/> — see <see cref="ProjectTemplateTaskItem"/>'s doc comment for why this is embedded rather than a standalone entity, and why it's a record.</summary>
public sealed record ProjectTemplateMilestoneItem
{
    public required string Title { get; set; }

    /// <summary>1-based day the milestone's target date falls on, relative to the instantiated project's chosen start date.</summary>
    public int DayOffset { get; set; } = 1;
}
