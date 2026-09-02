using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class FocusContextRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly FocusContextRepository _sut;

    public FocusContextRepositoryTests()
    {
        _sut = new FocusContextRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsItOrderedByName()
    {
        await _sut.AddAsync(new FocusContext { Name = "Work", ColorHex = "#3B82F6" });
        await _sut.AddAsync(new FocusContext { Name = "Learning", ColorHex = "#22C55E" });

        var results = await _sut.GetAllAsync();

        Assert.Equal(["Learning", "Work"], results.Select(c => c.Name));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheContext()
    {
        var context = new FocusContext { Name = "Personal", ColorHex = "#F59E0B" };
        await _sut.AddAsync(context);

        await _sut.DeleteAsync(context.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task WhenAContextIsDeleted_ATaskUsingItSurvives_WithContextIdSetToNull()
    {
        var context = new FocusContext { Name = "Side Project", ColorHex = "#8B5CF6" };
        await _sut.AddAsync(context);
        var taskRepository = new TaskRepository(_fixture.ContextFactory);
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Ship it", ContextId = context.Id };
        await taskRepository.AddAsync(task);

        await _sut.DeleteAsync(context.Id);

        var fetched = await taskRepository.GetByIdAsync(task.Id);
        Assert.NotNull(fetched);
        Assert.Null(fetched.ContextId);
    }
}
