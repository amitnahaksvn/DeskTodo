namespace DeskTodo.Domain.Enums;

/// <summary>A <see cref="Entities.TaskItem"/> field a <see cref="Entities.BulkEditCondition"/> can filter on.</summary>
public enum BulkEditConditionField
{
    Project = 0,
    Category = 1,
    Priority = 2,
    DueDate = 3,
    IsCompleted = 4,
    TitleContains = 5,
}
