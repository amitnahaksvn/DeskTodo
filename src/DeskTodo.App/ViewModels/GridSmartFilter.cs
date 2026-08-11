namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 25's Smart Lists — a small fixed set of cross-day, computed quick filters for the
/// grid view (the natural home for them, since it's already the one screen spanning every
/// day, unlike the day-scoped widget). <see cref="Favorites"/>/<see cref="Pinned"/> surface
/// <see cref="Domain.Entities.TaskItem.IsFavorite"/>/<see cref="Domain.Entities.TaskItem.IsPinned"/>
/// — flags that already existed but had no cross-day "view them all" UI before this.
/// </summary>
public enum GridSmartFilter
{
    None,
    Favorites,
    Pinned,
    Overdue,
    DueToday,
    HighPriority,
    NoProject,
}
