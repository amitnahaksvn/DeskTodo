using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class ExportProfileRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly ExportProfileRepository _sut;

    public ExportProfileRepositoryTests()
    {
        _sut = new ExportProfileRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private static ExportProfile MakeProfile(string name) => new()
    {
        Name = name,
        Format = ExportFormat.Csv,
        DateRange = ExportDateRange.ThisWeek,
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsTheProfile()
    {
        var profile = MakeProfile("Weekly Project Report");
        profile.ProjectId = Guid.NewGuid();

        await _sut.AddAsync(profile);
        var loaded = await _sut.GetByIdAsync(profile.Id);

        Assert.NotNull(loaded);
        Assert.Equal(ExportFormat.Csv, loaded!.Format);
        Assert.Equal(ExportDateRange.ThisWeek, loaded.DateRange);
        Assert.Equal(profile.ProjectId, loaded.ProjectId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProfilesOrderedByName()
    {
        await _sut.AddAsync(MakeProfile("Zeta Report"));
        await _sut.AddAsync(MakeProfile("Alpha Report"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(["Alpha Report", "Zeta Report"], all.Select(p => p.Name));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheProfile()
    {
        var profile = MakeProfile("Report");
        await _sut.AddAsync(profile);

        await _sut.DeleteAsync(profile.Id);

        Assert.Null(await _sut.GetByIdAsync(profile.Id));
    }
}
