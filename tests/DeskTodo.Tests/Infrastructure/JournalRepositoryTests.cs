using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class JournalRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly JournalRepository _sut;

    public JournalRepositoryTests()
    {
        _sut = new JournalRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetForDateAsync_ReturnsOnlyEntriesForThatDate()
    {
        var today = new DateOnly(2026, 8, 15);
        var todayEntry = new JournalEntry { Date = today, Title = "Today", Content = "..." };
        var otherEntry = new JournalEntry { Date = today.AddDays(1), Title = "Other", Content = "..." };
        await _sut.AddAsync(todayEntry);
        await _sut.AddAsync(otherEntry);

        var results = await _sut.GetForDateAsync(today);

        var result = Assert.Single(results);
        Assert.Equal("Today", result.Title);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        var earlier = new JournalEntry { Date = new DateOnly(2026, 8, 1), Title = "Earlier", Content = "..." };
        var later = new JournalEntry { Date = new DateOnly(2026, 8, 15), Title = "Later", Content = "..." };
        await _sut.AddAsync(earlier);
        await _sut.AddAsync(later);

        var results = await _sut.GetAllAsync();

        Assert.Equal([later.Id, earlier.Id], results.Select(e => e.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheEntry()
    {
        var entry = new JournalEntry { Date = new DateOnly(2026, 8, 15), Title = "Temp", Content = "..." };
        await _sut.AddAsync(entry);

        await _sut.DeleteAsync(entry.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }
}
