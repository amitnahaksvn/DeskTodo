using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;

namespace DeskTodo.Tests.Application;

public class FocusSessionServiceTests
{
    private readonly Mock<IFocusSessionRepository> _focusSessionRepository = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly FocusSessionService _sut;

    public FocusSessionServiceTests()
    {
        _sut = new FocusSessionService(_focusSessionRepository.Object, _taskService.Object);
    }

    [Fact]
    public async Task CompleteSessionAsync_WithNoTask_LogsTheSession_AndDoesNotTouchAnyTask()
    {
        var startedAt = new DateTime(2026, 8, 15, 9, 0, 0);
        var endedAt = new DateTime(2026, 8, 15, 9, 25, 0);

        var session = await _sut.CompleteSessionAsync(FocusSessionType.Stopwatch, startedAt, endedAt, 25);

        Assert.Equal(FocusSessionType.Stopwatch, session.Type);
        Assert.Equal(25, session.DurationMinutes);
        Assert.Null(session.TaskId);
        _focusSessionRepository.Verify(r => r.AddAsync(It.Is<FocusSession>(s => s.DurationMinutes == 25 && s.TaskId == null), It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.AddActualMinutesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteSessionAsync_WithATask_LogsTheSession_AndAddsActualMinutesToThatTask()
    {
        var taskId = Guid.NewGuid();
        var startedAt = new DateTime(2026, 8, 15, 9, 0, 0);
        var endedAt = new DateTime(2026, 8, 15, 9, 50, 0);

        var session = await _sut.CompleteSessionAsync(FocusSessionType.CountdownTimer, startedAt, endedAt, 50, taskId);

        Assert.Equal(taskId, session.TaskId);
        _focusSessionRepository.Verify(r => r.AddAsync(It.Is<FocusSession>(s => s.TaskId == taskId), It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.AddActualMinutesAsync(taskId, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessionsForTaskAsync_DelegatesToRepository()
    {
        var taskId = Guid.NewGuid();

        await _sut.GetSessionsForTaskAsync(taskId);

        _focusSessionRepository.Verify(r => r.GetByTaskIdAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
