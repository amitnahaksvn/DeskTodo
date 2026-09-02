using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class ProjectTemplatesViewModelTests
{
    private readonly Mock<IProjectTemplateService> _templateService = new();
    private readonly Mock<IRecurringProjectScheduleService> _scheduleService = new();

    private ProjectTemplatesViewModel CreateSut() =>
        new(_templateService.Object, _scheduleService.Object, NullLogger<ProjectTemplatesViewModel>.Instance);

    [Fact]
    public void ParseTaskItemsText_ParsesEachField()
    {
        var items = ProjectTemplatesViewModel.ParseTaskItemsText("Requirements | High | 1 | 1\nDevelopment | Medium | 2 | 6");

        Assert.Equal(2, items.Count);
        Assert.Equal("Requirements", items[0].Title);
        Assert.Equal(TaskPriority.High, items[0].Priority);
        Assert.Equal(1, items[0].DayOffsetStart);
        Assert.Equal(1, items[0].DurationDays);
        Assert.Equal("Development", items[1].Title);
        Assert.Equal(6, items[1].DurationDays);
    }

    [Fact]
    public void ParseTaskItemsText_SkipsBlankLines_AndDefaultsMissingFields()
    {
        var items = ProjectTemplatesViewModel.ParseTaskItemsText("\nJust a title\n  \n");

        var item = Assert.Single(items);
        Assert.Equal("Just a title", item.Title);
        Assert.Equal(TaskPriority.Medium, item.Priority);
        Assert.Equal(1, item.DayOffsetStart);
        Assert.Equal(1, item.DurationDays);
    }

    [Fact]
    public void ParseTaskItemsText_WithInvalidPriority_FallsBackToMedium()
    {
        var items = ProjectTemplatesViewModel.ParseTaskItemsText("Task | NotAPriority | 1 | 1");

        Assert.Equal(TaskPriority.Medium, items[0].Priority);
    }

    [Fact]
    public void ParseMilestoneItemsText_ParsesTitleAndDayOffset()
    {
        var items = ProjectTemplatesViewModel.ParseMilestoneItemsText("Code Complete | 7\nRelease");

        Assert.Equal(2, items.Count);
        Assert.Equal("Code Complete", items[0].Title);
        Assert.Equal(7, items[0].DayOffset);
        Assert.Equal("Release", items[1].Title);
        Assert.Equal(1, items[1].DayOffset);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenNameIsBlank_SetsErrorMessage_WithoutCallingTheService()
    {
        var sut = CreateSut();
        sut.NewTemplateName = "  ";
        sut.TaskItemsText = "Task | Medium | 1 | 1";

        await sut.CreateTemplateCommand.ExecuteAsync(null);

        Assert.Equal("Enter a name for the template.", sut.ErrorMessage);
        _templateService.Verify(s => s.CreateTemplateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<ProjectTemplateTaskItem>>(), It.IsAny<IReadOnlyList<ProjectTemplateMilestoneItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenNoTaskItemsParse_SetsErrorMessage()
    {
        var sut = CreateSut();
        sut.NewTemplateName = "Kit";
        sut.TaskItemsText = string.Empty;

        await sut.CreateTemplateCommand.ExecuteAsync(null);

        Assert.Contains("at least one task", sut.ErrorMessage);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithValidInput_CallsTheServiceAndClearsTheForm()
    {
        var sut = CreateSut();
        sut.NewTemplateName = "  Release Kit  ";
        sut.TaskItemsText = "Requirements | High | 1 | 1";
        sut.MilestoneItemsText = "Code Complete | 7";
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _scheduleService.Setup(s => s.GetSchedulesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await sut.CreateTemplateCommand.ExecuteAsync(null);

        _templateService.Verify(s => s.CreateTemplateAsync("Release Kit", It.IsAny<string?>(),
            It.Is<IReadOnlyList<ProjectTemplateTaskItem>>(items => items.Count == 1),
            It.Is<IReadOnlyList<ProjectTemplateMilestoneItem>>(items => items.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, sut.NewTemplateName);
        Assert.Equal(string.Empty, sut.TaskItemsText);
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_WhenNoTemplateSelected_SetsErrorMessage()
    {
        var sut = CreateSut();

        await sut.CreateProjectFromTemplateCommand.ExecuteAsync(null);

        Assert.Equal("Pick a template first.", sut.ErrorMessage);
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_WithATemplateSelected_ShowsTheCreatedProjectName()
    {
        var sut = CreateSut();
        var option = new ProjectTemplateOption(Guid.NewGuid(), "Release Kit");
        sut.SelectedTemplateForProject = option;
        _templateService.Setup(s => s.CreateProjectFromTemplateAsync(option.Id, "Release Kit", It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Name = "Release Kit", ColorHex = "#4A90D9" });

        await sut.CreateProjectFromTemplateCommand.ExecuteAsync(null);

        Assert.Contains("Release Kit", sut.StatusMessage);
    }

    [Fact]
    public async Task CreateScheduleAsync_WhenNoTemplateSelected_SetsErrorMessage()
    {
        var sut = CreateSut();
        sut.NewScheduleName = "Monthly Reporting";

        await sut.CreateScheduleCommand.ExecuteAsync(null);

        Assert.Equal("Pick a template for the schedule.", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_MapsScheduleRowsWithTheirTemplateName()
    {
        var template = new ProjectTemplate { Name = "Release Kit" };
        var schedule = new RecurringProjectSchedule
        {
            Name = "Monthly Reporting",
            ProjectTemplateId = template.Id,
            ColorHex = "#4A90D9",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            NextOccurrenceDate = DateOnly.FromDateTime(DateTime.Today),
        };
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([template]);
        _scheduleService.Setup(s => s.GetSchedulesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([schedule]);
        var sut = CreateSut();

        await sut.LoadAsync();

        var row = Assert.Single(sut.Schedules);
        Assert.Equal("Release Kit", row.TemplateName);
        Assert.Equal("Pause", row.ToggleLabel);
    }
}
