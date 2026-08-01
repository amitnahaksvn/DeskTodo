namespace DeskTodo.Domain.Enums;

/// <summary>How often a recurring task's next occurrence gets created — see <see cref="Entities.TaskItem.RecurrenceFrequency"/>.</summary>
public enum RecurrenceFrequency
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
}
