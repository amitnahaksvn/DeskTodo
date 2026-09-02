using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class ProjectTemplateServiceTests
{
    private readonly Mock<IProjectTemplateRepository> _templateRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IMilestoneRepository> _milestoneRepository = new();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly ProjectTemplateService _sut;

    public ProjectTemplateServiceTests()
    {
        _sut = new ProjectTemplateService(_templateRepository.Object, _projectRepository.Object, _milestoneRepository.Object, _taskRepository.Object);
    }

    [Fact]
    public async Task CreateTemplateAsync_TrimsNameAndPersistsItems()
    {
        var taskItems = new[] { new ProjectTemplateTaskItem { Title = "Requirements", DayOffsetStart = 1, DurationDays = 1 } };
        var milestoneItems = new[] { new ProjectTemplateMilestoneItem { Title = "Release", DayOffset = 5 } };

        var template = await _sut.CreateTemplateAsync("  Software Release Kit  ", "  desc  ", taskItems, milestoneItems);

        Assert.Equal("Software Release Kit", template.Name);
        Assert.Equal("desc", template.Description);
        Assert.Equal(taskItems, template.TaskItems);
        Assert.Equal(milestoneItems, template.MilestoneItems);
        _templateRepository.Verify(r => r.AddAsync(template, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenMissing_ThrowsProjectTemplateNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _templateRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTemplate?)null);

        await Assert.ThrowsAsync<ProjectTemplateNotFoundException>(() => _sut.UpdateTemplateAsync(missingId, "X", null, [], []));
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_WhenTemplateMissing_ThrowsProjectTemplateNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _templateRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTemplate?)null);

        await Assert.ThrowsAsync<ProjectTemplateNotFoundException>(() =>
            _sut.CreateProjectFromTemplateAsync(missingId, "New Project", "#4A90D9", DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_CreatesProjectTasksAndMilestonesWithComputedDates()
    {
        var template = new ProjectTemplate
        {
            Name = "Software Release Kit",
            TaskItems =
            [
                new ProjectTemplateTaskItem { Title = "Requirements", Priority = TaskPriority.High, DayOffsetStart = 1, DurationDays = 1 },
                new ProjectTemplateTaskItem { Title = "Development", Priority = TaskPriority.Medium, DayOffsetStart = 2, DurationDays = 6 },
            ],
            MilestoneItems =
            [
                new ProjectTemplateMilestoneItem { Title = "Code Complete", DayOffset = 7 },
            ],
        };
        _templateRepository.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var startDate = new DateOnly(2026, 9, 1);

        var project = await _sut.CreateProjectFromTemplateAsync(template.Id, "Q4 Release", "#4A90D9", startDate);

        Assert.Equal("Q4 Release", project.Name);
        _projectRepository.Verify(r => r.AddAsync(It.Is<Project>(p => p.Id == project.Id), It.IsAny<CancellationToken>()), Times.Once);
        _milestoneRepository.Verify(r => r.AddAsync(
            It.Is<Milestone>(m => m.Title == "Code Complete" && m.TargetDate == startDate.AddDays(6) && m.ProjectId == project.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.Title == "Requirements" && t.PlanDate == startDate && t.DueDate == startDate.ToDateTime(TimeOnly.MinValue) && t.ProjectId == project.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.Title == "Development" && t.PlanDate == startDate.AddDays(1) && t.DueDate == startDate.AddDays(6).ToDateTime(TimeOnly.MinValue)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
