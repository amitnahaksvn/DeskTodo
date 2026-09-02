using DeskTodo.Application.DTOs;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Roadmap-39-100.md Feature 89 (Mass Import Wizard) and Feature 90 (Data Migration Center),
/// delivered as one pipeline — see Feature 90's own roadmap entry for why. Field mapping
/// (arbitrary CSV column names → <see cref="TaskExportRecord"/> fields) plus the
/// Preview/Validate/Duplicate-Check/Import/Report pipeline both features' specs describe.
/// </summary>
public interface IMassImportService
{
    /// <summary>The raw header row of a CSV file, for the wizard's field-mapping step. Empty for a file with no rows.</summary>
    Task<IReadOnlyList<string>> ReadCsvHeadersAsync(Stream source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs every step except Import: maps each row via <paramref name="columnToField"/> (source
    /// CSV column name → one of Title/Description/PlanDate/DueDate/Priority/Category/Notes/
    /// IsCompleted/IsPinned/EstimatedMinutes), validates it, and flags likely duplicates against
    /// existing tasks (via <see cref="Services.IDuplicateDetectionService"/>) — without creating
    /// anything. Lets the wizard show "N tasks will be imported, M are duplicates" before Import runs.
    /// </summary>
    Task<MassImportPreviewResult> PreviewAsync(Stream source, IReadOnlyDictionary<string, string> columnToField, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-runs the same pipeline as <see cref="PreviewAsync"/> and, only if every row passes
    /// validation ("if validation fails, no partial import should remain" — honored by never
    /// starting to create tasks unless the whole batch is valid), creates one task per
    /// non-duplicate row. Persists a <see cref="MigrationRun"/> either way (Feature 90's "each
    /// migration should have an ID and log").
    /// </summary>
    Task<MigrationRun> ImportAsync(Stream source, IReadOnlyDictionary<string, string> columnToField, string sourceDescription, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationRun>> GetMigrationRunsAsync(CancellationToken cancellationToken = default);
}
