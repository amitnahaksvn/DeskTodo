using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class RecurringProjectScheduleServiceTests
{
    private readonly Mock<IRecurringProjectScheduleRepository> _scheduleRepository = new();
    private readonly Mock<IProjectTemplateService> _projectTemplateService = new();
    private readonly RecurringProjectScheduleService _sut;

    public RecurringProjectScheduleServiceTests()
    {
        _sut = new RecurringProjectScheduleService(_scheduleRepository.Object, _projectTemplateService.Object);
    }

    private static ProjectTemplate MakeTemplate() => new() { Name = "Monthly Reporting Kit" };

    [Fact]
    public async Task CreateScheduleAsync_WhenTemplateMissing_ThrowsProjectTemplateNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _projectTemplateService.Setup(s => s.GetTemplateAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTemplate?)null);

        await Assert.ThrowsAsync<ProjectTemplateNotFoundException>(() =>
            _sut.CreateScheduleAsync("Monthly Reporting", missingId, "#4A90D9", ProjectRecurrenceFrequency.Monthly, DateOnly.FromDateTime(DateTime.Today), null));
    }

    [Fact]
    public async Task CreateScheduleAsync_SetsNextOccurrenceDateToStartDate()
    {
        var template = MakeTemplate();
        _projectTemplateService.Setup(s => s.GetTemplateAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        var startDate = new DateOnly(2026, 10, 1);

        var schedule = await _sut.CreateScheduleAsync("Monthly Reporting", template.Id, "#4A90D9", ProjectRecurrenceFrequency.Monthly, startDate, null);

        Assert.Equal(startDate, schedule.NextOccurrenceDate);
        Assert.True(schedule.IsActive);
        _scheduleRepository.Verify(r => r.AddAsync(schedule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateDueProjectsAsync_CreatesAProjectPerDueScheduleAndAdvancesItsNextOccurrenceDate()
    {
        var template = MakeTemplate();
        var occurrenceDate = new DateOnly(2026, 9, 1);
        var schedule = new RecurringProjectSchedule
        {
            Name = "Monthly Reporting",
            ProjectTemplateId = template.Id,
            ColorHex = "#4A90D9",
            Frequency = ProjectRecurrenceFrequency.Monthly,
            StartDate = occurrenceDate,
            NextOccurrenceDate = occurrenceDate,
        };
        var generatedProject = new Project { Name = "generated", ColorHex = "#4A90D9" };
        _scheduleRepository.Setup(r => r.GetDueAsync(occurrenceDate, It.IsAny<CancellationToken>())).ReturnsAsync([schedule]);
        _projectTemplateService.Setup(s => s.CreateProjectFromTemplateAsync(
                schedule.ProjectTemplateId, It.IsAny<string>(), schedule.ColorHex, occurrenceDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedProject);

        var results = await _sut.GenerateDueProjectsAsync(occurrenceDate);

        Assert.Single(results);
        Assert.Equal(generatedProject.Id, results[0].Id);
        Assert.Contains(generatedProject.Id, schedule.GeneratedProjectIds);
        Assert.Equal(occurrenceDate.AddMonths(1), schedule.NextOccurrenceDate);
        _scheduleRepository.Verify(r => r.UpdateAsync(schedule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ProjectRecurrenceFrequency.Weekly, 2026, 9, 8)]
    [InlineData(ProjectRecurrenceFrequency.Monthly, 2026, 10, 1)]
    [InlineData(ProjectRecurrenceFrequency.Quarterly, 2026, 12, 1)]
    [InlineData(ProjectRecurrenceFrequency.Yearly, 2027, 9, 1)]
    public async Task GenerateDueProjectsAsync_AdvancesByTheConfiguredFrequency(ProjectRecurrenceFrequency frequency, int year, int month, int day)
    {
        var template = MakeTemplate();
        var occurrenceDate = new DateOnly(2026, 9, 1);
        var schedule = new RecurringProjectSchedule
        {
            Name = "Schedule",
            ProjectTemplateId = template.Id,
            ColorHex = "#4A90D9",
            Frequency = frequency,
            StartDate = occurrenceDate,
            NextOccurrenceDate = occurrenceDate,
        };
        _scheduleRepository.Setup(r => r.GetDueAsync(occurrenceDate, It.IsAny<CancellationToken>())).ReturnsAsync([schedule]);
        _projectTemplateService.Setup(s => s.CreateProjectFromTemplateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Name = "x", ColorHex = "#4A90D9" });

        await _sut.GenerateDueProjectsAsync(occurrenceDate);

        Assert.Equal(new DateOnly(year, month, day), schedule.NextOccurrenceDate);
    }

    [Fact]
    public async Task SetActiveAsync_WhenScheduleMissing_ThrowsRecurringProjectScheduleNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _scheduleRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((RecurringProjectSchedule?)null);

        await Assert.ThrowsAsync<RecurringProjectScheduleNotFoundException>(() => _sut.SetActiveAsync(missingId, false));
    }
}
