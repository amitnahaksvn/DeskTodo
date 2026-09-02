using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using DeskTodo.Infrastructure.ImportExport;
using Moq;

namespace DeskTodo.Tests.Infrastructure;

public class ExportProfileServiceTests
{
    private readonly Mock<IExportProfileRepository> _profileRepository = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ITaskExportService> _taskExportService = new();
    private readonly ExportProfileService _sut;

    public ExportProfileServiceTests()
    {
        _sut = new ExportProfileService(_profileRepository.Object, _taskService.Object, _taskExportService.Object);
    }

    private static TaskItem MakeTask(DateOnly planDate, Guid? projectId = null, string title = "Task") =>
        new() { PlanDate = planDate, Title = title, ProjectId = projectId };

    [Fact]
    public async Task CreateProfileAsync_TrimsNameAndPersistsConfiguration()
    {
        var projectId = Guid.NewGuid();

        var profile = await _sut.CreateProfileAsync("  Weekly Report  ", ExportFormat.Csv, projectId, ExportDateRange.ThisWeek);

        Assert.Equal("Weekly Report", profile.Name);
        Assert.Equal(projectId, profile.ProjectId);
        _profileRepository.Verify(r => r.AddAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunProfileAsync_WhenProfileMissing_ThrowsExportProfileNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _profileRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((ExportProfile?)null);

        await Assert.ThrowsAsync<ExportProfileNotFoundException>(() => _sut.RunProfileAsync(missingId, new MemoryStream()));
    }

    [Fact]
    public async Task RunProfileAsync_FiltersByProject()
    {
        var projectId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var matching = MakeTask(today, projectId, "In project");
        var other = MakeTask(today, Guid.NewGuid(), "Other project");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([matching, other]);
        var profile = new ExportProfile { Name = "P", ProjectId = projectId, DateRange = ExportDateRange.All };
        _profileRepository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        IReadOnlyList<TaskExportRecord>? captured = null;
        _taskExportService.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<TaskExportRecord>>(), It.IsAny<TaskExportFormat>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<TaskExportRecord>, TaskExportFormat, Stream, CancellationToken>((records, _, _, _) => captured = records)
            .Returns(Task.CompletedTask);

        var count = await _sut.RunProfileAsync(profile.Id, new MemoryStream());

        Assert.Equal(1, count);
        Assert.Equal("In project", Assert.Single(captured!).Title);
    }

    [Theory]
    [InlineData(ExportDateRange.Today)]
    [InlineData(ExportDateRange.ThisWeek)]
    [InlineData(ExportDateRange.ThisMonth)]
    [InlineData(ExportDateRange.All)]
    public async Task RunProfileAsync_IncludesTodaysTaskUnderEveryRange(ExportDateRange range)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var task = MakeTask(today);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var profile = new ExportProfile { Name = "P", DateRange = range };
        _profileRepository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _taskExportService.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<TaskExportRecord>>(), It.IsAny<TaskExportFormat>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var count = await _sut.RunProfileAsync(profile.Id, new MemoryStream());

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunProfileAsync_Today_ExcludesATaskFromYesterday()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var task = MakeTask(yesterday);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var profile = new ExportProfile { Name = "P", DateRange = ExportDateRange.Today };
        _profileRepository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _taskExportService.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<TaskExportRecord>>(), It.IsAny<TaskExportFormat>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var count = await _sut.RunProfileAsync(profile.Id, new MemoryStream());

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RunProfileAsync_ThisMonth_ExcludesATaskFromLastMonth()
    {
        var lastMonth = DateOnly.FromDateTime(DateTime.Today).AddMonths(-1);
        var task = MakeTask(lastMonth);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var profile = new ExportProfile { Name = "P", DateRange = ExportDateRange.ThisMonth };
        _profileRepository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _taskExportService.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<TaskExportRecord>>(), It.IsAny<TaskExportFormat>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var count = await _sut.RunProfileAsync(profile.Id, new MemoryStream());

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RunProfileAsync_TranslatesDomainFormatToTaskExportFormat()
    {
        var task = MakeTask(DateOnly.FromDateTime(DateTime.Today));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var profile = new ExportProfile { Name = "P", Format = ExportFormat.Markdown };
        _profileRepository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        TaskExportFormat? capturedFormat = null;
        _taskExportService.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<TaskExportRecord>>(), It.IsAny<TaskExportFormat>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<TaskExportRecord>, TaskExportFormat, Stream, CancellationToken>((_, format, _, _) => capturedFormat = format)
            .Returns(Task.CompletedTask);

        await _sut.RunProfileAsync(profile.Id, new MemoryStream());

        Assert.Equal(TaskExportFormat.Markdown, capturedFormat);
    }
}
