using System.Globalization;
using System.Text;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Infrastructure.ImportExport;

/// <inheritdoc cref="IMassImportService"/>
public sealed class MassImportService(
    ITaskService taskService,
    ICategoryRepository categoryRepository,
    IDuplicateDetectionService duplicateDetectionService,
    IMigrationRunRepository migrationRunRepository) : IMassImportService
{
    public async Task<IReadOnlyList<string>> ReadCsvHeadersAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var rows = await ReadCsvRowsAsync(source, cancellationToken);
        return rows.Count > 0 ? rows[0] : [];
    }

    public async Task<MassImportPreviewResult> PreviewAsync(Stream source, IReadOnlyDictionary<string, string> columnToField, CancellationToken cancellationToken = default)
    {
        var rows = await BuildRowPreviewsAsync(source, columnToField, cancellationToken);
        return new MassImportPreviewResult(rows.Count, rows);
    }

    public async Task<MigrationRun> ImportAsync(Stream source, IReadOnlyDictionary<string, string> columnToField, string sourceDescription, CancellationToken cancellationToken = default)
    {
        var rowPreviews = await BuildRowPreviewsAsync(source, columnToField, cancellationToken);
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var run = new MigrationRun
        {
            SourceDescription = sourceDescription,
            TotalRecords = rowPreviews.Count,
        };

        if (rowPreviews.Any(r => r.ValidationErrors.Count > 0))
        {
            run.Status = MigrationStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.LogEntries = rowPreviews
                .Where(r => r.ValidationErrors.Count > 0)
                .Select(r => $"Row {r.RowNumber} rejected: {string.Join("; ", r.ValidationErrors)}")
                .ToList();
            run.LogEntries.Insert(0, "Import aborted — no tasks were created because at least one row failed validation.");

            await migrationRunRepository.AddAsync(run, cancellationToken);
            return run;
        }

        var log = new List<string>();
        var imported = 0;
        var skipped = 0;

        foreach (var rowPreview in rowPreviews)
        {
            if (rowPreview.Record is not { } record)
            {
                continue;
            }

            if (rowPreview.IsDuplicate)
            {
                skipped++;
                log.Add($"Row {rowPreview.RowNumber} skipped: duplicate of an existing task ('{record.Title}').");
                continue;
            }

            await CreateTaskFromRecordAsync(record, categoryIdByName, cancellationToken);
            imported++;
            log.Add($"Row {rowPreview.RowNumber} imported: '{record.Title}'.");
        }

        run.Status = MigrationStatus.Completed;
        run.ImportedCount = imported;
        run.SkippedCount = skipped;
        run.LogEntries = log;
        run.CompletedAt = DateTime.UtcNow;

        await migrationRunRepository.AddAsync(run, cancellationToken);
        return run;
    }

    public Task<IReadOnlyList<MigrationRun>> GetMigrationRunsAsync(CancellationToken cancellationToken = default) =>
        migrationRunRepository.GetAllAsync(cancellationToken);

    private async Task<List<MassImportRowPreview>> BuildRowPreviewsAsync(Stream source, IReadOnlyDictionary<string, string> columnToField, CancellationToken cancellationToken)
    {
        var rows = await ReadCsvRowsAsync(source, cancellationToken);
        var previews = new List<MassImportRowPreview>();
        if (rows.Count == 0)
        {
            return previews;
        }

        var headerIndex = rows[0]
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
        var fieldToColumn = columnToField.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var existingTasks = await taskService.GetAllTasksAsync(cancellationToken);

        var rowNumber = 1;
        foreach (var row in rows.Skip(1))
        {
            if (row.Count == 1 && row[0].Length == 0)
            {
                continue; // A trailing blank line parses as a single empty field.
            }

            rowNumber++;
            var (record, errors) = MapRow(headerIndex, row, fieldToColumn);

            var isDuplicate = false;
            if (record is not null)
            {
                var categoryId = record.Category is { } categoryName && categoryIdByName.TryGetValue(categoryName, out var id) ? id : (Guid?)null;
                isDuplicate = duplicateDetectionService.FindPossibleDuplicates(record.Title, record.PlanDate, categoryId, existingTasks).Count > 0;
            }

            previews.Add(new MassImportRowPreview(rowNumber, record, errors, isDuplicate));
        }

        return previews;
    }

    private static (TaskExportRecord? Record, List<string> Errors) MapRow(IReadOnlyDictionary<string, int> headerIndex, IReadOnlyList<string> row, IReadOnlyDictionary<string, string> fieldToColumn)
    {
        string? Get(string field)
        {
            if (!fieldToColumn.TryGetValue(field, out var column) || !headerIndex.TryGetValue(column, out var index))
            {
                return null;
            }

            return index < row.Count && row[index].Length > 0 ? row[index] : null;
        }

        var errors = new List<string>();
        var title = Get("Title");
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title is required (map a column to the Title field).");
            return (null, errors);
        }

        var record = new TaskExportRecord
        {
            Title = title,
            Description = Get("Description"),
            PlanDate = DateOnly.TryParse(Get("PlanDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var planDate)
                ? planDate
                : DateOnly.FromDateTime(DateTime.Now),
            DueDate = DateTime.TryParse(Get("DueDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate) ? dueDate : null,
            Priority = Get("Priority") ?? "Medium",
            Category = Get("Category"),
            Notes = Get("Notes"),
            IsCompleted = bool.TryParse(Get("IsCompleted"), out var isCompleted) && isCompleted,
            IsPinned = bool.TryParse(Get("IsPinned"), out var isPinned) && isPinned,
            EstimatedMinutes = int.TryParse(Get("EstimatedMinutes"), out var minutes) ? minutes : null,
        };

        return (record, errors);
    }

    private async Task CreateTaskFromRecordAsync(TaskExportRecord record, IReadOnlyDictionary<string, Guid> categoryIdByName, CancellationToken cancellationToken)
    {
        var categoryId = record.Category is { } categoryName && categoryIdByName.TryGetValue(categoryName, out var id) ? id : (Guid?)null;
        var priority = Enum.TryParse<TaskPriority>(record.Priority, ignoreCase: true, out var parsedPriority) ? parsedPriority : TaskPriority.Medium;

        var task = await taskService.CreateTaskAsync(record.PlanDate, record.Title, record.Description, priority, categoryId, record.DueDate, cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(record.Notes) || record.EstimatedMinutes.HasValue)
        {
            task.Notes = record.Notes;
            task.EstimatedMinutes = record.EstimatedMinutes;
            await taskService.UpdateTaskAsync(task, cancellationToken);
        }

        if (record.IsCompleted)
        {
            await taskService.CompleteTaskAsync(task.Id, cancellationToken);
        }

        if (record.IsPinned)
        {
            await taskService.PinTaskAsync(task.Id, cancellationToken);
        }
    }

    private static async Task<List<List<string>>> ReadCsvRowsAsync(Stream source, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(source, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return CsvParsing.Parse(text);
    }
}
