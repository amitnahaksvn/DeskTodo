using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="JournalEntry"/> — Feature 60's Daily Journal.</summary>
public interface IJournalRepository
{
    Task<IReadOnlyList<JournalEntry>> GetForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Every entry, most recent date first — for search.</summary>
    Task<IReadOnlyList<JournalEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
