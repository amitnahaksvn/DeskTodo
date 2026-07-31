using DeskTodo.Application.DTOs;

namespace DeskTodo.Application.Abstractions;

public enum TaskExportFormat
{
    Csv,
    Json,
    Markdown,
    Excel,
}

/// <summary>
/// Writes a set of tasks out in the given format. Implemented in Infrastructure (the Excel
/// writer needs a third-party library, which the Application layer shouldn't depend on
/// directly — see docs/ARCHITECTURE.md's "Phase 14" section for why CSV/JSON/Markdown live
/// there too instead of splitting the implementation).
/// </summary>
public interface ITaskExportService
{
    Task ExportAsync(IReadOnlyList<TaskExportRecord> records, TaskExportFormat format, Stream destination, CancellationToken cancellationToken = default);
}
