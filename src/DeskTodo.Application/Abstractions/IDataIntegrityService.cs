namespace DeskTodo.Application.Abstractions;

/// <summary>One finding from a Feature 70 (Roadmap-39-100.md) integrity check.</summary>
public sealed record IntegrityIssue(string Category, string Description, bool IsAutoRepairable);

/// <summary>
/// Feature 70 — Data Integrity Checker. Scans the live database for the kinds of quiet
/// corruption a soft-delete/nullable-FK-heavy schema can accumulate over time (a dangling
/// reference an app bug left behind, an attachment whose backing file got moved or deleted
/// outside the app) plus SQLite's own low-level page-corruption check. Read-only by default;
/// <see cref="RepairAsync"/> only ever touches the specific issues flagged
/// <see cref="IntegrityIssue.IsAutoRepairable"/> — anything riskier (e.g. the SQLite-level
/// integrity check failing) is reported, never auto-fixed.
/// </summary>
public interface IDataIntegrityService
{
    Task<IReadOnlyList<IntegrityIssue>> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Fixes every auto-repairable issue from a previous <see cref="CheckAsync"/> call. Returns how many were fixed.</summary>
    Task<int> RepairAsync(IReadOnlyList<IntegrityIssue> issues, CancellationToken cancellationToken = default);
}
