namespace DeskTodo.Domain.Enums;

/// <summary>
/// Mirrors <c>DeskTodo.Application.Abstractions.TaskExportFormat</c> exactly (Csv/Json/Markdown/
/// Excel) — duplicated rather than referenced because Domain must not depend on Application, and
/// <see cref="Entities.ExportProfile"/> (a Domain entity) needs to store a format. Translated
/// back to <c>TaskExportFormat</c> by the Infrastructure-layer service that actually runs a
/// profile against <c>ITaskExportService</c>.
/// </summary>
public enum ExportFormat
{
    Csv = 0,
    Json = 1,
    Markdown = 2,
    Excel = 3,
}
