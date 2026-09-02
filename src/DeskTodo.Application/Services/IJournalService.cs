using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Feature 60's Daily Journal use cases.</summary>
public interface IJournalService
{
    Task<IReadOnlyList<JournalEntry>> GetEntriesForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Every entry whose title or content contains <paramref name="searchText"/>, most recent date first.</summary>
    Task<IReadOnlyList<JournalEntry>> SearchAsync(string searchText, CancellationToken cancellationToken = default);

    Task<JournalEntry> AddEntryAsync(DateOnly date, string title, string content, string? mood, CancellationToken cancellationToken = default);

    Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
}
