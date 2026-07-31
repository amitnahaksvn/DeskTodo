using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;

namespace DeskTodo.Infrastructure.ImportExport;

/// <inheritdoc cref="ITaskExportService"/>
public sealed class TaskExportService : ITaskExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task ExportAsync(IReadOnlyList<TaskExportRecord> records, TaskExportFormat format, Stream destination, CancellationToken cancellationToken = default)
    {
        switch (format)
        {
            case TaskExportFormat.Csv:
                await WriteCsvAsync(records, destination, cancellationToken);
                break;
            case TaskExportFormat.Json:
                await JsonSerializer.SerializeAsync(destination, records, JsonOptions, cancellationToken);
                break;
            case TaskExportFormat.Markdown:
                await WriteMarkdownAsync(records, destination, cancellationToken);
                break;
            case TaskExportFormat.Excel:
                WriteExcel(records, destination);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static readonly string[] CsvHeader =
        ["Title", "Description", "PlanDate", "DueDate", "Priority", "Category", "Notes", "IsCompleted", "IsPinned", "EstimatedMinutes"];

    private static async Task WriteCsvAsync(IReadOnlyList<TaskExportRecord> records, Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(string.Join(",", CsvHeader));
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fields = new[]
            {
                CsvField(record.Title),
                CsvField(record.Description),
                CsvField(record.PlanDate.ToString("yyyy-MM-dd")),
                CsvField(record.DueDate?.ToString("yyyy-MM-ddTHH:mm:ss")),
                CsvField(record.Priority),
                CsvField(record.Category),
                CsvField(record.Notes),
                CsvField(record.IsCompleted.ToString()),
                CsvField(record.IsPinned.ToString()),
                CsvField(record.EstimatedMinutes?.ToString(CultureInfo.InvariantCulture)),
            };
            await writer.WriteLineAsync(string.Join(",", fields));
        }
    }

    // RFC 4180: a field is quoted (and internal quotes doubled) only when it actually
    // contains a comma, quote or newline — quoting everything unconditionally would still
    // parse correctly, but this keeps a plain export readable when opened in a text editor.
    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static async Task WriteMarkdownAsync(IReadOnlyList<TaskExportRecord> records, Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync("# DeskTodo Export");
        await writer.WriteLineAsync();

        foreach (var group in records.GroupBy(r => r.PlanDate).OrderBy(g => g.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync($"## {group.Key:yyyy-MM-dd}");
            await writer.WriteLineAsync();

            foreach (var record in group.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase))
            {
                var checkbox = record.IsCompleted ? "[x]" : "[ ]";
                var suffix = new List<string>();
                if (record.Priority is not "Medium")
                {
                    suffix.Add(record.Priority);
                }

                if (!string.IsNullOrWhiteSpace(record.Category))
                {
                    suffix.Add(record.Category);
                }

                if (record.DueDate is { } due)
                {
                    suffix.Add($"due {due:yyyy-MM-dd}");
                }

                var suffixText = suffix.Count > 0 ? $" _{string.Join(", ", suffix)}_" : string.Empty;
                await writer.WriteLineAsync($"- {checkbox} {record.Title}{suffixText}");

                if (!string.IsNullOrWhiteSpace(record.Notes))
                {
                    await writer.WriteLineAsync($"  - Notes: {record.Notes}");
                }
            }

            await writer.WriteLineAsync();
        }
    }

    private static void WriteExcel(IReadOnlyList<TaskExportRecord> records, Stream destination)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tasks");

        for (var column = 0; column < CsvHeader.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = CsvHeader[column];
        }

        var row = 2;
        foreach (var record in records)
        {
            sheet.Cell(row, 1).Value = record.Title;
            sheet.Cell(row, 2).Value = record.Description ?? string.Empty;
            sheet.Cell(row, 3).Value = record.PlanDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 3).Style.DateFormat.Format = "yyyy-mm-dd";
            if (record.DueDate is { } due)
            {
                sheet.Cell(row, 4).Value = due;
                sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }

            sheet.Cell(row, 5).Value = record.Priority;
            sheet.Cell(row, 6).Value = record.Category ?? string.Empty;
            sheet.Cell(row, 7).Value = record.Notes ?? string.Empty;
            sheet.Cell(row, 8).Value = record.IsCompleted;
            sheet.Cell(row, 9).Value = record.IsPinned;
            if (record.EstimatedMinutes is { } minutes)
            {
                sheet.Cell(row, 10).Value = minutes;
            }

            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(destination);
    }
}
