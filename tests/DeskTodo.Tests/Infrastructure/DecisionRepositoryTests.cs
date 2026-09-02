using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class DecisionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly DecisionRepository _sut;

    public DecisionRepositoryTests()
    {
        _sut = new DecisionRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsTheDecision()
    {
        var decision = new Decision { Title = "Use PostgreSQL", DecisionText = "PostgreSQL over MongoDB" };

        await _sut.AddAsync(decision);
        var results = await _sut.GetAllAsync();

        var result = Assert.Single(results);
        Assert.Equal("Use PostgreSQL", result.Title);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        var earlier = new Decision { Title = "Earlier", DecisionText = "X", CreatedAt = new DateTime(2026, 8, 1) };
        var later = new Decision { Title = "Later", DecisionText = "Y", CreatedAt = new DateTime(2026, 8, 15) };
        await _sut.AddAsync(earlier);
        await _sut.AddAsync(later);

        var results = await _sut.GetAllAsync();

        Assert.Equal([later.Id, earlier.Id], results.Select(d => d.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheDecision()
    {
        var decision = new Decision { Title = "Temp", DecisionText = "X" };
        await _sut.AddAsync(decision);

        await _sut.DeleteAsync(decision.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task WhenItsProjectIsDeleted_TheDecisionSurvives_WithProjectIdSetToNull()
    {
        var project = new Project { Name = "Backend", ColorHex = "#3B82F6" };
        var projectRepository = new ProjectRepository(_fixture.ContextFactory);
        await projectRepository.AddAsync(project);
        var decision = new Decision { Title = "Db choice", DecisionText = "X", ProjectId = project.Id };
        await _sut.AddAsync(decision);

        await projectRepository.DeleteAsync(project.Id);

        var fetched = await _sut.GetByIdAsync(decision.Id);
        Assert.NotNull(fetched);
        Assert.Null(fetched.ProjectId);
    }
}
