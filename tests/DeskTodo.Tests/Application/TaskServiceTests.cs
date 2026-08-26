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
    private readonly Mock<ITaskHistoryRepository> _taskHistoryRepository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object, _taskHistoryRepository.Object);
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
    public async Task SnoozeTaskAsync_SetsSnoozedUntil()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Pay rent" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var until = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

        await _sut.SnoozeTaskAsync(task.Id, until);

        Assert.Equal(until, task.SnoozedUntil);
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

    [Fact]
    public async Task AddActualMinutesAsync_WithNoPriorActualMinutes_SetsItToTheGivenAmount()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Write report" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.AddActualMinutesAsync(task.Id, 25);

        Assert.Equal(25, task.ActualMinutes);
        _taskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddActualMinutesAsync_WithExistingActualMinutes_Accumulates()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Write report", ActualMinutes = 30 };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.AddActualMinutesAsync(task.Id, 25);

        Assert.Equal(55, task.ActualMinutes);
    }

    [Fact]
    public async Task GetDeletedTasksAsync_DelegatesToTheRepository()
    {
        var deleted = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Deleted" };
        _taskRepository.Setup(r => r.GetDeletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([deleted]);

        var results = await _sut.GetDeletedTasksAsync();

        Assert.Equal([deleted], results);
    }

    [Fact]
    public async Task PermanentlyDeleteTaskAsync_DelegatesToTheRepository()
    {
        var taskId = Guid.NewGuid();

        await _sut.PermanentlyDeleteTaskAsync(taskId);

        _taskRepository.Verify(r => r.RemoveAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmptyTrashAsync_PermanentlyDeletesEveryCurrentlyDeletedTask()
    {
        var first = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "First" };
        var second = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Second" };
        _taskRepository.Setup(r => r.GetDeletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);

        await _sut.EmptyTrashAsync();

        _taskRepository.Verify(r => r.RemoveAsync(first.Id, It.IsAny<CancellationToken>()), Times.Once);
        _taskRepository.Verify(r => r.RemoveAsync(second.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_RecordsACreatedHistoryEntry()
    {
        var planDate = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var task = await _sut.CreateTaskAsync(planDate, "Morning Exercise");

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Created),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_RecordsACompletedHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Read System Design" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReopenTaskAsync_RecordsAReopenedHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Read System Design" };
        task.Complete();
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.ReopenTaskAsync(task.Id);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Reopened),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveTaskAsync_RecordsAnArchivedHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "DSA Practice" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.ArchiveTaskAsync(task.Id);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Archived),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreTaskAsync_RecordsARestoredHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "DSA Practice" };
        task.Archive();
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.RestoreTaskAsync(task.Id);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Restored),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_RecordsADeletedHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "DSA Practice" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.DeleteTaskAsync(task.Id);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id && h.Action == TaskHistoryAction.Deleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameTaskAsync_RecordsARenamedHistoryEntry_WithOldAndNewTitle()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Old title" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.RenameTaskAsync(task.Id, "New title");

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == task.Id
                && h.Action == TaskHistoryAction.Renamed
                && h.OldValue == "Old title"
                && h.NewValue == "New title"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameTaskAsync_ToTheSameTitle_RecordsNoHistoryEntry()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Same title" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.RenameTaskAsync(task.Id, "Same title");

        _taskHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TaskHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskAsync_RecordsAnUpdatedHistoryEntryPerChangedField()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task", Priority = TaskPriority.Low };
        var edited = new TaskItem { Id = existing.Id, PlanDate = existing.PlanDate, Title = "Task", Priority = TaskPriority.High };
        _taskRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.UpdateTaskAsync(edited);

        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.TaskId == existing.Id
                && h.Action == TaskHistoryAction.Updated
                && h.FieldName == nameof(TaskItem.Priority)
                && h.OldValue == "Low"
                && h.NewValue == "High"),
            It.IsAny<CancellationToken>()), Times.Once);
        _taskHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TaskHistory>(h => h.FieldName == nameof(TaskItem.Title)),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithNoActualChanges_RecordsNoHistoryEntry()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task" };
        _taskRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.UpdateTaskAsync(existing);

        _taskHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TaskHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTaskHistoryAsync_DelegatesToTheRepository()
    {
        var taskId = Guid.NewGuid();
        var entry = new TaskHistory { TaskId = taskId, Action = TaskHistoryAction.Created };
        _taskHistoryRepository.Setup(r => r.GetForTaskAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        var results = await _sut.GetTaskHistoryAsync(taskId);

        Assert.Equal([entry], results);
    }
}
