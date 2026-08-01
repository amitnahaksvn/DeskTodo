using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object);
    }

    [Fact]
    public async Task CreateTaskAsync_AssignsNextDayOrder_AndAdds()
    {
        var planDate = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var task = await _sut.CreateTaskAsync(planDate, "Morning Exercise");

        Assert.Equal(3, task.DayOrder);
        Assert.Equal("Morning Exercise", task.Title);
        _taskRepository.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t == task), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_OnEmptyDay_StartsAtOrderZero()
    {
        var planDate = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var task = await _sut.CreateTaskAsync(planDate, "First task of the day");

        Assert.Equal(0, task.DayOrder);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskExists_CompletesAndPersists()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Read System Design" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        Assert.True(task.IsCompleted);
        _taskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskMissing_ThrowsTaskNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _taskRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<TaskNotFoundException>(() => _sut.CompleteTaskAsync(missingId));
        Assert.Equal(missingId, exception.TaskId);
    }

    [Fact]
    public async Task DuplicateTaskAsync_CopiesFieldsIntoANewTaskAtTheEndOfTheDay()
    {
        var source = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            DayOrder = 0,
            Title = "LinkedIn Post",
            Notes = "Draft first",
        };
        _taskRepository.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(source.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var copy = await _sut.DuplicateTaskAsync(source.Id);

        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal(source.Title, copy.Title);
        Assert.Equal(source.Notes, copy.Notes);
        Assert.Equal(5, copy.DayOrder);
    }

    [Fact]
    public async Task ReorderTasksAsync_DelegatesToRepository()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await _sut.ReorderTasksAsync(planDate, ids);

        _taskRepository.Verify(r => r.ReorderAsync(planDate, ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveTaskAsync_SetsIsArchived()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "DSA Practice" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.ArchiveTaskAsync(task.Id);

        Assert.True(task.IsArchived);
    }

    [Fact]
    public async Task FavoriteTaskAsync_SetsIsFavorite()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Big Goal" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.FavoriteTaskAsync(task.Id);

        Assert.True(task.IsFavorite);
    }

    [Fact]
    public async Task UnfavoriteTaskAsync_ClearsIsFavorite()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Big Goal" };
        task.MarkFavorite();
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.UnfavoriteTaskAsync(task.Id);

        Assert.False(task.IsFavorite);
    }

    [Fact]
    public async Task CompleteTaskAsync_OnANonRecurringTask_DoesNotCreateAnyNewTask()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "One-off" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        Assert.True(task.IsCompleted);
        _taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenBlocked_ThrowsTaskBlockedException_AndDoesNotComplete()
    {
        var blocker = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Blocker" };
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Blocked task" };
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var exception = await Assert.ThrowsAsync<TaskBlockedException>(() => _sut.CompleteTaskAsync(task.Id));

        Assert.Equal(task.Id, exception.TaskId);
        Assert.False(task.IsCompleted);
        _taskRepository.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTheBlockerIsAlreadyComplete_Succeeds()
    {
        var blocker = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Blocker" };
        blocker.Complete();
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Formerly blocked task" };
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task CreateTaskAsync_WithParentTaskId_SetsIt()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var parentId = Guid.NewGuid();
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var subtask = await _sut.CreateTaskAsync(planDate, "Write changelog", parentTaskId: parentId);

        Assert.Equal(parentId, subtask.ParentTaskId);
    }

    [Fact]
    public async Task CompleteTaskAsync_OnARecurringTask_CreatesTheNextOccurrence()
    {
        var task = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            Title = "Water plants",
            CategoryId = Guid.NewGuid(),
            RecurrenceFrequency = RecurrenceFrequency.Daily,
            RecurrenceInterval = 1,
        };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(new DateOnly(2026, 7, 28), It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        await _sut.CompleteTaskAsync(task.Id);

        Assert.True(task.IsCompleted);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.Title == "Water plants" && t.PlanDate == new DateOnly(2026, 7, 28) && t.CategoryId == task.CategoryId && !t.IsCompleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_OnARecurringTaskPastItsEndDate_DoesNotCreateANextOccurrence()
    {
        var task = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            Title = "Last one",
            RecurrenceFrequency = RecurrenceFrequency.Daily,
            RecurrenceEndDate = new DateOnly(2026, 7, 27),
        };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        _taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RescheduleOverdueTasksAsync_MovesEachOverdueTaskToTodayAtTheEndOfTheList()
    {
        var today = new DateOnly(2026, 7, 27);
        var overdueA = new TaskItem { PlanDate = today.AddDays(-3), Title = "A" };
        var overdueB = new TaskItem { PlanDate = today.AddDays(-1), Title = "B" };
        _taskRepository.Setup(r => r.GetIncompleteBeforeDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([overdueA, overdueB]);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var movedCount = await _sut.RescheduleOverdueTasksAsync(today);

        Assert.Equal(2, movedCount);
        Assert.Equal(today, overdueA.PlanDate);
        Assert.Equal(2, overdueA.DayOrder);
        Assert.Equal(today, overdueB.PlanDate);
        Assert.Equal(3, overdueB.DayOrder);
        _taskRepository.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RescheduleOverdueTasksAsync_WhenNothingIsOverdue_ReturnsZero_AndDoesNotQueryMaxDayOrder()
    {
        var today = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetIncompleteBeforeDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var movedCount = await _sut.RescheduleOverdueTasksAsync(today);

        Assert.Equal(0, movedCount);
        _taskRepository.Verify(r => r.GetMaxDayOrderAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
