namespace DeskTodo.Domain.Enums;

/// <summary>
/// What kind of activity a task represents — distinct from <see cref="TaskPriority"/>
/// (urgency) and <see cref="Entities.Category"/> (project/context grouping), which both
/// already exist; this answers "what is this," not "how urgent" or "which bucket."
/// </summary>
public enum TaskType
{
    Task = 0,
    Event = 1,
    Reminder = 2,
    Note = 3,
    Meeting = 4,
}
