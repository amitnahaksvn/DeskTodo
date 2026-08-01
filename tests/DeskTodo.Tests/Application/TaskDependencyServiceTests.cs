using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskDependencyServiceTests
{
    private readonly Mock<ITaskDependencyRepository> _dependencyRepository = new();
    private readonly TaskDependencyService _sut;

    public TaskDependencyServiceTests()
    {
        _sut = new TaskDependencyService(_dependencyRepository.Object);
    }

    [Fact]
    public async Task AddBlockerAsync_WithNewValidPair_AddsIt()
    {
        var blockedId = Guid.NewGuid();
        var blockingId = Guid.NewGuid();

        await _sut.AddBlockerAsync(blockedId, blockingId);

        _dependencyRepository.Verify(r => r.AddAsync(
            It.Is<TaskDependency>(d => d.BlockedTaskId == blockedId && d.BlockingTaskId == blockingId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddBlockerAsync_WhenTaskWouldBlockItself_IsANoOp()
    {
        var taskId = Guid.NewGuid();

        await _sut.AddBlockerAsync(taskId, taskId);

        _dependencyRepository.Verify(r => r.AddAsync(It.IsAny<TaskDependency>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddBlockerAsync_WhenTheRelationshipAlreadyExists_IsANoOp()
    {
        var blockedId = Guid.NewGuid();
        var blockingId = Guid.NewGuid();
        _dependencyRepository.Setup(r => r.ExistsAsync(blockingId, blockedId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.AddBlockerAsync(blockedId, blockingId);

        _dependencyRepository.Verify(r => r.AddAsync(It.IsAny<TaskDependency>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddBlockerAsync_WhenItWouldCreateADirectTwoTaskCycle_IsANoOp()
    {
        var blockedId = Guid.NewGuid();
        var blockingId = Guid.NewGuid();
        // blockedId already blocks blockingId — adding the reverse would make a 2-cycle.
        _dependencyRepository.Setup(r => r.ExistsAsync(blockedId, blockingId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.AddBlockerAsync(blockedId, blockingId);

        _dependencyRepository.Verify(r => r.AddAsync(It.IsAny<TaskDependency>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveBlockerAsync_DelegatesToRepository()
    {
        var dependencyId = Guid.NewGuid();

        await _sut.RemoveBlockerAsync(dependencyId);

        _dependencyRepository.Verify(r => r.DeleteAsync(dependencyId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
