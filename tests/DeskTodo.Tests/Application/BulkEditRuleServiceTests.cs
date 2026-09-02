using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Application;

public class BulkEditRuleServiceTests
{
    private readonly Mock<IBulkEditRuleRepository> _ruleRepository = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ITagService> _tagService = new();
    private readonly BulkEditRuleService _sut;

    public BulkEditRuleServiceTests()
    {
        _sut = new BulkEditRuleService(_ruleRepository.Object, _taskService.Object, _tagService.Object, NullLogger<BulkEditRuleService>.Instance);
    }

    private static TaskItem MakeTask(TaskPriority priority = TaskPriority.High, DateTime? dueDate = null) =>
        new() { PlanDate = DateOnly.FromDateTime(DateTime.Today), Title = "Task", Priority = priority, DueDate = dueDate ?? DateTime.Today.AddDays(-1) };

    [Fact]
    public async Task CreateRuleAsync_TrimsNameAndPersistsConditionsAndActions()
    {
        var conditions = new[] { new BulkEditCondition { Field = BulkEditConditionField.Priority, Value = "High" } };
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.SetPriority, Value = "Critical" } };

        var rule = await _sut.CreateRuleAsync("  Escalate  ", conditions, actions);

        Assert.Equal("Escalate", rule.Name);
        _ruleRepository.Verify(r => r.AddAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreviewAsync_ReturnsOnlyMatchingTasks()
    {
        var matching = MakeTask(priority: TaskPriority.High);
        var nonMatching = MakeTask(priority: TaskPriority.Low);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([matching, nonMatching]);
        var conditions = new[] { new BulkEditCondition { Field = BulkEditConditionField.Priority, Value = "High" } };

        var result = await _sut.PreviewAsync(conditions);

        var found = Assert.Single(result);
        Assert.Equal(matching.Id, found.Id);
    }

    [Fact]
    public async Task ApplyAsync_SetPriority_UpdatesEveryMatchingTask()
    {
        var task = MakeTask(priority: TaskPriority.High);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var conditions = new[] { new BulkEditCondition { Field = BulkEditConditionField.Priority, Value = "High" } };
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.SetPriority, Value = "Critical" } };

        var count = await _sut.ApplyAsync(conditions, actions);

        Assert.Equal(1, count);
        Assert.Equal(TaskPriority.Critical, task.Priority);
        _taskService.Verify(s => s.UpdateTaskAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_AddTag_DelegatesToTagService()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.AddTag, Value = "overdue" } };

        await _sut.ApplyAsync([], actions);

        _tagService.Verify(s => s.AssignTagAsync(task.Id, "overdue", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_MoveToProject_SetsProjectIdAndUpdates()
    {
        var task = MakeTask();
        var projectId = Guid.NewGuid();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.MoveToProject, Value = projectId.ToString() } };

        await _sut.ApplyAsync([], actions);

        Assert.Equal(projectId, task.ProjectId);
        _taskService.Verify(s => s.UpdateTaskAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_MarkCompleted_CallsCompleteTaskAsync()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.MarkCompleted } };

        await _sut.ApplyAsync([], actions);

        _taskService.Verify(s => s.CompleteTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_Delete_CallsDeleteTaskAsync()
    {
        var task = MakeTask();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.Delete } };

        await _sut.ApplyAsync([], actions);

        _taskService.Verify(s => s.DeleteTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenOneActionThrows_StillProcessesTheRestOfTheBatch()
    {
        var failing = MakeTask();
        var succeeding = MakeTask();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([failing, succeeding]);
        _taskService.Setup(s => s.CompleteTaskAsync(failing.Id, It.IsAny<CancellationToken>())).ThrowsAsync(new TaskBlockedException(failing.Id));
        var actions = new[] { new BulkEditAction { Type = BulkEditActionType.MarkCompleted } };

        var count = await _sut.ApplyAsync([], actions);

        Assert.Equal(2, count);
        _taskService.Verify(s => s.CompleteTaskAsync(succeeding.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyRuleAsync_WhenRuleMissing_ThrowsBulkEditRuleNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _ruleRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((BulkEditRule?)null);

        await Assert.ThrowsAsync<BulkEditRuleNotFoundException>(() => _sut.ApplyRuleAsync(missingId));
    }

    [Fact]
    public async Task ApplyRuleAsync_LoadsTheRuleAndAppliesItsConditionsAndActions()
    {
        var task = MakeTask(priority: TaskPriority.High);
        var rule = new BulkEditRule
        {
            Name = "Escalate",
            Conditions = [new BulkEditCondition { Field = BulkEditConditionField.Priority, Value = "High" }],
            Actions = [new BulkEditAction { Type = BulkEditActionType.SetPriority, Value = "Critical" }],
        };
        _ruleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>())).ReturnsAsync(rule);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var count = await _sut.ApplyRuleAsync(rule.Id);

        Assert.Equal(1, count);
        Assert.Equal(TaskPriority.Critical, task.Priority);
    }
}
