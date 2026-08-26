using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="TaskItem"/>. Implemented against
/// EF Core/SQLite in the Infrastructure layer; the Application layer only
/// depends on this interface, never on EF Core directly.
/// </summary>
/// <remarks>
/// Each method is a self-contained unit of work (it persists immediately;
/// there is no separate "SaveChanges" step). Infrastructure creates a new,
/// short-lived <c>DbContext</c> per call via <c>IDbContextFactory</c> rather
/// than sharing one long-lived context for the app's lifetime — the
/// recommended EF Core pattern for desktop apps, since a single shared
/// context isn't thread-safe and its change tracker would otherwise grow
/// unbounded for the life of the process.
/// </remarks>
public interface ITaskRepository
{
    /// <summary>Non-deleted, non-archived tasks for a given day, ordered by <see cref="TaskItem.DayOrder"/>.</summary>
    Task<IReadOnlyList<TaskItem>> GetByDateAsync(DateOnly planDate, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted task (including archived), across every day — for export, not the day-scoped widget view.</summary>
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Includes <see cref="TaskItem.ChecklistItems"/> (ordered) and <see cref="TaskItem.Tags"/> — the full-field editor's data source.</summary>
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Incomplete, non-archived, non-deleted tasks whose <see cref="TaskItem.PlanDate"/> is before <paramref name="date"/> — feeds the "auto-reschedule overdue tasks" setting.</summary>
    Task<IReadOnlyList<TaskItem>> GetIncompleteBeforeDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetArchivedAsync(CancellationToken cancellationToken = default);

    /// <summary>Soft-deleted tasks, most recently deleted first — Feature 46's Trash view.</summary>
    Task<IReadOnlyList<TaskItem>> GetDeletedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetPinnedAsync(CancellationToken cancellationToken = default);

    /// <summary>Highest <see cref="TaskItem.DayOrder"/> currently used on the given day, or -1 if it has no tasks.</summary>
    Task<int> GetMaxDayOrderAsync(DateOnly planDate, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>Persists in-memory changes made to a previously-fetched (and since detached) <see cref="TaskItem"/>.</summary>
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard delete — permanently removes the row, unlike every other "delete" in this app
    /// (see <see cref="TaskItem.SoftDelete"/>). Only Feature 46's Trash view ("Delete
    /// Permanently"/"Empty Trash") should ever call this. No-ops if the task doesn't exist.
    /// </summary>
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a new drag-to-reorder sequence for a day's task list in a
    /// single unit of work (batches every <see cref="TaskItem.DayOrder"/>
    /// change into one save rather than one round-trip per task).
    /// </summary>
    Task ReorderAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default);
}
