using ClosedXML.Excel;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Infrastructure.ImportExport;

namespace DeskTodo.Tests.Infrastructure;

public class TaskExportImportServiceTests
{
    private static TaskExportRecord CreateRecord(
        string title = "Buy groceries",
        string? description = null,
        DateOnly? planDate = null,
        DateTime? dueDate = null,
        string priority = "Medium",
        string? category = null,
        string? notes = null,
        bool isCompleted = false,
        bool isPinned = false,
        int? estimatedMinutes = null) => new()
    {
        Title = title,
        Description = description,
        PlanDate = planDate ?? new DateOnly(2026, 7, 31),
        DueDate = dueDate,
        Priority = priority,
        Category = category,
        Notes = notes,
        IsCompleted = isCompleted,
        IsPinned = isPinned,
        EstimatedMinutes = estimatedMinutes,
    };

    [Fact]
    public async Task Csv_ExportThenImport_RoundTripsPlainFields()
    {
        var records = new List<TaskExportRecord>
        {
            CreateRecord(title: "Task A", category: "Work", priority: "High", isCompleted: true, estimatedMinutes: 30),
            CreateRecord(title: "Task B", dueDate: new DateTime(2026, 8, 1, 9, 0, 0), isPinned: true),
        };
        var exportService = new TaskExportService();
        var importService = new TaskImportService();
        using var stream = new MemoryStream();

        await exportService.ExportAsync(records, TaskExportFormat.Csv, stream);
        stream.Position = 0;
        var imported = await importService.ImportAsync(stream, TaskImportFormat.Csv);

        Assert.Equal(2, imported.Count);
        Assert.Equal("Task A", imported[0].Title);
        Assert.Equal("Work", imported[0].Category);
        Assert.Equal("High", imported[0].Priority);
        Assert.True(imported[0].IsCompleted);
        Assert.Equal(30, imported[0].EstimatedMinutes);
        Assert.Equal("Task B", imported[1].Title);
        Assert.Equal(new DateTime(2026, 8, 1, 9, 0, 0), imported[1].DueDate);
        Assert.True(imported[1].IsPinned);
    }

    [Fact]
    public async Task Csv_ExportThenImport_RoundTripsFieldsContainingCommasQuotesAndNewlines()
    {
        var records = new List<TaskExportRecord>
        {
            CreateRecord(
                title: "Task, with a comma",
                notes: "Line one\nLine two, with a comma and a \"quoted\" word",
                description: "Has \"quotes\" too"),
        };
        var exportService = new TaskExportService();
        var importService = new TaskImportService();
        using var stream = new MemoryStream();

        await exportService.ExportAsync(records, TaskExportFormat.Csv, stream);
        stream.Position = 0;
        var imported = await importService.ImportAsync(stream, TaskImportFormat.Csv);

        Assert.Single(imported);
        Assert.Equal("Task, with a comma", imported[0].Title);
        Assert.Equal("Line one\nLine two, with a comma and a \"quoted\" word", imported[0].Notes);
        Assert.Equal("Has \"quotes\" too", imported[0].Description);
    }

    [Fact]
    public async Task Csv_Import_SkipsRowsWithNoTitle()
    {
        const string csv = "Title,Description,PlanDate,DueDate,Priority,Category,Notes,IsCompleted,IsPinned,EstimatedMinutes\n,Description only,2026-07-31,,Medium,,,False,False,\nReal task,,2026-07-31,,Medium,,,False,False,\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var importService = new TaskImportService();

        var imported = await importService.ImportAsync(stream, TaskImportFormat.Csv);

        Assert.Single(imported);
        Assert.Equal("Real task", imported[0].Title);
    }

    [Fact]
    public async Task Json_ExportThenImport_RoundTripsAllFields()
    {
        var records = new List<TaskExportRecord>
        {
            CreateRecord(title: "Task A", description: "Desc", category: "Personal", notes: "Some notes", isCompleted: true, isPinned: true, estimatedMinutes: 45, dueDate: new DateTime(2026, 8, 2, 10, 30, 0)),
        };
        var exportService = new TaskExportService();
        var importService = new TaskImportService();
        using var stream = new MemoryStream();

        await exportService.ExportAsync(records, TaskExportFormat.Json, stream);
        stream.Position = 0;
        var imported = await importService.ImportAsync(stream, TaskImportFormat.Json);

        Assert.Single(imported);
        var record = imported[0];
        Assert.Equal("Task A", record.Title);
        Assert.Equal("Desc", record.Description);
        Assert.Equal("Personal", record.Category);
        Assert.Equal("Some notes", record.Notes);
        Assert.True(record.IsCompleted);
        Assert.True(record.IsPinned);
        Assert.Equal(45, record.EstimatedMinutes);
        Assert.Equal(new DateTime(2026, 8, 2, 10, 30, 0), record.DueDate);
    }

    [Fact]
    public async Task Markdown_Export_GroupsByDateAndShowsCompletionCheckboxes()
    {
        var records = new List<TaskExportRecord>
        {
            CreateRecord(title: "Done task", planDate: new DateOnly(2026, 7, 31), isCompleted: true),
            CreateRecord(title: "Open task", planDate: new DateOnly(2026, 7, 31), priority: "High", category: "Work"),
        };
        var exportService = new TaskExportService();
        using var stream = new MemoryStream();

        await exportService.ExportAsync(records, TaskExportFormat.Markdown, stream);
        stream.Position = 0;
        var text = await new StreamReader(stream).ReadToEndAsync();

        Assert.Contains("## 2026-07-31", text);
        Assert.Contains("- [x] Done task", text);
        Assert.Contains("- [ ] Open task", text);
        Assert.Contains("High", text);
        Assert.Contains("Work", text);
    }

    [Fact]
    public async Task Excel_Export_ProducesAReadableWorkbookWithCorrectData()
    {
        var records = new List<TaskExportRecord>
        {
            CreateRecord(title: "Spreadsheet task", category: "Finance", isCompleted: true, estimatedMinutes: 20),
        };
        var exportService = new TaskExportService();
        using var stream = new MemoryStream();

        await exportService.ExportAsync(records, TaskExportFormat.Excel, stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Tasks");
        Assert.Equal("Title", sheet.Cell(1, 1).GetString());
        Assert.Equal("Spreadsheet task", sheet.Cell(2, 1).GetString());
        Assert.Equal("Finance", sheet.Cell(2, 6).GetString());
        Assert.True(sheet.Cell(2, 8).GetBoolean());
        Assert.Equal(20, sheet.Cell(2, 10).GetValue<int>());
    }
}
