using DeskTodo.Application.DTOs;

namespace DeskTodo.Application.Abstractions;

public enum TaskImportFormat
{
    Csv,
    Json,
}

/// <summary>
/// Parses tasks out of a stream. Markdown/Excel are deliberately export-only —
/// Markdown's task-list format is lossy/ambiguous to parse back unambiguously, and Excel
/// adds parsing complexity (cell type coercion, header detection) for a format most users
/// would export to for viewing, not round-tripping. Parsing only, no persistence — see
/// <see cref="DeskTodo.Application.Services.ITaskService"/> for the use case that actually
/// creates the tasks from what this returns.
/// </summary>
public interface ITaskImportService
{
    Task<IReadOnlyList<TaskExportRecord>> ImportAsync(Stream source, TaskImportFormat format, CancellationToken cancellationToken = default);
}
