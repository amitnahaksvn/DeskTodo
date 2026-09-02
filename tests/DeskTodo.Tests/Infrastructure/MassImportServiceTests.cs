using System.Text;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.ImportExport;
using Moq;

namespace DeskTodo.Tests.Infrastructure;

public class MassImportServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IMigrationRunRepository> _migrationRunRepository = new();
    private readonly IDuplicateDetectionService _duplicateDetectionService = new DuplicateDetectionService();
    private readonly MassImportService _sut;

    public MassImportServiceTests()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut = new MassImportService(_taskService.Object, _categoryRepository.Object, _duplicateDetectionService, _migrationRunRepository.Object);
    }

    private static MemoryStream ToStream(string csv) => new(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task ReadCsvHeadersAsync_ReturnsTheFirstRow()
    {
        var stream = ToStream("Task Name,Deadline,Category\nShip it,2026-09-10,Work\n");

        var headers = await _sut.ReadCsvHeadersAsync(stream);

        Assert.Equal(["Task Name", "Deadline", "Category"], headers);
    }

    [Fact]
    public async Task PreviewAsync_MapsColumnsToTaskFieldsPerTheGivenMapping()
    {
        var stream = ToStream("Task Name,Deadline\nShip it,2026-09-10\n");
        var mapping = new Dictionary<string, string> { ["Task Name"] = "Title", ["Deadline"] = "DueDate" };

        var result = await _sut.PreviewAsync(stream, mapping);

        var row = Assert.Single(result.Rows);
        Assert.Equal("Ship it", row.Record!.Title);
        Assert.Equal(new DateTime(2026, 9, 10), row.Record.DueDate);
        Assert.Empty(row.ValidationErrors);
        Assert.False(result.HasValidationErrors);
    }

    [Fact]
    public async Task PreviewAsync_WhenTitleColumnIsUnmappedOrBlank_FlagsAValidationError()
    {
        var stream = ToStream("Task Name,Deadline\n,2026-09-10\n");
        var mapping = new Dictionary<string, string> { ["Task Name"] = "Title" };

        var result = await _sut.PreviewAsync(stream, mapping);

        var row = Assert.Single(result.Rows);
        Assert.Null(row.Record);
        Assert.NotEmpty(row.ValidationErrors);
        Assert.True(result.HasValidationErrors);
    }

    [Fact]
    public async Task PreviewAsync_FlagsARowAsDuplicateWhenAMatchingTaskAlreadyExists()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { PlanDate = today, Title = "Ship it" }]);
        var stream = ToStream("Task Name\nShip it\n");
        var mapping = new Dictionary<string, string> { ["Task Name"] = "Title" };

        var result = await _sut.PreviewAsync(stream, mapping);

        Assert.True(Assert.Single(result.Rows).IsDuplicate);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public async Task ImportAsync_WhenAnyRowFailsValidation_AbortsWithoutCreatingAnyTasks()
    {
        var stream = ToStream("Task Name\nShip it\n\n,\n");
        var mapping = new Dictionary<string, string> { ["Task Name"] = "Title" };

        var run = await _sut.ImportAsync(stream, mapping, "tasks.csv");

        Assert.Equal(MigrationStatus.Failed, run.Status);
        Assert.Equal(0, run.ImportedCount);
        _taskService.Verify(s => s.CreateTaskAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _migrationRunRepository.Verify(r => r.AddAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_CreatesATaskPerValidNonDuplicateRow_AndSkipsDuplicates()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { PlanDate = today, Title = "Already here" }]);
        _taskService.Setup(s => s.CreateTaskAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly planDate, string title, string? _, TaskPriority _, Guid? _, DateTime? _, Guid? _, CancellationToken _) => new TaskItem { PlanDate = planDate, Title = title });
        var stream = ToStream("Task Name\nAlready here\nShip it\n");
        var mapping = new Dictionary<string, string> { ["Task Name"] = "Title" };

        var run = await _sut.ImportAsync(stream, mapping, "tasks.csv");

        Assert.Equal(MigrationStatus.Completed, run.Status);
        Assert.Equal(1, run.ImportedCount);
        Assert.Equal(1, run.SkippedCount);
        Assert.Equal(2, run.TotalRecords);
        _taskService.Verify(s => s.CreateTaskAsync(It.IsAny<DateOnly>(), "Ship it", It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.CreateTaskAsync(It.IsAny<DateOnly>(), "Already here", It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMigrationRunsAsync_DelegatesToTheRepository()
    {
        await _sut.GetMigrationRunsAsync();

        _migrationRunRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
