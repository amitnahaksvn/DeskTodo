using System.Text;
using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class MassImportViewModelTests
{
    private readonly Mock<IMassImportService> _massImportService = new();

    private MassImportViewModel CreateSut()
    {
        _massImportService.Setup(s => s.GetMigrationRunsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new MassImportViewModel(_massImportService.Object, NullLogger<MassImportViewModel>.Instance);
    }

    private static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void FieldMappings_HasOneRowPerTaskExportRecordField()
    {
        var sut = CreateSut();

        Assert.Equal(["Title", "Description", "PlanDate", "DueDate", "Priority", "Category", "Notes", "IsCompleted", "IsPinned", "EstimatedMinutes"],
            sut.FieldMappings.Select(f => f.FieldName));
    }

    [Fact]
    public async Task LoadHeadersAsync_PopulatesHeaderOptions_AndAutoMapsExactNameMatches()
    {
        _massImportService.Setup(s => s.ReadCsvHeadersAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Title", "Task Deadline"]);
        var sut = CreateSut();

        await sut.LoadHeadersAsync(ToStream("Title,Task Deadline"), "tasks.csv");

        Assert.Equal("tasks.csv", sut.SelectedFileName);
        Assert.Contains("Title", sut.HeaderOptions);
        Assert.Contains("Task Deadline", sut.HeaderOptions);
        var titleRow = sut.FieldMappings.Single(f => f.FieldName == "Title");
        Assert.Equal("Title", titleRow.SelectedHeader);
        var dueDateRow = sut.FieldMappings.Single(f => f.FieldName == "DueDate");
        Assert.Null(dueDateRow.SelectedHeader);
    }

    [Fact]
    public void BuildColumnToFieldMapping_OnlyIncludesMappedRows()
    {
        var sut = CreateSut();
        sut.FieldMappings.Single(f => f.FieldName == "Title").SelectedHeader = "Task Name";
        sut.FieldMappings.Single(f => f.FieldName == "DueDate").SelectedHeader = "Deadline";

        var mapping = sut.BuildColumnToFieldMapping();

        Assert.Equal(2, mapping.Count);
        Assert.Equal("Title", mapping["Task Name"]);
        Assert.Equal("DueDate", mapping["Deadline"]);
    }

    [Fact]
    public async Task PreviewAsync_PopulatesCountsAndErrorLines()
    {
        var validRow = new MassImportRowPreview(2, new TaskExportRecord { Title = "Ship it", PlanDate = DateOnly.FromDateTime(DateTime.Today) }, [], false);
        var invalidRow = new MassImportRowPreview(3, null, ["Title is required."], false);
        _massImportService.Setup(s => s.PreviewAsync(It.IsAny<Stream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MassImportPreviewResult(2, [validRow, invalidRow]));
        var sut = CreateSut();

        await sut.PreviewAsync(ToStream("x"));

        Assert.Equal(2, sut.PreviewTotalRows);
        var errorLine = Assert.Single(sut.PreviewErrorLines);
        Assert.Contains("Row 3", errorLine);
    }

    [Fact]
    public async Task ImportAsync_OnCompletion_SetsAnInformativeStatusMessage()
    {
        _massImportService.Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationRun { SourceDescription = "tasks.csv", Status = MigrationStatus.Completed, TotalRecords = 5, ImportedCount = 4, SkippedCount = 1 });
        var sut = CreateSut();

        await sut.ImportAsync(ToStream("x"));

        Assert.Contains("Imported 4 of 5", sut.StatusMessage);
    }

    [Fact]
    public async Task ImportAsync_WhenValidationFails_SetsAnAbortedStatusMessage()
    {
        _massImportService.Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationRun { SourceDescription = "tasks.csv", Status = MigrationStatus.Failed, TotalRecords = 5 });
        var sut = CreateSut();

        await sut.ImportAsync(ToStream("x"));

        Assert.Contains("aborted", sut.StatusMessage);
    }

    [Fact]
    public async Task LoadAsync_FormatsMigrationRunSummaries()
    {
        var sut = CreateSut();
        var run = new MigrationRun { SourceDescription = "tasks.csv", Status = MigrationStatus.Completed, TotalRecords = 3, ImportedCount = 2, SkippedCount = 1, StartedAt = new DateTime(2026, 9, 2, 10, 0, 0) };
        _massImportService.Setup(s => s.GetMigrationRunsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([run]);

        await sut.LoadAsync();

        var summary = Assert.Single(sut.MigrationRunSummaries);
        Assert.Contains("tasks.csv", summary);
        Assert.Contains("2 imported", summary);
    }

    [Fact]
    public async Task ResetCommand_ClearsAllState()
    {
        _massImportService.Setup(s => s.ReadCsvHeadersAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(["Title"]);
        var sut = CreateSut();
        await sut.LoadHeadersAsync(ToStream("Title"), "tasks.csv");

        sut.ResetCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SelectedFileName);
        Assert.All(sut.FieldMappings, row => Assert.Null(row.SelectedHeader));
        Assert.Null(sut.PreviewTotalRows);
    }
}
