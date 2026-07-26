using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TaskItemViewModelTests
{
    private static TaskItem CreateTask(bool completed)
    {
        var task = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            Title = "Morning Exercise",
            DayOrder = 0,
        };

        if (completed)
        {
            task.Complete();
        }

        return task;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_NeverPersistsTheJustLoadedState(bool completed)
    {
        // Regression test: an [ObservableProperty]-generated setter fires its
        // On<Property>Changed hook even when assigned from the constructor, so a
        // naive "IsCompleted = task.IsCompleted" would previously re-persist each
        // task's own state back to the database on every single load.
        var taskRepository = new Mock<ITaskRepository>();
        var taskService = new TaskService(taskRepository.Object);

        _ = new TaskItemViewModel(CreateTask(completed), taskService, NullLogger<TaskItemViewModel>.Instance);

        taskRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        taskRepository.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompleteCommand_OnIncompleteTask_CompletesIt()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance);

        await sut.ToggleCompleteCommand.ExecuteAsync(null);

        Assert.True(sut.IsCompleted);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleCompleteCommand_OnCompletedTask_ReopensIt()
    {
        var task = CreateTask(completed: true);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance);

        await sut.ToggleCompleteCommand.ExecuteAsync(null);

        Assert.False(sut.IsCompleted);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => !t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }
}
