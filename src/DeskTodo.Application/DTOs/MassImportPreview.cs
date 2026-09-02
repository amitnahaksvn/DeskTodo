namespace DeskTodo.Application.DTOs;

/// <summary>One CSV row's outcome from <see cref="Abstractions.IMassImportService.PreviewAsync"/> — Feature 89's "Preview" pipeline step.</summary>
public sealed record MassImportRowPreview(int RowNumber, TaskExportRecord? Record, IReadOnlyList<string> ValidationErrors, bool IsDuplicate);

/// <summary>The full result of previewing a mass import before anything is actually created — Feature 89's Preview/Validate/Duplicate Check steps combined.</summary>
public sealed record MassImportPreviewResult(int TotalRows, IReadOnlyList<MassImportRowPreview> Rows)
{
    public bool HasValidationErrors => Rows.Any(r => r.ValidationErrors.Count > 0);

    public int DuplicateCount => Rows.Count(r => r.IsDuplicate);
}
