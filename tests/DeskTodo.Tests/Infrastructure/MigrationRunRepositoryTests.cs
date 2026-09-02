using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class MigrationRunRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly MigrationRunRepository _sut;

    public MigrationRunRepositoryTests()
    {
        _sut = new MigrationRunRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_RoundTripsLogEntries()
    {
        var run = new MigrationRun
        {
            SourceDescription = "tasks.csv",
            Status = MigrationStatus.Completed,
            TotalRecords = 3,
            ImportedCount = 2,
            SkippedCount = 1,
            LogEntries = ["Row 2 imported: 'A'", "Row 3 skipped: duplicate"],
            CompletedAt = DateTime.UtcNow,
        };

        await _sut.AddAsync(run);
        var all = await _sut.GetAllAsync();

        var loaded = Assert.Single(all);
        Assert.Equal("tasks.csv", loaded.SourceDescription);
        Assert.Equal(2, loaded.LogEntries.Count);
        Assert.Equal(MigrationStatus.Completed, loaded.Status);
    }

    [Fact]
    public async Task GetAllAsync_OrdersMostRecentFirst()
    {
        var older = new MigrationRun { SourceDescription = "old.csv", StartedAt = DateTime.UtcNow.AddDays(-2) };
        var newer = new MigrationRun { SourceDescription = "new.csv", StartedAt = DateTime.UtcNow };
        await _sut.AddAsync(older);
        await _sut.AddAsync(newer);

        var all = await _sut.GetAllAsync();

        Assert.Equal(["new.csv", "old.csv"], all.Select(r => r.SourceDescription));
    }
}
