using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Roadmap-39-100.md Feature 88 — a saved, reusable "find tasks matching X, apply Y" rule.
/// Distinct from Phase 28's Batch Actions (bulk complete/delete on a manual multi-selection):
/// this finds its own targets from a set of AND-ed conditions rather than acting on whatever
/// the user happened to have selected, and can set fields/add a tag/move a project, not just
/// complete or delete.
/// </summary>
public sealed class BulkEditRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>All of these must match for a task to be included — this feature's spec shows only AND-chained conditions ("Project = X AND Priority = High AND DueDate &lt; Today"), so no OR/grouping is supported.</summary>
    public List<BulkEditCondition> Conditions { get; set; } = [];

    public List<BulkEditAction> Actions { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One AND-ed condition within a <see cref="BulkEditRule"/> — embedded (JSON column), same reasoning as <see cref="ProjectTemplateTaskItem"/>.</summary>
public sealed record BulkEditCondition
{
    public BulkEditConditionField Field { get; set; }

    public BulkEditConditionOperator Operator { get; set; } = BulkEditConditionOperator.Equals;

    /// <summary>Interpreted per <see cref="Field"/>: a <see cref="TaskPriority"/> name for Priority; a Project/Category id (as text) for Project/Category; "Today" or an ISO date for DueDate; "true"/"false" for IsCompleted; free text for TitleContains.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>One action within a <see cref="BulkEditRule"/>, applied to every task a rule matches.</summary>
public sealed record BulkEditAction
{
    public BulkEditActionType Type { get; set; }

    /// <summary>Interpreted per <see cref="Type"/>: a <see cref="TaskPriority"/> name for SetPriority; a tag name for AddTag; a Project/Category id (as text) for MoveToProject/SetCategory; unused for MarkCompleted/Delete.</summary>
    public string Value { get; set; } = string.Empty;
}
