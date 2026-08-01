using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TaskDependencyRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskDependencyRepository _sut;
    private readonly TaskRepository _taskRepository;

    public TaskDependencyRepositoryTests()
    {
        _sut = new TaskDependencyRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<TaskItem> CreateTaskAsync(string title)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = title };
        await _taskRepository.AddAsync(task);
        return task;
    }

    [Fact]
    public async Task AddAsync_ThenGetBlockersForTaskAsync_IncludesTheBlockingTask()
    {
        var blocked = await CreateTaskAsync("Ship release");
        var blocking = await CreateTaskAsync("Finish tests");

        await _sut.AddAsync(new TaskDependency { BlockingTaskId = blocking.Id, BlockedTaskId = blocked.Id });

        var blockers = await _sut.GetBlockersForTaskAsync(blocked.Id);
        Assert.Single(blockers);
        Assert.Equal("Finish tests", blockers[0].BlockingTask?.Title);
    }

    [Fact]
    public async Task ExistsAsync_TrueOnlyForTheExactDirection()
    {
        var blocked = await CreateTaskAsync("Ship release");
        var blocking = await CreateTaskAsync("Finish tests");
        await _sut.AddAsync(new TaskDependency { BlockingTaskId = blocking.Id, BlockedTaskId = blocked.Id });

        Assert.True(await _sut.ExistsAsync(blocking.Id, blocked.Id));
        Assert.False(await _sut.ExistsAsync(blocked.Id, blocking.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheDependency()
    {
        var blocked = await CreateTaskAsync("Ship release");
        var blocking = await CreateTaskAsync("Finish tests");
        var dependency = new TaskDependency { BlockingTaskId = blocking.Id, BlockedTaskId = blocked.Id };
        await _sut.AddAsync(dependency);

        await _sut.DeleteAsync(dependency.Id);

        Assert.Empty(await _sut.GetBlockersForTaskAsync(blocked.Id));
    }
}
