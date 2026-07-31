namespace DeskTodo.App.ViewModels;

/// <summary>Widget-only display concept — not a Domain state, unlike <c>TaskPriority</c>.</summary>
public enum TaskStatusFilter
{
    All,
    Active,
    Completed,
}
