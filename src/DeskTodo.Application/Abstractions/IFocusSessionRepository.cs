using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="FocusSession"/>. Write-once (see the entity's own doc comment) — there's no Update, only Add and reads.</summary>
public interface IFocusSessionRepository
{
    Task AddAsync(FocusSession session, CancellationToken cancellationToken = default);

    /// <summary>Most recent first — for a task's own "time logged" history in the full-field editor.</summary>
    Task<IReadOnlyList<FocusSession>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Every session ever logged, most recent first — read-only raw material for Phase 24's analytics, not consumed by anything in this phase.</summary>
    Task<IReadOnlyList<FocusSession>> GetAllAsync(CancellationToken cancellationToken = default);
}
