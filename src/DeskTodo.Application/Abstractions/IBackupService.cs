namespace DeskTodo.Application.Abstractions;

/// <summary>One backup archive on disk — Feature 67 (Roadmap-39-100.md), Local Backup Manager.</summary>
public sealed record BackupInfo(string FilePath, string FileName, DateTime CreatedAt, long SizeBytes);

/// <summary>
/// A dry-run comparison of a backup archive against the live database — Feature 68, Backup
/// Restore Simulator. Computed without touching the live database, so it's safe to run before
/// deciding whether to actually restore.
/// </summary>
public sealed record RestoreSimulationResult(
    int TotalTasksInBackup,
    int TasksToAdd,
    int TasksToUpdate,
    int TasksToRemove,
    IReadOnlyList<string> SampleChanges);

/// <summary>
/// Local, on-disk backup/restore for DeskTodo's SQLite database and settings file — Features 67
/// and 68 (Roadmap-39-100.md). No cloud storage involved (that's Phase 31's still-deferred Cloud
/// Sync); every backup is a plain zip file this device already has.
/// </summary>
public interface IBackupService
{
    /// <summary>Zips the current database and settings file into a new timestamped backup, then prunes old backups beyond the retention count.</summary>
    Task<BackupInfo> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>Every backup currently on disk, most recent first.</summary>
    Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);

    Task DeleteBackupAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Feature 68 — reports what restoring this backup would change, without touching the live database.</summary>
    Task<RestoreSimulationResult> SimulateRestoreAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the live database with the one inside this backup. A safety backup of the
    /// *current* database is taken first (via <see cref="CreateBackupAsync"/>), so restoring is
    /// itself recoverable from.
    /// </summary>
    Task RestoreAsync(string filePath, CancellationToken cancellationToken = default);
}
