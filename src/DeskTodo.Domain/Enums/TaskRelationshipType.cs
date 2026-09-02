namespace DeskTodo.Domain.Enums;

/// <summary>
/// The relationship kinds Feature 48 (Roadmap-39-100.md, Task Relationships Graph) records
/// between two tasks. Purely informational/visual — unlike <see cref="Entities.TaskDependency"/>
/// (Phase 19), none of these enforce a completion guard; that stays exclusively
/// <see cref="Entities.TaskDependency"/>'s job so there is only ever one place that can block a
/// task from completing.
/// </summary>
public enum TaskRelationshipType
{
    Related,
    Blocks,
    BlockedBy,
    DependsOn,
    DuplicateOf,
    DerivedFrom,
    FollowUpOf,
}
