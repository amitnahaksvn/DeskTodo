using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskTemplateServiceTests
{
    private readonly Mock<ITaskTemplateRepository> _templateRepository = new();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IChecklistRepository> _checklistRepository = new();
    private readonly TaskTemplateService _sut;

    public TaskTemplateServiceTests()
    {
        _sut = new TaskTemplateService(_templateRepository.Object, _taskRepository.Object, _checklistRepository.Object);
    }

    [Fact]
    public async Task SaveAsTemplateAsync_CopiesFieldsAndChecklistFromTheTask()
    {
        var task = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            Title = "Groceries",
            Notes = "Weekly run",
        };
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "Second", Order = 1 });
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "First", Order = 0 });
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var template = await _sut.SaveAsTemplateAsync(task.Id, "Weekly grocery run");

        Assert.Equal("Weekly grocery run", template.Name);
        Assert.Equal("Groceries", template.TaskTitle);
        Assert.Equal("Weekly run", template.Notes);
        Assert.Equal(["First", "Second"], template.ChecklistItems);
        _templateRepository.Verify(r => r.AddAsync(template, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsTemplateAsync_WhenTaskMissing_ThrowsTaskNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _taskRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => _sut.SaveAsTemplateAsync(missingId, "X"));
    }

    [Fact]
    public async Task CreateTaskFromTemplateAsync_SeedsATaskAndItsChecklist()
    {
        var planDate = new DateOnly(2026, 7, 28);
        var template = new TaskTemplate
        {
            Name = "Sprint prep",
            TaskTitle = "Sprint planning prep",
            ChecklistItems = ["Review backlog", "Draft agenda"],
        };
        _templateRepository.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var task = await _sut.CreateTaskFromTemplateAsync(template.Id, planDate);

        Assert.NotNull(task);
        Assert.Equal("Sprint planning prep", task.Title);
        Assert.Equal(0, task.DayOrder);
        _taskRepository.Verify(r => r.AddAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        _checklistRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<ChecklistItem>>(items => items.Select(i => i.Text).SequenceEqual(new[] { "Review backlog", "Draft agenda" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskFromTemplateAsync_WhenTemplateMissing_ReturnsNull()
    {
        _templateRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskTemplate?)null);

        var task = await _sut.CreateTaskFromTemplateAsync(Guid.NewGuid(), new DateOnly(2026, 7, 28));

        Assert.Null(task);
        _taskRepository.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTemplateAsync_DelegatesToRepository()
    {
        var templateId = Guid.NewGuid();

        await _sut.DeleteTemplateAsync(templateId);

        _templateRepository.Verify(r => r.DeleteAsync(templateId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
