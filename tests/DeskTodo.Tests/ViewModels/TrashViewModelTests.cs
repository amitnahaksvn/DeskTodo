using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TrashViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly TrashViewModel _sut;

    public TrashViewModelTests()
    {
        _sut = new TrashViewModel(_taskService.Object, NullLogger<TrashViewModel>.Instance);
    }

    private static TaskItem CreateDeletedTask(string title)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = title };
        task.SoftDelete();
        return task;
    }

    [Fact]
    public async Task LoadAsync_PopulatesDeletedTasks()
    {
        var task = CreateDeletedTask("Old report");
        _taskService.Setup(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.DeletedTasks);
        Assert.Equal("Old report", _sut.DeletedTasks[0].Title);
        Assert.NotEqual(string.Empty, _sut.DeletedTasks[0].DeletedAtDisplay);
    }

    [Fact]
    public async Task RestoreCommand_RestoresTheTask_AndRefreshesTheList_AndRaisesTaskRestored()
    {
        var taskId = Guid.NewGuid();
        _taskService.Setup(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var raised = false;
        _sut.TaskRestored += (_, _) => raised = true;

        await _sut.RestoreCommand.ExecuteAsync(taskId);

        _taskService.Verify(s => s.RestoreTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(raised);
    }

    [Fact]
    public async Task DeleteForeverCommand_PermanentlyDeletesTheTask_AndRefreshesTheList()
    {
        var taskId = Guid.NewGuid();
        _taskService.Setup(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.DeleteForeverCommand.ExecuteAsync(taskId);

        _taskService.Verify(s => s.PermanentlyDeleteTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmptyTrashCommand_EmptiesTheTrash_AndRefreshesTheList()
    {
        _taskService.Setup(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.EmptyTrashCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.EmptyTrashAsync(It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.GetDeletedTasksAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
