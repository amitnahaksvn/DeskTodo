using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TaskEditViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IChecklistService> _checklistService = new();
    private readonly Mock<ITagService> _tagService = new();
    private readonly Mock<ITaskTemplateService> _templateService = new();
    private readonly Mock<ITaskDependencyService> _taskDependencyService = new();
    private readonly Mock<IAttachmentService> _attachmentService = new();
    private readonly TaskEditViewModel _sut;

    public TaskEditViewModelTests()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _taskService.Setup(s => s.GetTasksForDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskItem>());
        _attachmentService.Setup(s => s.GetAttachmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Attachment>());
        _sut = new TaskEditViewModel(
            _taskService.Object,
            _categoryRepository.Object,
            _checklistService.Object,
            _tagService.Object,
            _templateService.Object,
            _taskDependencyService.Object,
            _attachmentService.Object,
            NullLogger<TaskEditViewModel>.Instance,
            NullLogger<ChecklistItemRowViewModel>.Instance);
    }

    private static TaskItem MakeTask(Action<TaskItem>? configure = null)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Water plants" };
        configure?.Invoke(task);
        return task;
    }

    [Fact]
    public async Task LoadAsync_PopulatesChecklistTagsColorAndRecurrenceFromTheTask()
    {
        var task = MakeTask(t =>
        {
            t.ColorHex = "#8B5CF6";
            t.RecurrenceFrequency = RecurrenceFrequency.Weekly;
            t.RecurrenceInterval = 2;
            t.RecurrenceEndDate = new DateOnly(2026, 12, 31);
            t.ChecklistItems.Add(new ChecklistItem { TaskId = t.Id, Text = "Buy soil", Order = 0 });
            t.Tags.Add(new Tag { Name = "Home" });
        });
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.LoadAsync(task.Id);

        Assert.Equal("#8B5CF6", _sut.SelectedColorHex);
        Assert.Equal(RecurrenceFrequency.Weekly, _sut.SelectedRecurrenceFrequency);
        Assert.Equal(2, _sut.RecurrenceInterval);
        Assert.Equal(new DateOnly(2026, 12, 31), DateOnly.FromDateTime(_sut.RecurrenceEndDate!.Value.DateTime));
        Assert.Single(_sut.ChecklistItems);
        Assert.Equal("Buy soil", _sut.ChecklistItems[0].Text);
        Assert.Single(_sut.Tags);
        Assert.Equal("Home", _sut.Tags[0].Name);
    }

    [Fact]
    public async Task LoadAsync_PopulatesType()
    {
        var task = MakeTask(t => t.Type = TaskType.Meeting);
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.LoadAsync(task.Id);

        Assert.Equal(TaskType.Meeting, _sut.Type);
    }

    [Fact]
    public async Task SaveAsync_PersistsTheSelectedType()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);
        _sut.Type = TaskType.Reminder;

        await _sut.SaveCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.UpdateTaskAsync(It.Is<TaskItem>(t => t.Type == TaskType.Reminder), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddChecklistItemAsync_OnSuccess_AddsARowAndClearsTheInput()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        var newItem = new ChecklistItem { TaskId = task.Id, Text = "Buy soil" };
        _checklistService.Setup(s => s.AddItemAsync(task.Id, "Buy soil", It.IsAny<CancellationToken>())).ReturnsAsync(newItem);
        _sut.NewChecklistItemText = "Buy soil";

        await _sut.AddChecklistItemCommand.ExecuteAsync(null);

        Assert.Single(_sut.ChecklistItems);
        Assert.Equal(string.Empty, _sut.NewChecklistItemText);
    }

    [Fact]
    public async Task AddTagAsync_OnSuccess_AddsAChipAndClearsTheInput()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        var tag = new Tag { Name = "Urgent" };
        _tagService.Setup(s => s.GetTagsForTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync([tag]);
        _sut.NewTagText = "Urgent";

        await _sut.AddTagCommand.ExecuteAsync(null);

        _tagService.Verify(s => s.AssignTagAsync(task.Id, "Urgent", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_sut.Tags);
        Assert.Equal("Urgent", _sut.Tags[0].Name);
        Assert.Equal(string.Empty, _sut.NewTagText);
    }

    [Fact]
    public async Task SaveAsTemplateAsync_OnBlankName_DoesNotCallTheService()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        await _sut.SaveAsTemplateCommand.ExecuteAsync(null);

        _templateService.Verify(s => s.SaveAsTemplateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsTemplateAsync_WithAName_CallsTheServiceAndClearsTheInput()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);
        _sut.TemplateNameToSave = "Weekly watering";

        await _sut.SaveAsTemplateCommand.ExecuteAsync(null);

        _templateService.Verify(s => s.SaveAsTemplateAsync(task.Id, "Weekly watering", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.TemplateNameToSave);
    }

    [Fact]
    public async Task SaveAsync_PersistsColorAndRecurrenceOntoTheTask()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        _sut.SelectColorCommand.Execute("#22C55E");
        _sut.SelectedRecurrenceFrequency = RecurrenceFrequency.Daily;
        _sut.RecurrenceInterval = 3;
        _sut.RecurrenceEndDate = new DateTimeOffset(new DateTime(2026, 9, 1));

        await _sut.SaveCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.UpdateTaskAsync(
            It.Is<TaskItem>(t =>
                t.ColorHex == "#22C55E" &&
                t.RecurrenceFrequency == RecurrenceFrequency.Daily &&
                t.RecurrenceInterval == 3 &&
                t.RecurrenceEndDate == new DateOnly(2026, 9, 1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesSameDayTasks_ExcludingSelfAndOwnSubtasks()
    {
        var task = MakeTask();
        var subtask = MakeTask(t => t.ParentTaskId = task.Id);
        var other = MakeTask(t => t.Title = "Unrelated task");
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _taskService.Setup(s => s.GetTasksForDateAsync(task.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync([task, subtask, other]);

        await _sut.LoadAsync(task.Id);

        Assert.Equal(["None", "Unrelated task"], _sut.SameDayTasks.Select(o => o.Title));
    }

    [Fact]
    public async Task LoadAsync_PopulatesSubtasksBlockersAndAttachments()
    {
        var task = MakeTask();
        var subtask = MakeTask(t => { t.Title = "Write changelog"; t.ParentTaskId = task.Id; });
        task.Subtasks.Add(subtask);
        var blocker = MakeTask(t => t.Title = "Finish QA");
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var attachment = new Attachment { TaskId = task.Id, FileName = "spec.pdf", StoredRelativePath = "attachments/x.pdf", FileSizeBytes = 2048 };
        _attachmentService.Setup(s => s.GetAttachmentsAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync([attachment]);
        _attachmentService.Setup(s => s.GetAbsolutePath(attachment)).Returns("/tmp/spec.pdf");

        await _sut.LoadAsync(task.Id);

        Assert.Single(_sut.Subtasks);
        Assert.Equal("Write changelog", _sut.Subtasks[0].Title);
        Assert.Single(_sut.Blockers);
        Assert.Equal("Finish QA", _sut.Blockers[0].Title);
        Assert.True(_sut.IsBlocked);
        Assert.Single(_sut.Attachments);
        Assert.Equal("spec.pdf", _sut.Attachments[0].FileName);
    }

    [Fact]
    public async Task AddSubtaskAsync_CreatesAChildTask_AndAddsARow()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        var newSubtask = MakeTask(t => { t.Title = "Write changelog"; t.ParentTaskId = task.Id; });
        _taskService.Setup(s => s.CreateTaskAsync(task.PlanDate, "Write changelog", null, TaskPriority.Medium, null, null, task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSubtask);
        _sut.NewSubtaskTitle = "Write changelog";

        await _sut.AddSubtaskCommand.ExecuteAsync(null);

        Assert.Single(_sut.Subtasks);
        Assert.Equal(string.Empty, _sut.NewSubtaskTitle);
    }

    [Fact]
    public async Task AddBlockerAsync_WithNoSelection_DoesNotCallTheService()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        await _sut.AddBlockerCommand.ExecuteAsync(null);

        _taskDependencyService.Verify(s => s.AddBlockerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddBlockerAsync_WithASelection_AddsItAndUpdatesIsBlocked()
    {
        var task = MakeTask();
        var blocker = MakeTask(t => t.Title = "Finish QA");
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _taskService.Setup(s => s.GetTasksForDateAsync(task.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync([task, blocker]);
        await _sut.LoadAsync(task.Id);

        var dependency = new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id };
        _taskDependencyService.Setup(s => s.GetBlockersAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync([dependency]);
        _sut.SelectedBlockerToAdd = _sut.SameDayTasks.Single(o => o.Id == blocker.Id);

        await _sut.AddBlockerCommand.ExecuteAsync(null);

        _taskDependencyService.Verify(s => s.AddBlockerAsync(task.Id, blocker.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_sut.Blockers);
        Assert.True(_sut.IsBlocked);
        Assert.Null(_sut.SelectedBlockerToAdd);
    }

    [Fact]
    public async Task AddAttachmentAsync_OnSuccess_AddsARow()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        var attachment = new Attachment { TaskId = task.Id, FileName = "spec.pdf", StoredRelativePath = "attachments/x.pdf" };
        _attachmentService.Setup(s => s.AddAttachmentAsync(task.Id, "/tmp/spec.pdf", It.IsAny<CancellationToken>())).ReturnsAsync(attachment);
        _attachmentService.Setup(s => s.GetAbsolutePath(attachment)).Returns("/data/attachments/x.pdf");

        await _sut.AddAttachmentAsync("/tmp/spec.pdf");

        Assert.Single(_sut.Attachments);
        Assert.Equal("spec.pdf", _sut.Attachments[0].FileName);
    }

    [Fact]
    public async Task AddAttachmentAsync_OnFailure_DoesNotAddARow()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);
        _attachmentService.Setup(s => s.AddAttachmentAsync(task.Id, "/tmp/missing.pdf", It.IsAny<CancellationToken>())).ReturnsAsync((Attachment?)null);

        await _sut.AddAttachmentAsync("/tmp/missing.pdf");

        Assert.Empty(_sut.Attachments);
    }

    [Fact]
    public async Task SaveAsync_PersistsTheSelectedParentTask()
    {
        var task = MakeTask();
        var parent = MakeTask(t => t.Title = "Ship release");
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _taskService.Setup(s => s.GetTasksForDateAsync(task.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync([task, parent]);
        await _sut.LoadAsync(task.Id);
        _sut.SelectedParentTask = _sut.SameDayTasks.Single(o => o.Id == parent.Id);

        await _sut.SaveCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.UpdateTaskAsync(It.Is<TaskItem>(t => t.ParentTaskId == parent.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleNotesPreviewCommand_FlipsIsNotesPreview()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync(task.Id);

        _sut.ToggleNotesPreviewCommand.Execute(null);
        Assert.True(_sut.IsNotesPreview);

        _sut.ToggleNotesPreviewCommand.Execute(null);
        Assert.False(_sut.IsNotesPreview);
    }
}
