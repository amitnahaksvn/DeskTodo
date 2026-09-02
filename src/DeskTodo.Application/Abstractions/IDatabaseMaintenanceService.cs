namespace DeskTodo.Application.Abstractions;

/// <summary>Feature 69's Database Maintenance Center dashboard numbers.</summary>
public sealed record DatabaseStats(
    long DatabaseSizeBytes,
    int TaskCount,
    int ProjectCount,
    int TagCount,
    int HistoryRecordCount,
    int VersionCount,
    int AttachmentCount,
    string MigrationVersion);

/// <summary>
/// Feature 69 (Roadmap-39-100.md) — diagnostic dashboard and maintenance operations for the
/// SQLite database. "Backup" and "Integrity Check" from the spec's own operations list are
/// deliberately not duplicated here — <see cref="IBackupService"/> (Feature 67) and
/// <see cref="IDataIntegrityService"/> (Feature 70) already own those, reachable from their own
/// windows; this service only owns the two operations neither of those already covers.
/// </summary>
public interface IDatabaseMaintenanceService
{
    Task<DatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs SQLite's own <c>VACUUM</c> — rebuilds the database file to reclaim space left by deleted rows.</summary>
    Task VacuumAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs SQLite's own <c>REINDEX</c> — rebuilds every index from scratch.</summary>
    Task RebuildIndexesAsync(CancellationToken cancellationToken = default);
}
