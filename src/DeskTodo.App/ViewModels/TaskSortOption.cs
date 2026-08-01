namespace DeskTodo.App.ViewModels;

/// <summary>
/// "Manual" preserves <c>DayOrder</c> — the order drag-to-reorder writes to. The others
/// are display-only re-sorts; they don't touch <c>DayOrder</c>, so switching back to
/// Manual always restores whatever order was last dragged/created. "Category" doubles as
/// "group by category" — sorting by category name clusters same-category tasks together
/// without needing a separate grouped-list UI/view mode.
/// </summary>
public enum TaskSortOption
{
    Manual,
    Priority,
    DueDate,
    Title,
    Category,
}
