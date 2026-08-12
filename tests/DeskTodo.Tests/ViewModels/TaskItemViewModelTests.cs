using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
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

        _ = new TaskItemViewModel(CreateTask(completed), taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

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
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

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
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        await sut.ToggleCompleteCommand.ExecuteAsync(null);

        Assert.False(sut.IsCompleted);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => !t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void BeginEditCommand_CopiesTitleAndEntersEditMode()
    {
        var task = CreateTask(completed: false);
        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        sut.BeginEditCommand.Execute(null);

        Assert.True(sut.IsEditing);
        Assert.Equal(sut.Title, sut.EditingTitle);
    }

    [Fact]
    public void CancelEditCommand_LeavesTitleUnchanged()
    {
        var task = CreateTask(completed: false);
        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });
        sut.BeginEditCommand.Execute(null);
        sut.EditingTitle = "Something else entirely";

        sut.CancelEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
        Assert.Equal("Morning Exercise", sut.Title);
    }

    [Fact]
    public async Task CommitEditCommand_WithNewTitle_RenamesAndPersists()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });
        sut.BeginEditCommand.Execute(null);
        sut.EditingTitle = "Evening Exercise";

        await sut.CommitEditCommand.ExecuteAsync(null);

        Assert.False(sut.IsEditing);
        Assert.Equal("Evening Exercise", sut.Title);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.Title == "Evening Exercise"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitEditCommand_WithBlankTitle_CancelsInsteadOfSaving()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });
        sut.BeginEditCommand.Execute(null);
        sut.EditingTitle = "   ";

        await sut.CommitEditCommand.ExecuteAsync(null);

        Assert.False(sut.IsEditing);
        Assert.Equal("Morning Exercise", sut.Title);
        taskRepository.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TogglePinCommand_TogglesIsPinned()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        await sut.TogglePinCommand.ExecuteAsync(null);
        Assert.True(sut.IsPinned);

        await sut.TogglePinCommand.ExecuteAsync(null);
        Assert.False(sut.IsPinned);
    }

    [Fact]
    public async Task SnoozeCommand_SetsSnoozedUntilAboutOneHourFromNow()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        await sut.SnoozeCommand.ExecuteAsync(null);

        Assert.NotNull(sut.SnoozedUntil);
        Assert.True(sut.SnoozedUntil > DateTime.Now.AddMinutes(55) && sut.SnoozedUntil < DateTime.Now.AddMinutes(65));
    }

    [Fact]
    public async Task ArchiveCommand_RequestsAListRefresh()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var refreshRequested = false;
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => refreshRequested = true, _ => { });

        await sut.ArchiveCommand.ExecuteAsync(null);

        Assert.True(refreshRequested);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.IsArchived), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_RequestsAListRefresh()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var refreshRequested = false;
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => refreshRequested = true, _ => { });

        await sut.DeleteCommand.ExecuteAsync(null);

        Assert.True(refreshRequested);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DuplicateCommand_RequestsAListRefresh()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        taskRepository.Setup(r => r.GetMaxDayOrderAsync(task.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var taskService = new TaskService(taskRepository.Object);
        var refreshRequested = false;
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => refreshRequested = true, _ => { });

        await sut.DuplicateCommand.ExecuteAsync(null);

        Assert.True(refreshRequested);
        taskRepository.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t.Title == task.Title && t.Id != task.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_TogglesIsFavorite()
    {
        var task = CreateTask(completed: false);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var taskService = new TaskService(taskRepository.Object);
        var sut = new TaskItemViewModel(task, taskService, NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        await sut.ToggleFavoriteCommand.ExecuteAsync(null);
        Assert.True(sut.IsFavorite);

        await sut.ToggleFavoriteCommand.ExecuteAsync(null);
        Assert.False(sut.IsFavorite);
    }

    [Fact]
    public void DisplayColorHex_WhenTaskHasNoColor_FallsBackToPriorityColor()
    {
        var task = CreateTask(completed: false);
        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.Equal(sut.PriorityColorHex, sut.DisplayColorHex);
    }

    [Fact]
    public void DisplayColorHex_WhenTaskHasAColor_UsesIt()
    {
        var task = CreateTask(completed: false);
        task.ColorHex = "#8B5CF6";
        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.Equal("#8B5CF6", sut.DisplayColorHex);
    }

    [Fact]
    public void OpenEditorCommand_InvokesRequestFullEditWithTheTaskId()
    {
        var task = CreateTask(completed: false);
        Guid? requestedId = null;
        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, id => requestedId = id);

        sut.OpenEditorCommand.Execute(null);

        Assert.Equal(task.Id, requestedId);
    }

    [Fact]
    public void IsSubtask_ReflectsWhetherTheTaskHasAParent()
    {
        var withParent = CreateTask(completed: false);
        withParent.ParentTaskId = Guid.NewGuid();
        var withoutParent = CreateTask(completed: false);

        var sutWithParent = new TaskItemViewModel(withParent, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });
        var sutWithoutParent = new TaskItemViewModel(withoutParent, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.True(sutWithParent.IsSubtask);
        Assert.False(sutWithoutParent.IsSubtask);
    }

    [Fact]
    public void SubtaskCount_ReflectsTheNumberOfSubtasks()
    {
        var task = CreateTask(completed: false);
        task.Subtasks.Add(new TaskItem { PlanDate = task.PlanDate, Title = "Child 1", ParentTaskId = task.Id });
        task.Subtasks.Add(new TaskItem { PlanDate = task.PlanDate, Title = "Child 2", ParentTaskId = task.Id });

        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.Equal(2, sut.SubtaskCount);
    }

    [Fact]
    public void IsBlocked_ReflectsTheTasksBlockedState()
    {
        var task = CreateTask(completed: false);
        var blocker = CreateTask(completed: false);
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });

        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.True(sut.IsBlocked);
    }

    [Fact]
    public void HasNonDefaultType_IsFalseForThePlainTaskType()
    {
        var task = CreateTask(completed: false);
        task.Type = TaskType.Task;

        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.False(sut.HasNonDefaultType);
        Assert.Equal(string.Empty, sut.TypeIcon);
    }

    [Theory]
    [InlineData(TaskType.Event, "📅")]
    [InlineData(TaskType.Reminder, "⏰")]
    [InlineData(TaskType.Note, "📝")]
    [InlineData(TaskType.Meeting, "👥")]
    public void TypeIcon_ReflectsTheTasksType(TaskType type, string expectedIcon)
    {
        var task = CreateTask(completed: false);
        task.Type = type;

        var sut = new TaskItemViewModel(task, Mock.Of<ITaskService>(), NullLogger<TaskItemViewModel>.Instance, () => { }, _ => { });

        Assert.True(sut.HasNonDefaultType);
        Assert.Equal(expectedIcon, sut.TypeIcon);
    }
}
