using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class KanbanViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly KanbanViewModel _sut;

    public KanbanViewModelTests()
    {
        _sut = new KanbanViewModel(_taskService.Object, NullLogger<KanbanViewModel>.Instance);
    }

    private static TaskItem CreateTask(string title, bool completed)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = title };
        if (completed)
        {
            task.Complete();
        }

        return task;
    }

    [Fact]
    public async Task LoadAsync_SplitsTasksIntoToDoAndDoneByIsCompleted()
    {
        var tasks = new[] { CreateTask("Open", false), CreateTask("Finished", true) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        await _sut.LoadAsync();

        Assert.Single(_sut.ToDoCards);
        Assert.Equal("Open", _sut.ToDoCards[0].Title);
        Assert.Single(_sut.DoneCards);
        Assert.Equal("Finished", _sut.DoneCards[0].Title);
    }

    [Fact]
    public async Task LoadAsync_ExcludesArchivedTasks()
    {
        var archived = CreateTask("Archived", false);
        archived.Archive();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([archived]);

        await _sut.LoadAsync();

        Assert.Empty(_sut.ToDoCards);
        Assert.Empty(_sut.DoneCards);
    }

    [Fact]
    public async Task MoveCommand_OnAToDoCard_CompletesTheTaskAndReloads()
    {
        var task = CreateTask("Open", false);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        var card = _sut.ToDoCards[0];
        Assert.Equal("Move to Done", card.MoveButtonLabel);

        await card.MoveCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CompleteTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task MoveCommand_OnADoneCard_ReopensTheTask()
    {
        var task = CreateTask("Finished", true);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        var card = _sut.DoneCards[0];
        Assert.Equal("Move to To Do", card.MoveButtonLabel);

        await card.MoveCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.ReopenTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
