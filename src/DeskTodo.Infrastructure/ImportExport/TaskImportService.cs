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
        var rows = ParseCsv(text);
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

    /// <summary>
    /// A small hand-rolled RFC 4180 parser (quoted fields, doubled-quote escaping, commas
    /// and newlines inside quotes) rather than a naive <c>Split(',')</c>, which would
    /// silently corrupt any exported field that itself contained a comma or newline (e.g. a
    /// multi-line Notes field) — exactly the fields <see cref="TaskExportService"/> quotes
    /// for that reason.
    /// </summary>
    private static List<List<string>> ParseCsv(string text)
    {
        var records = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    continue;
                }

                field.Append(c);
                i++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    i++;
                    break;
                case '\r':
                    i++;
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    records.Add(fields);
                    fields = [];
                    i++;
                    break;
                default:
                    field.Append(c);
                    i++;
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields);
        }

        return records;
    }
}
