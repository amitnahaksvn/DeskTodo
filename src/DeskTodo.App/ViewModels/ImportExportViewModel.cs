using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Import/Export window. The actual file picking (native save/open dialogs) is a
/// View concern — this only ever works against a <see cref="Stream"/> the View hands it
/// after the user picks a location, same division of responsibility as everywhere else in
/// this app that touches a platform API a ViewModel shouldn't reference directly.
/// </summary>
public sealed partial class ImportExportViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITaskExportService _exportService;
    private readonly ITaskImportService _importService;
    private readonly ILogger<ImportExportViewModel> _logger;

    public ImportExportViewModel(
        ITaskService taskService,
        ICategoryRepository categoryRepository,
        ITaskExportService exportService,
        ITaskImportService importService,
        ILogger<ImportExportViewModel> logger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _exportService = exportService;
        _importService = importService;
        _logger = logger;
    }

    public IReadOnlyList<TaskExportFormat> ExportFormats { get; } = Enum.GetValues<TaskExportFormat>();

    [ObservableProperty]
    public partial TaskExportFormat SelectedExportFormat { get; set; } = TaskExportFormat.Csv;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>The file extension (no dot) matching <see cref="SelectedExportFormat"/> — the View uses this to suggest a filename/filter for the save dialog.</summary>
    public string SelectedExportExtension => SelectedExportFormat switch
    {
        TaskExportFormat.Csv => "csv",
        TaskExportFormat.Json => "json",
        TaskExportFormat.Markdown => "md",
        TaskExportFormat.Excel => "xlsx",
        _ => "txt",
    };

    public async Task ExportToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);
            var records = tasks.Select(ToRecord).ToList();

            await _exportService.ExportAsync(records, SelectedExportFormat, destination, cancellationToken);

            StatusMessage = records.Count == 1 ? "Exported 1 task." : $"Exported {records.Count} tasks.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export tasks");
            StatusMessage = "Export failed — see the log for details.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportFromAsync(Stream source, TaskImportFormat format, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var records = await _importService.ImportAsync(source, format, cancellationToken);
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

            var importedCount = 0;
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await CreateTaskFromRecordAsync(record, categoryIdByName, cancellationToken);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    // One malformed row shouldn't fail the whole batch — logged and skipped.
                    _logger.LogWarning(ex, "Failed to import task '{Title}'; skipping", record.Title);
                }
            }

            StatusMessage = $"Imported {importedCount} of {records.Count} task{(records.Count == 1 ? "" : "s")}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import tasks");
            StatusMessage = "Import failed — see the log for details.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateTaskFromRecordAsync(TaskExportRecord record, IReadOnlyDictionary<string, Guid> categoryIdByName, CancellationToken cancellationToken)
    {
        var categoryId = record.Category is { } categoryName && categoryIdByName.TryGetValue(categoryName, out var id) ? id : (Guid?)null;
        var priority = Enum.TryParse<TaskPriority>(record.Priority, ignoreCase: true, out var parsedPriority) ? parsedPriority : TaskPriority.Medium;

        var task = await _taskService.CreateTaskAsync(record.PlanDate, record.Title, record.Description, priority, categoryId, record.DueDate, cancellationToken);

        if (!string.IsNullOrWhiteSpace(record.Notes) || record.EstimatedMinutes.HasValue)
        {
            task.Notes = record.Notes;
            task.EstimatedMinutes = record.EstimatedMinutes;
            await _taskService.UpdateTaskAsync(task, cancellationToken);
        }

        if (record.IsCompleted)
        {
            await _taskService.CompleteTaskAsync(task.Id, cancellationToken);
        }

        if (record.IsPinned)
        {
            await _taskService.PinTaskAsync(task.Id, cancellationToken);
        }
    }

    private static TaskExportRecord ToRecord(TaskItem task) => new()
    {
        Title = task.Title,
        Description = task.Description,
        PlanDate = task.PlanDate,
        DueDate = task.DueDate,
        Priority = task.Priority.ToString(),
        Category = task.Category?.Name,
        Notes = task.Notes,
        IsCompleted = task.IsCompleted,
        IsPinned = task.IsPinned,
        EstimatedMinutes = task.EstimatedMinutes,
    };
}
