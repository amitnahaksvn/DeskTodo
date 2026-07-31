namespace DeskTodo.App.ViewModels;

/// <summary>
/// "Manual" preserves <c>DayOrder</c> — the order drag-to-reorder writes to. The others
/// are display-only re-sorts; they don't touch <c>DayOrder</c>, so switching back to
/// Manual always restores whatever order was last dragged/created.
/// </summary>
public enum TaskSortOption
{
    Manual,
    Priority,
    DueDate,
    Title,
}
