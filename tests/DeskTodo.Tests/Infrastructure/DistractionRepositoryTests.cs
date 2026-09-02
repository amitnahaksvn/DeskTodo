using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class DistractionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly DistractionRepository _sut;

    public DistractionRepositoryTests()
    {
        _sut = new DistractionRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsIt()
    {
        var distraction = new Distraction { StartedAt = DateTime.UtcNow.AddMinutes(-5), Category = DistractionCategory.Phone };
        distraction.End(DateTime.UtcNow);

        await _sut.AddAsync(distraction);
        var results = await _sut.GetAllAsync();

        var result = Assert.Single(results);
        Assert.Equal(DistractionCategory.Phone, result.Category);
        Assert.Equal(5, result.DurationMinutes);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartedAtDescending()
    {
        var earlier = new Distraction { StartedAt = new DateTime(2026, 8, 1), Category = DistractionCategory.Email };
        var later = new Distraction { StartedAt = new DateTime(2026, 8, 15), Category = DistractionCategory.Chat };
        await _sut.AddAsync(earlier);
        await _sut.AddAsync(later);

        var results = await _sut.GetAllAsync();

        Assert.Equal([later.Id, earlier.Id], results.Select(d => d.Id));
    }
}
