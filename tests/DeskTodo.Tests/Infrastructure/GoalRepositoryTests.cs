using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class GoalRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly GoalRepository _sut;

    public GoalRepositoryTests()
    {
        _sut = new GoalRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheGoal()
    {
        var goal = new Goal { Name = "Meditate daily" };

        await _sut.AddAsync(goal);
        var fetched = await _sut.GetByIdAsync(goal.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Meditate daily", fetched.Name);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName_AndIncludesCompletions()
    {
        var zebra = new Goal { Name = "Zebra" };
        var alpha = new Goal { Name = "Alpha" };
        await _sut.AddAsync(zebra);
        await _sut.AddAsync(alpha);
        await _sut.AddCompletionAsync(alpha.Id, new DateOnly(2026, 8, 15));

        var results = await _sut.GetAllAsync();

        Assert.Equal(["Alpha", "Zebra"], results.Select(g => g.Name));
        Assert.Single(results.Single(g => g.Name == "Alpha").Completions);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var goal = new Goal { Name = "Read more" };
        await _sut.AddAsync(goal);
        goal.IsArchived = true;

        await _sut.UpdateAsync(goal);

        var fetched = await _sut.GetByIdAsync(goal.Id);
        Assert.True(fetched!.IsArchived);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheGoal_AndItsCompletions()
    {
        var goal = new Goal { Name = "Exercise" };
        await _sut.AddAsync(goal);
        await _sut.AddCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));

        await _sut.DeleteAsync(goal.Id);

        Assert.Null(await _sut.GetByIdAsync(goal.Id));
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task AddCompletionAsync_IsIdempotentForTheSameDay()
    {
        var goal = new Goal { Name = "Meditate" };
        await _sut.AddAsync(goal);

        await _sut.AddCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));
        await _sut.AddCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));

        var fetched = await _sut.GetByIdAsync(goal.Id);
        Assert.Single(fetched!.Completions);
    }

    [Fact]
    public async Task RemoveCompletionAsync_RemovesOnlyThatDay()
    {
        var goal = new Goal { Name = "Meditate" };
        await _sut.AddAsync(goal);
        await _sut.AddCompletionAsync(goal.Id, new DateOnly(2026, 8, 14));
        await _sut.AddCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));

        await _sut.RemoveCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));

        var fetched = await _sut.GetByIdAsync(goal.Id);
        Assert.Single(fetched!.Completions);
        Assert.Equal(new DateOnly(2026, 8, 14), fetched.Completions.Single().CompletedDate);
    }

    [Fact]
    public async Task RemoveCompletionAsync_OnADayNeverMarked_DoesNotThrow()
    {
        var goal = new Goal { Name = "Meditate" };
        await _sut.AddAsync(goal);

        await _sut.RemoveCompletionAsync(goal.Id, new DateOnly(2026, 8, 15));
    }
}
