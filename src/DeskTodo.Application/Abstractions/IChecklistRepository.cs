using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="ChecklistItem"/>. Each method is a
/// self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.
/// </summary>
public interface IChecklistRepository
{
    /// <summary>A task's checklist, ordered by <see cref="ChecklistItem.Order"/>.</summary>
    Task<IReadOnlyList<ChecklistItem>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<ChecklistItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Highest <see cref="ChecklistItem.Order"/> currently used on the given task's checklist, or -1 if it has none.</summary>
    Task<int> GetMaxOrderAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task AddAsync(ChecklistItem item, CancellationToken cancellationToken = default);

    /// <summary>Bulk-inserts every item in one unit of work — used when a template's checklist is copied wholesale onto a newly-created task.</summary>
    Task AddRangeAsync(IEnumerable<ChecklistItem> items, CancellationToken cancellationToken = default);

    Task UpdateAsync(ChecklistItem item, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the item doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
