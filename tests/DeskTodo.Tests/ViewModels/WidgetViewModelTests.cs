using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class WidgetViewModelTests
{
    private static TaskItem CreateTask(DateOnly planDate, int order, string title) => new()
    {
        PlanDate = planDate,
        DayOrder = order,
        Title = title,
    };

    [Fact]
    public async Task TaskEditRequested_FiresWithTheRowsTaskId_WhenItsOpenEditorCommandRuns()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Read System Design");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        Guid? requestedId = null;
        sut.TaskEditRequested += (_, id) => requestedId = id;
        sut.Tasks[0].OpenEditorCommand.Execute(null);

        Assert.Equal(task.Id, requestedId);
    }

    [Fact]
    public async Task ReorderAsync_PersistsTheNewSequence_AndReloads()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var first = CreateTask(today, 0, "First");
        var second = CreateTask(today, 1, "Second");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.ReorderAsync(second.Id, first.Id);

        taskRepository.Verify(
            r => r.ReorderAsync(today, It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2 && ids[0] == second.Id && ids[1] == first.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        // LoadTasksAsync is called again as part of the reorder — GetByDateAsync should
        // have run twice (initial load + post-reorder reload).
        taskRepository.Verify(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReorderAsync_WithSameSourceAndTarget_IsANoOp()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Only task");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.ReorderAsync(task.Id, task.Id);

        taskRepository.Verify(r => r.ReorderAsync(It.IsAny<DateOnly>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
