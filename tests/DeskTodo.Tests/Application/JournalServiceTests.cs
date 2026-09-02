using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class JournalServiceTests
{
    private readonly Mock<IJournalRepository> _journalRepository = new();
    private readonly JournalService _sut;

    public JournalServiceTests()
    {
        _sut = new JournalService(_journalRepository.Object);
    }

    [Fact]
    public async Task AddEntryAsync_TrimsFields_AndAdds()
    {
        var date = new DateOnly(2026, 8, 15);

        var entry = await _sut.AddEntryAsync(date, "  Title  ", "  Content  ", "  😀  ");

        Assert.Equal("Title", entry.Title);
        Assert.Equal("Content", entry.Content);
        Assert.Equal("😀", entry.Mood);
        _journalRepository.Verify(r => r.AddAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEntryAsync_WithBlankMood_StoresNull()
    {
        var entry = await _sut.AddEntryAsync(new DateOnly(2026, 8, 15), "Title", "Content", "   ");

        Assert.Null(entry.Mood);
    }

    [Fact]
    public async Task SearchAsync_WithBlankSearchText_ReturnsEverything()
    {
        var entry = new JournalEntry { Date = new DateOnly(2026, 8, 15), Title = "X", Content = "Y" };
        _journalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        var results = await _sut.SearchAsync("");

        Assert.Equal([entry], results);
    }

    [Fact]
    public async Task SearchAsync_MatchesTitleOrContent_CaseInsensitively()
    {
        var matchByTitle = new JournalEntry { Date = new DateOnly(2026, 8, 15), Title = "Great Day", Content = "..." };
        var matchByContent = new JournalEntry { Date = new DateOnly(2026, 8, 16), Title = "...", Content = "It was a great day" };
        var noMatch = new JournalEntry { Date = new DateOnly(2026, 8, 17), Title = "Nothing", Content = "special" };
        _journalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([matchByTitle, matchByContent, noMatch]);

        var results = await _sut.SearchAsync("great day");

        Assert.Equal(2, results.Count);
        Assert.Contains(matchByTitle, results);
        Assert.Contains(matchByContent, results);
    }

    [Fact]
    public async Task DeleteEntryAsync_DelegatesToTheRepository()
    {
        var entryId = Guid.NewGuid();

        await _sut.DeleteEntryAsync(entryId);

        _journalRepository.Verify(r => r.DeleteAsync(entryId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
