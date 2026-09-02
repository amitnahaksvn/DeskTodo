using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TaskGraphViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ITaskRelationshipService> _relationshipService = new();
    private readonly TaskGraphViewModel _sut;

    private readonly Guid _centerId = Guid.NewGuid();
    private readonly Guid _neighborId = Guid.NewGuid();

    public TaskGraphViewModelTests()
    {
        _sut = new TaskGraphViewModel(_taskService.Object, _relationshipService.Object, NullLogger<TaskGraphViewModel>.Instance);

        _taskService.Setup(s => s.GetTaskAsync(_centerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem { Id = _centerId, PlanDate = new DateOnly(2026, 9, 2), Title = "Design the API" });
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TaskItem { Id = _centerId, PlanDate = new DateOnly(2026, 9, 2), Title = "Design the API" },
                new TaskItem { Id = _neighborId, PlanDate = new DateOnly(2026, 9, 2), Title = "Implement the API" },
            ]);
        _relationshipService.Setup(s => s.GetRelationshipsForTaskAsync(_centerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TaskRelationship
                {
                    SourceTaskId = _centerId,
                    SourceTask = new TaskItem { Id = _centerId, PlanDate = new DateOnly(2026, 9, 2), Title = "Design the API" },
                    TargetTaskId = _neighborId,
                    TargetTask = new TaskItem { Id = _neighborId, PlanDate = new DateOnly(2026, 9, 2), Title = "Implement the API" },
                    RelationshipType = TaskRelationshipType.DependsOn,
                },
            ]);
    }

    [Fact]
    public async Task LoadAsync_PopulatesTheCenterNodeAndItsNeighbors()
    {
        await _sut.LoadAsync(_centerId);

        Assert.Equal(2, _sut.Nodes.Count);
        Assert.Contains(_sut.Nodes, n => n.TaskId == _centerId && n.IsCenter);
        Assert.Contains(_sut.Nodes, n => n.TaskId == _neighborId && !n.IsCenter);
        Assert.Single(_sut.Edges);
        Assert.Equal("DependsOn", _sut.Edges[0].Label);
    }

    [Fact]
    public async Task LoadAsync_WithAnUnknownTask_SetsAStatusMessage_AndBuildsNoGraph()
    {
        var unknownId = Guid.NewGuid();
        _taskService.Setup(s => s.GetTaskAsync(unknownId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        await _sut.LoadAsync(unknownId);

        Assert.NotEmpty(_sut.StatusMessage);
        Assert.Empty(_sut.Nodes);
    }

    [Fact]
    public async Task LoadAsync_PopulatesAvailableTasksOnlyOnce()
    {
        await _sut.LoadAsync(_centerId);
        await _sut.LoadAsync(_centerId);

        _taskService.Verify(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TogglingATypeFilterOff_RemovesMatchingEdgesAndNeighbors_WithoutARepositoryReload()
    {
        await _sut.LoadAsync(_centerId);
        var filter = _sut.TypeFilters.Single(f => f.Type == TaskRelationshipType.DependsOn);

        filter.IsEnabled = false;

        Assert.Empty(_sut.Edges);
        Assert.Single(_sut.Nodes); // Only the center remains.
        _relationshipService.Verify(s => s.GetRelationshipsForTaskAsync(_centerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectNodeCommand_SetsSelectedNode()
    {
        await _sut.LoadAsync(_centerId);
        var neighbor = _sut.Nodes.Single(n => n.TaskId == _neighborId);

        _sut.SelectNodeCommand.Execute(neighbor);

        Assert.Equal(neighbor, _sut.SelectedNode);
    }

    [Fact]
    public async Task RecenterCommand_ReloadsTheGraphOnTheGivenTask()
    {
        await _sut.LoadAsync(_centerId);
        _taskService.Setup(s => s.GetTaskAsync(_neighborId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem { Id = _neighborId, PlanDate = new DateOnly(2026, 9, 2), Title = "Implement the API" });
        _relationshipService.Setup(s => s.GetRelationshipsForTaskAsync(_neighborId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.RecenterCommand.ExecuteAsync(_neighborId);

        Assert.Equal(_neighborId, _sut.CenterTaskId);
        Assert.Equal("Implement the API", _sut.CenterTaskTitle);
    }

    [Fact]
    public void OpenTaskCommand_RaisesOpenTaskRequested()
    {
        Guid? requested = null;
        _sut.OpenTaskRequested += (_, id) => requested = id;

        _sut.OpenTaskCommand.Execute(_neighborId);

        Assert.Equal(_neighborId, requested);
    }

    [Fact]
    public async Task AddRelationshipCommand_WithNoTargetSelected_IsANoOp()
    {
        await _sut.LoadAsync(_centerId);

        await _sut.AddRelationshipCommand.ExecuteAsync(null);

        _relationshipService.Verify(s => s.AddRelationshipAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TaskRelationshipType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddRelationshipCommand_WithATargetSelected_AddsIt_AndReloadsTheGraph()
    {
        await _sut.LoadAsync(_centerId);
        var otherId = Guid.NewGuid();
        _sut.TargetTaskToAdd = new TaskOption(otherId, "Some other task");
        _sut.RelationshipTypeToAdd = TaskRelationshipType.Related;
        _relationshipService.Setup(s => s.AddRelationshipAsync(_centerId, otherId, TaskRelationshipType.Related, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskRelationship { SourceTaskId = _centerId, TargetTaskId = otherId, RelationshipType = TaskRelationshipType.Related });

        await _sut.AddRelationshipCommand.ExecuteAsync(null);

        _relationshipService.Verify(s => s.AddRelationshipAsync(_centerId, otherId, TaskRelationshipType.Related, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(_sut.TargetTaskToAdd);
    }

    [Fact]
    public async Task AddRelationshipCommand_WhenTheServiceReturnsNull_SetsAStatusMessage_AndDoesNotClearTheSelection()
    {
        await _sut.LoadAsync(_centerId);
        var otherId = Guid.NewGuid();
        _sut.TargetTaskToAdd = new TaskOption(otherId, "Some other task");
        _relationshipService.Setup(s => s.AddRelationshipAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TaskRelationshipType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskRelationship?)null);

        await _sut.AddRelationshipCommand.ExecuteAsync(null);

        Assert.NotEmpty(_sut.StatusMessage);
        Assert.NotNull(_sut.TargetTaskToAdd);
    }

    [Fact]
    public async Task RemoveRelationshipCommand_RemovesIt_AndReloadsTheGraph()
    {
        await _sut.LoadAsync(_centerId);
        var relationshipId = _sut.Edges[0].RelationshipId;
        _relationshipService.Setup(s => s.GetRelationshipsForTaskAsync(_centerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.RemoveRelationshipCommand.ExecuteAsync(relationshipId);

        _relationshipService.Verify(s => s.RemoveRelationshipAsync(relationshipId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_sut.Edges);
    }
}
