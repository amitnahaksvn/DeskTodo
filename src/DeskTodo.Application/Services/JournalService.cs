using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IJournalService"/>
public sealed class JournalService(IJournalRepository journalRepository) : IJournalService
{
    public Task<IReadOnlyList<JournalEntry>> GetEntriesForDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        journalRepository.GetForDateAsync(date, cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var all = await journalRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return all;
        }

        return all.Where(e =>
                e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                e.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<JournalEntry> AddEntryAsync(DateOnly date, string title, string content, string? mood, CancellationToken cancellationToken = default)
    {
        var entry = new JournalEntry
        {
            Date = date,
            Title = title.Trim(),
            Content = content.Trim(),
            Mood = string.IsNullOrWhiteSpace(mood) ? null : mood.Trim(),
        };
        await journalRepository.AddAsync(entry, cancellationToken);
        return entry;
    }

    public Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default) =>
        journalRepository.DeleteAsync(entryId, cancellationToken);
}
