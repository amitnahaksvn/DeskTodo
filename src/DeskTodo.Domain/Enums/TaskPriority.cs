namespace DeskTodo.Domain.Enums;

/// <summary>
/// How urgently a task needs attention. Ordered low to high so callers can
/// sort/compare with the natural enum ordering.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}
