using System.IO.Compression;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeskTodo.Infrastructure.LocalBackup;

/// <summary>
/// Local, on-disk backup/restore — Features 67 and 68 (Roadmap-39-100.md). Each backup is a
/// plain zip under <c>{AppStorageOptions.RootDirectory}/backups/</c> containing a copy of the
/// live SQLite database (and settings.json, if present at backup time). No network, no cloud
/// storage — that's Phase 31's still-deferred Cloud Sync.
/// </summary>
public sealed class BackupService(IOptions<AppStorageOptions> storageOptions, ILogger<BackupService> logger) : IBackupService
{
    private const int RetentionCount = 14;
    private const string DatabaseEntryName = "database.db";
    private const string SettingsEntryName = "settings.json";

    public async Task<BackupInfo> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var storage = storageOptions.Value;
        var backupsDirectory = GetBackupsDirectory(storage);
        Directory.CreateDirectory(backupsDirectory);

        var databasePath = Path.Combine(storage.RootDirectory, storage.DatabaseFileName);
        var settingsPath = Path.Combine(storage.RootDirectory, storage.SettingsFileName);

        // Millisecond resolution, not just seconds: RestoreAsync takes a safety backup of the
        // live database immediately before extracting the backup being restored from — a
        // second-resolution filename could collide between the two (or between two rapid
        // manual "Create Backup Now" clicks) and silently overwrite one archive with the
        // other's content.
        var fileName = $"desktodo-backup-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.zip";
        var backupPath = Path.Combine(backupsDirectory, fileName);

        // Release any pooled native SQLite handles first — Microsoft.Data.Sqlite pools
        // connections by default, and a stale pooled handle could otherwise keep the file
        // locked against a clean copy, even though this app's own DbContexts are short-lived.
        SqliteConnection.ClearAllPools();

        await using (var zipStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            if (File.Exists(databasePath))
            {
                archive.CreateEntryFromFile(databasePath, DatabaseEntryName);
            }

            if (File.Exists(settingsPath))
            {
                archive.CreateEntryFromFile(settingsPath, SettingsEntryName);
            }
        }

        logger.LogInformation("Created backup {BackupPath}", backupPath);

        await PruneOldBackupsAsync(backupsDirectory, cancellationToken);

        var info = new FileInfo(backupPath);
        return new BackupInfo(backupPath, fileName, info.CreationTimeUtc, info.Length);
    }

    public Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backupsDirectory = GetBackupsDirectory(storageOptions.Value);
        if (!Directory.Exists(backupsDirectory))
        {
            return Task.FromResult<IReadOnlyList<BackupInfo>>([]);
        }

        var backups = Directory.EnumerateFiles(backupsDirectory, "*.zip")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.CreationTimeUtc)
            .Select(info => new BackupInfo(info.FullName, info.Name, info.CreationTimeUtc, info.Length))
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupInfo>>(backups);
    }

    public Task DeleteBackupAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("Deleted backup {BackupPath}", filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<RestoreSimulationResult> SimulateRestoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extractedDatabasePath = ExtractDatabaseToTempFile(filePath);
        try
        {
            var backupTasks = ReadTaskSnapshots(extractedDatabasePath);
            var liveDatabasePath = Path.Combine(storageOptions.Value.RootDirectory, storageOptions.Value.DatabaseFileName);
            var liveTasks = File.Exists(liveDatabasePath)
                ? ReadTaskSnapshots(liveDatabasePath)
                : new Dictionary<Guid, (bool IsDeleted, DateTime ModifiedAt, string Title)>();

            var toAdd = 0;
            var toUpdate = 0;
            var samples = new List<string>();

            foreach (var (id, backupTask) in backupTasks)
            {
                if (!liveTasks.TryGetValue(id, out var liveTask))
                {
                    toAdd++;
                    if (samples.Count < 5)
                    {
                        samples.Add($"Would add: \"{backupTask.Title}\"");
                    }
                }
                else if (liveTask.ModifiedAt != backupTask.ModifiedAt || liveTask.IsDeleted != backupTask.IsDeleted)
                {
                    toUpdate++;
                    if (samples.Count < 5)
                    {
                        samples.Add($"Would change: \"{backupTask.Title}\"");
                    }
                }
            }

            var toRemove = liveTasks.Keys.Count(id => !backupTasks.ContainsKey(id));

            return new RestoreSimulationResult(backupTasks.Count, toAdd, toUpdate, toRemove, samples);
        }
        finally
        {
            TryDeleteTempFile(extractedDatabasePath);
        }
    }

    public async Task RestoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Extract the requested backup's database to a temp file *before* taking the
        // pre-restore safety backup below — belt-and-braces against the two ever touching the
        // same archive path (see CreateBackupAsync's filename comment).
        var extractedDatabasePath = ExtractDatabaseToTempFile(filePath);
        try
        {
            // A safety net: back up the current (pre-restore) database too, so restoring is
            // itself recoverable from — the same "the undo path is itself covered" reasoning as
            // TaskService.RestoreTaskVersionAsync.
            await CreateBackupAsync(cancellationToken);

            var storage = storageOptions.Value;
            var databasePath = Path.Combine(storage.RootDirectory, storage.DatabaseFileName);

            SqliteConnection.ClearAllPools();
            File.Copy(extractedDatabasePath, databasePath, overwrite: true);

            using var archive = ZipFile.OpenRead(filePath);
            var settingsEntry = archive.GetEntry(SettingsEntryName);
            if (settingsEntry is not null)
            {
                var settingsPath = Path.Combine(storage.RootDirectory, storage.SettingsFileName);
                settingsEntry.ExtractToFile(settingsPath, overwrite: true);
            }

            logger.LogInformation("Restored database from backup {BackupPath}", filePath);
        }
        finally
        {
            TryDeleteTempFile(extractedDatabasePath);
        }
    }

    private static string GetBackupsDirectory(AppStorageOptions storage) => Path.Combine(storage.RootDirectory, "backups");

    private static string ExtractDatabaseToTempFile(string backupZipPath)
    {
        using var archive = ZipFile.OpenRead(backupZipPath);
        var entry = archive.GetEntry(DatabaseEntryName)
            ?? throw new InvalidOperationException($"Backup '{backupZipPath}' does not contain a database file.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"desktodo-restore-{Guid.NewGuid():N}.db");
        entry.ExtractToFile(tempPath, overwrite: true);
        return tempPath;
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a scratch temp file — leaving a stray file in %TEMP%
            // isn't worth surfacing an error over.
        }
    }

    /// <summary>
    /// Reads just enough of the Tasks table (Id/IsDeleted/ModifiedAt/Title) via a raw ADO.NET
    /// query rather than spinning up a full second <c>DeskTodoDbContext</c> against the backup
    /// copy — deliberately: pointing EF Core's migration-aware context at an old backup could
    /// try to apply pending migrations to it, which is not what a read-only comparison should
    /// ever do.
    /// </summary>
    private static Dictionary<Guid, (bool IsDeleted, DateTime ModifiedAt, string Title)> ReadTaskSnapshots(string databasePath)
    {
        var result = new Dictionary<Guid, (bool, DateTime, string)>();

        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, IsDeleted, ModifiedAt, Title FROM Tasks";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var isDeleted = reader.GetInt64(1) != 0;
            var modifiedAt = reader.GetDateTime(2);
            var title = reader.GetString(3);
            result[id] = (isDeleted, modifiedAt, title);
        }

        return result;
    }

    private async Task PruneOldBackupsAsync(string backupsDirectory, CancellationToken cancellationToken)
    {
        var backups = await GetBackupsAsync(cancellationToken);
        foreach (var stale in backups.Skip(RetentionCount))
        {
            await DeleteBackupAsync(stale.FilePath, cancellationToken);
        }
    }
}
