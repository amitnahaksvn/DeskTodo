using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskRelationshipServiceTests
{
    private readonly Mock<ITaskRelationshipRepository> _relationshipRepository = new();
    private readonly TaskRelationshipService _sut;

    public TaskRelationshipServiceTests()
    {
        _sut = new TaskRelationshipService(_relationshipRepository.Object);
    }

    [Fact]
    public async Task AddRelationshipAsync_WithANewValidPair_AddsIt_AndReturnsIt()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var result = await _sut.AddRelationshipAsync(sourceId, targetId, TaskRelationshipType.Related);

        Assert.NotNull(result);
        Assert.Equal(sourceId, result.SourceTaskId);
        Assert.Equal(targetId, result.TargetTaskId);
        Assert.Equal(TaskRelationshipType.Related, result.RelationshipType);
        _relationshipRepository.Verify(r => r.AddAsync(
            It.Is<TaskRelationship>(rel => rel.SourceTaskId == sourceId && rel.TargetTaskId == targetId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRelationshipAsync_WhenSourceAndTargetAreTheSameTask_IsANoOp_AndReturnsNull()
    {
        var taskId = Guid.NewGuid();

        var result = await _sut.AddRelationshipAsync(taskId, taskId, TaskRelationshipType.Related);

        Assert.Null(result);
        _relationshipRepository.Verify(r => r.AddAsync(It.IsAny<TaskRelationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddRelationshipAsync_WhenTheExactRelationshipAlreadyExists_IsANoOp_AndReturnsNull()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _relationshipRepository.Setup(r => r.ExistsAsync(sourceId, targetId, TaskRelationshipType.DuplicateOf, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.AddRelationshipAsync(sourceId, targetId, TaskRelationshipType.DuplicateOf);

        Assert.Null(result);
        _relationshipRepository.Verify(r => r.AddAsync(It.IsAny<TaskRelationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRelationshipsForTaskAsync_DelegatesToRepository()
    {
        var taskId = Guid.NewGuid();

        await _sut.GetRelationshipsForTaskAsync(taskId);

        _relationshipRepository.Verify(r => r.GetForTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRelationshipAsync_DelegatesToRepository()
    {
        var relationshipId = Guid.NewGuid();

        await _sut.RemoveRelationshipAsync(relationshipId);

        _relationshipRepository.Verify(r => r.DeleteAsync(relationshipId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
