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

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetArchivedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetPinnedAsync(CancellationToken cancellationToken = default);

    /// <summary>Highest <see cref="TaskItem.DayOrder"/> currently used on the given day, or -1 if it has no tasks.</summary>
    Task<int> GetMaxDayOrderAsync(DateOnly planDate, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>Persists in-memory changes made to a previously-fetched (and since detached) <see cref="TaskItem"/>.</summary>
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a new drag-to-reorder sequence for a day's task list in a
    /// single unit of work (batches every <see cref="TaskItem.DayOrder"/>
    /// change into one save rather than one round-trip per task).
    /// </summary>
    Task ReorderAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default);
}
