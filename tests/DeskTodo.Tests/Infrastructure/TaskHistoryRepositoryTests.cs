using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TaskHistoryRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskHistoryRepository _sut;
    private readonly TaskRepository _taskRepository;

    public TaskHistoryRepositoryTests()
    {
        _sut = new TaskHistoryRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetForTaskAsync_ReturnsTheEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs" };
        await _taskRepository.AddAsync(task);
        var entry = new TaskHistory { TaskId = task.Id, Action = TaskHistoryAction.Created };

        await _sut.AddAsync(entry);
        var results = await _sut.GetForTaskAsync(task.Id);

        Assert.Single(results);
        Assert.Equal(TaskHistoryAction.Created, results[0].Action);
    }

    [Fact]
    public async Task GetForTaskAsync_OrdersByTimestampDescending_AndExcludesOtherTasks()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs" };
        var otherTask = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Unrelated task" };
        await _taskRepository.AddAsync(task);
        await _taskRepository.AddAsync(otherTask);

        var earlier = new TaskHistory { TaskId = task.Id, Action = TaskHistoryAction.Created, Timestamp = new DateTime(2026, 8, 15, 9, 0, 0) };
        var later = new TaskHistory { TaskId = task.Id, Action = TaskHistoryAction.Completed, Timestamp = new DateTime(2026, 8, 15, 11, 0, 0) };
        var unrelated = new TaskHistory { TaskId = otherTask.Id, Action = TaskHistoryAction.Created, Timestamp = new DateTime(2026, 8, 15, 10, 0, 0) };
        await _sut.AddAsync(earlier);
        await _sut.AddAsync(later);
        await _sut.AddAsync(unrelated);

        var results = await _sut.GetForTaskAsync(task.Id);

        Assert.Equal([later.Id, earlier.Id], results.Select(h => h.Id));
    }

    [Fact]
    public async Task WhenItsTaskIsPermanentlyDeleted_TheHistoryEntrySurvives_WithTaskIdSetToNull()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Gone forever" };
        await _taskRepository.AddAsync(task);
        await _sut.AddAsync(new TaskHistory { TaskId = task.Id, Action = TaskHistoryAction.Created });

        await _taskRepository.RemoveAsync(task.Id);

        var results = await _sut.GetForTaskAsync(task.Id);
        Assert.Empty(results); // no longer findable by the now-deleted task's Id ...

        await using var context = _fixture.ContextFactory.CreateDbContext();
        var survivingEntry = Assert.Single(context.TaskHistories);
        Assert.Null(survivingEntry.TaskId); // ... but the row itself, and its audit trail, still exists.
    }
}
