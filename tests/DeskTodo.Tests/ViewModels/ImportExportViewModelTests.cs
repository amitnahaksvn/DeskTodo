using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class ImportExportViewModelTests
{
    private static ImportExportViewModel CreateSut(
        Mock<ITaskService>? taskService = null,
        Mock<ICategoryRepository>? categoryRepository = null,
        Mock<ITaskExportService>? exportService = null,
        Mock<ITaskImportService>? importService = null)
    {
        if (categoryRepository is null)
        {
            categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Category>());
        }

        return new ImportExportViewModel(
            (taskService ?? new Mock<ITaskService>()).Object,
            categoryRepository.Object,
            (exportService ?? new Mock<ITaskExportService>()).Object,
            (importService ?? new Mock<ITaskImportService>()).Object,
            NullLogger<ImportExportViewModel>.Instance);
    }

    [Fact]
    public async Task ExportToAsync_MapsTasksToRecords_AndCallsExportService()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Work", ColorHex = "#3B82F6" };
        var task = new TaskItem
        {
            Title = "Ship the release",
            PlanDate = new DateOnly(2026, 7, 31),
            Priority = TaskPriority.High,
            CategoryId = category.Id,
            Category = category,
            Notes = "Double-check the changelog",
        };
        var taskService = new Mock<ITaskService>();
        taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var exportService = new Mock<ITaskExportService>();
        var sut = CreateSut(taskService: taskService, exportService: exportService);
        sut.SelectedExportFormat = TaskExportFormat.Json;

        using var stream = new MemoryStream();
        await sut.ExportToAsync(stream);

        exportService.Verify(e => e.ExportAsync(
            It.Is<IReadOnlyList<TaskExportRecord>>(records =>
                records.Count == 1 &&
                records[0].Title == "Ship the release" &&
                records[0].Priority == "High" &&
                records[0].Category == "Work" &&
                records[0].Notes == "Double-check the changelog"),
            TaskExportFormat.Json,
            stream,
            It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("Exported 1 task.", sut.StatusMessage);
    }

    [Fact]
    public async Task ImportFromAsync_CreatesATaskPerRecord_AndMatchesCategoryByNameCaseInsensitively()
    {
        var categoryId = Guid.NewGuid();
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = categoryId, Name = "Work", ColorHex = "#3B82F6" }]);
        var taskService = new Mock<ITaskService>();
        taskService.Setup(s => s.CreateTaskAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly planDate, string title, string? _, TaskPriority _, Guid? _, DateTime? _, CancellationToken _) => new TaskItem { PlanDate = planDate, Title = title });
        var importService = new Mock<ITaskImportService>();
        importService.Setup(i => i.ImportAsync(It.IsAny<Stream>(), TaskImportFormat.Csv, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskExportRecord { Title = "Imported task", PlanDate = new DateOnly(2026, 7, 31), Category = "work", Priority = "High" }]);
        var sut = CreateSut(taskService: taskService, categoryRepository: categoryRepository, importService: importService);

        using var stream = new MemoryStream();
        await sut.ImportFromAsync(stream, TaskImportFormat.Csv);

        taskService.Verify(s => s.CreateTaskAsync(
            new DateOnly(2026, 7, 31), "Imported task", null, TaskPriority.High, categoryId, null, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("Imported 1 of 1 task.", sut.StatusMessage);
    }

    [Fact]
    public async Task ImportFromAsync_AppliesNotesEstimatedMinutesCompletedAndPinned()
    {
        var createdTask = new TaskItem { PlanDate = new DateOnly(2026, 7, 31), Title = "Imported task" };
        var taskService = new Mock<ITaskService>();
        taskService.Setup(s => s.CreateTaskAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);
        var importService = new Mock<ITaskImportService>();
        importService.Setup(i => i.ImportAsync(It.IsAny<Stream>(), TaskImportFormat.Csv, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskExportRecord
            {
                Title = "Imported task",
                PlanDate = new DateOnly(2026, 7, 31),
                Notes = "Some notes",
                EstimatedMinutes = 45,
                IsCompleted = true,
                IsPinned = true,
            }]);
        var sut = CreateSut(taskService: taskService, importService: importService);

        using var stream = new MemoryStream();
        await sut.ImportFromAsync(stream, TaskImportFormat.Csv);

        taskService.Verify(s => s.UpdateTaskAsync(
            It.Is<TaskItem>(t => t.Notes == "Some notes" && t.EstimatedMinutes == 45), It.IsAny<CancellationToken>()),
            Times.Once);
        taskService.Verify(s => s.CompleteTaskAsync(createdTask.Id, It.IsAny<CancellationToken>()), Times.Once);
        taskService.Verify(s => s.PinTaskAsync(createdTask.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromAsync_WhenOneRecordFailsToCreate_SkipsItAndContinuesWithTheRest()
    {
        var taskService = new Mock<ITaskService>();
        taskService.SetupSequence(s => s.CreateTaskAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .ReturnsAsync(new TaskItem { PlanDate = new DateOnly(2026, 7, 31), Title = "Second task" });
        var importService = new Mock<ITaskImportService>();
        importService.Setup(i => i.ImportAsync(It.IsAny<Stream>(), TaskImportFormat.Csv, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TaskExportRecord { Title = "First task (will fail)", PlanDate = new DateOnly(2026, 7, 31) },
                new TaskExportRecord { Title = "Second task", PlanDate = new DateOnly(2026, 7, 31) },
            ]);
        var sut = CreateSut(taskService: taskService, importService: importService);

        using var stream = new MemoryStream();
        await sut.ImportFromAsync(stream, TaskImportFormat.Csv);

        Assert.Equal("Imported 1 of 2 tasks.", sut.StatusMessage);
    }

    [Theory]
    [InlineData(TaskExportFormat.Csv, "csv")]
    [InlineData(TaskExportFormat.Json, "json")]
    [InlineData(TaskExportFormat.Markdown, "md")]
    [InlineData(TaskExportFormat.Excel, "xlsx")]
    public void SelectedExportExtension_MatchesTheSelectedFormat(TaskExportFormat format, string expectedExtension)
    {
        var sut = CreateSut();

        sut.SelectedExportFormat = format;

        Assert.Equal(expectedExtension, sut.SelectedExportExtension);
    }
}
