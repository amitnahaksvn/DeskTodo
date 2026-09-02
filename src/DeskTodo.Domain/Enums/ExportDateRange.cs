namespace DeskTodo.Domain.Enums;

/// <summary>Which tasks (by <see cref="Entities.TaskItem.PlanDate"/>) an <see cref="Entities.ExportProfile"/> includes — resolved relative to "today" every time the profile runs, not frozen to when it was saved.</summary>
public enum ExportDateRange
{
    All = 0,
    Today = 1,
    ThisWeek = 2,
    ThisMonth = 3,
}
