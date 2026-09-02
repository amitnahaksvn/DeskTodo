using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="InboxItem"/> — Feature 39's capture queue.</summary>
public interface IInboxRepository
{
    Task<InboxItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Unprocessed items, oldest first — the working queue a user actually triages.</summary>
    Task<IReadOnlyList<InboxItem>> GetUnprocessedAsync(CancellationToken cancellationToken = default);

    Task AddAsync(InboxItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(InboxItem item, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
