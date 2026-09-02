using System.Globalization;
using System.Text;
using System.Text.Json;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;

namespace DeskTodo.Infrastructure.ImportExport;

/// <inheritdoc cref="ITaskImportService"/>
public sealed class TaskImportService : ITaskImportService
{
    public Task<IReadOnlyList<TaskExportRecord>> ImportAsync(Stream source, TaskImportFormat format, CancellationToken cancellationToken = default) =>
        format switch
        {
            TaskImportFormat.Csv => ReadCsvAsync(source, cancellationToken),
            TaskImportFormat.Json => ReadJsonAsync(source, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };

    private static async Task<IReadOnlyList<TaskExportRecord>> ReadCsvAsync(Stream source, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(source, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        var rows = CsvParsing.Parse(text);
        if (rows.Count == 0)
        {
            return [];
        }

        var columnIndex = rows[0]
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var records = new List<TaskExportRecord>();
        foreach (var row in rows.Skip(1))
        {
            if (row.Count == 1 && row[0].Length == 0)
            {
                continue; // A trailing blank line parses as a single empty field.
            }

            string? Get(string column) =>
                columnIndex.TryGetValue(column, out var index) && index < row.Count && row[index].Length > 0
                    ? row[index]
                    : null;

            var title = Get("Title");
            if (string.IsNullOrWhiteSpace(title))
            {
                // A row with no title isn't a usable task — skipped rather than failing the
                // whole import over one bad row.
                continue;
            }

            records.Add(new TaskExportRecord
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
            });
        }

        return records;
    }

    private static async Task<IReadOnlyList<TaskExportRecord>> ReadJsonAsync(Stream source, CancellationToken cancellationToken)
    {
        var records = await JsonSerializer.DeserializeAsync<List<TaskExportRecord>>(source, cancellationToken: cancellationToken);
        return records ?? [];
    }
}
