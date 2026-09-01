using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TaskVersionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskVersionRepository _sut;
    private readonly TaskRepository _taskRepository;

    public TaskVersionRepositoryTests()
    {
        _sut = new TaskVersionRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetForTaskAsync_ReturnsTheSnapshot()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs" };
        await _taskRepository.AddAsync(task);
        var version = new TaskVersion { TaskId = task.Id, VersionNumber = 1, Title = "Write docs", Priority = TaskPriority.Medium };

        await _sut.AddAsync(version);
        var results = await _sut.GetForTaskAsync(task.Id);

        Assert.Single(results);
        Assert.Equal("Write docs", results[0].Title);
    }

    [Fact]
    public async Task GetForTaskAsync_OrdersByVersionNumberDescending_AndExcludesOtherTasks()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task A" };
        var otherTask = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task B" };
        await _taskRepository.AddAsync(task);
        await _taskRepository.AddAsync(otherTask);

        var v1 = new TaskVersion { TaskId = task.Id, VersionNumber = 1, Title = "v1" };
        var v2 = new TaskVersion { TaskId = task.Id, VersionNumber = 2, Title = "v2" };
        var unrelated = new TaskVersion { TaskId = otherTask.Id, VersionNumber = 1, Title = "other" };
        await _sut.AddAsync(v1);
        await _sut.AddAsync(v2);
        await _sut.AddAsync(unrelated);

        var results = await _sut.GetForTaskAsync(task.Id);

        Assert.Equal([v2.Id, v1.Id], results.Select(v => v.Id));
    }

    [Fact]
    public async Task GetMaxVersionNumberAsync_ReturnsZero_WhenNoneExist()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Fresh task" };
        await _taskRepository.AddAsync(task);

        var max = await _sut.GetMaxVersionNumberAsync(task.Id);

        Assert.Equal(0, max);
    }

    [Fact]
    public async Task GetMaxVersionNumberAsync_ReturnsHighestVersionNumber()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task" };
        await _taskRepository.AddAsync(task);
        await _sut.AddAsync(new TaskVersion { TaskId = task.Id, VersionNumber = 1, Title = "v1" });
        await _sut.AddAsync(new TaskVersion { TaskId = task.Id, VersionNumber = 3, Title = "v3" });

        var max = await _sut.GetMaxVersionNumberAsync(task.Id);

        Assert.Equal(3, max);
    }

    [Fact]
    public async Task WhenItsTaskIsPermanentlyDeleted_TheVersionSurvives_WithTaskIdSetToNull()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Gone forever" };
        await _taskRepository.AddAsync(task);
        var version = new TaskVersion { TaskId = task.Id, VersionNumber = 1, Title = "Gone forever" };
        await _sut.AddAsync(version);

        await _taskRepository.RemoveAsync(task.Id);

        var results = await _sut.GetForTaskAsync(task.Id);
        Assert.Empty(results);

        await using var context = _fixture.ContextFactory.CreateDbContext();
        var survivingVersion = Assert.Single(context.TaskVersions);
        Assert.Null(survivingVersion.TaskId);
    }
}
