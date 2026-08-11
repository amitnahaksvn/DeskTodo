using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class FocusSessionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly FocusSessionRepository _sut;
    private readonly TaskRepository _taskRepository;

    public FocusSessionRepositoryTests()
    {
        _sut = new FocusSessionRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private static FocusSession MakeSession(FocusSessionType type, DateTime startedAt, int durationMinutes, Guid? taskId = null) => new()
    {
        Type = type,
        TaskId = taskId,
        StartedAt = startedAt,
        EndedAt = startedAt.AddMinutes(durationMinutes),
        DurationMinutes = durationMinutes,
    };

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsTheSession()
    {
        var session = MakeSession(FocusSessionType.Stopwatch, new DateTime(2026, 8, 15, 9, 0, 0), 25);

        await _sut.AddAsync(session);
        var all = await _sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal(FocusSessionType.Stopwatch, all[0].Type);
        Assert.Equal(25, all[0].DurationMinutes);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartedAtDescending()
    {
        var earlier = MakeSession(FocusSessionType.Pomodoro, new DateTime(2026, 8, 15, 9, 0, 0), 25);
        var later = MakeSession(FocusSessionType.Pomodoro, new DateTime(2026, 8, 15, 11, 0, 0), 25);
        await _sut.AddAsync(earlier);
        await _sut.AddAsync(later);

        var all = await _sut.GetAllAsync();

        Assert.Equal([later.Id, earlier.Id], all.Select(s => s.Id));
    }

    [Fact]
    public async Task GetByTaskIdAsync_ReturnsOnlySessionsForThatTask_MostRecentFirst()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs" };
        var otherTask = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Unrelated task" };
        await _taskRepository.AddAsync(task);
        await _taskRepository.AddAsync(otherTask);

        var first = MakeSession(FocusSessionType.CountdownTimer, new DateTime(2026, 8, 15, 9, 0, 0), 25, task.Id);
        var second = MakeSession(FocusSessionType.CountdownTimer, new DateTime(2026, 8, 15, 10, 0, 0), 25, task.Id);
        var unrelated = MakeSession(FocusSessionType.CountdownTimer, new DateTime(2026, 8, 15, 9, 30, 0), 25, otherTask.Id);
        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        await _sut.AddAsync(unrelated);

        var results = await _sut.GetByTaskIdAsync(task.Id);

        Assert.Equal([second.Id, first.Id], results.Select(s => s.Id));
    }

    [Fact]
    public async Task GetByTaskIdAsync_WithNoSessions_ReturnsEmpty()
    {
        var results = await _sut.GetByTaskIdAsync(Guid.NewGuid());

        Assert.Empty(results);
    }
}
