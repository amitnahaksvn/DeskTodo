using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TaskHistoryViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly TaskHistoryViewModel _sut;

    public TaskHistoryViewModelTests()
    {
        _sut = new TaskHistoryViewModel(_taskService.Object, NullLogger<TaskHistoryViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_SetsTaskTitle_AndPopulatesEntries()
    {
        var taskId = Guid.NewGuid();
        var entry = new TaskHistory { TaskId = taskId, Action = TaskHistoryAction.Completed };
        _taskService.Setup(s => s.GetTaskHistoryAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        await _sut.LoadAsync(taskId, "Write report");

        Assert.Equal("Write report", _sut.TaskTitle);
        Assert.Single(_sut.Entries);
        Assert.Equal("Completed", _sut.Entries[0].Description);
    }

    [Fact]
    public async Task LoadAsync_WithNoHistory_LeavesEntriesEmpty()
    {
        var taskId = Guid.NewGuid();
        _taskService.Setup(s => s.GetTaskHistoryAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync(taskId, "New task");

        Assert.Empty(_sut.Entries);
    }

    [Fact]
    public void FromEntity_OnARenamedEntry_DescribesOldAndNewTitle()
    {
        var entry = new TaskHistory { Action = TaskHistoryAction.Renamed, OldValue = "Old", NewValue = "New" };

        var option = TaskHistoryEntryOption.FromEntity(entry);

        Assert.Equal("Renamed from \"Old\" to \"New\"", option.Description);
    }

    [Fact]
    public void FromEntity_OnAnUpdatedEntryWithANullOldValue_DisplaysNoneRatherThanBlank()
    {
        var entry = new TaskHistory { Action = TaskHistoryAction.Updated, FieldName = "DueDate", OldValue = null, NewValue = "2026-09-01" };

        var option = TaskHistoryEntryOption.FromEntity(entry);

        Assert.Equal("DueDate changed from \"(none)\" to \"2026-09-01\"", option.Description);
    }
}
