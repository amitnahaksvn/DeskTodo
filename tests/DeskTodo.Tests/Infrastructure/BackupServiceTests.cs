using DeskTodo.Application.Options;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.LocalBackup;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// Real (not mocked) file-based tests for Features 67/68 (Roadmap-39-100.md) — a real SQLite
/// file on disk, a real zip archive, and a real file-copy restore. Same "verify against the
/// real thing" bar Feature 46's Trash tests set for hard-delete/foreign-key behavior.
/// </summary>
public sealed class BackupServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"));

    public BackupServiceTests() => Directory.CreateDirectory(_rootDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_rootDirectory, recursive: true);
    }

    private BackupService CreateSut() =>
        new(Options.Create(new AppStorageOptions { RootDirectory = _rootDirectory, DatabaseFileName = "desktodo.db", SettingsFileName = "settings.json" }), NullLogger<BackupService>.Instance);

    private string DatabasePath => Path.Combine(_rootDirectory, "desktodo.db");

    private async Task SeedDatabaseAsync(params string[] titles)
    {
        var options = new DbContextOptionsBuilder<DeskTodoDbContext>().UseSqlite($"Data Source={DatabasePath}").Options;
        await using (var context = new DeskTodoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var factory = new SingleOptionsDbContextFactory(options);
        var repository = new TaskRepository(factory);
        foreach (var title in titles)
        {
            await repository.AddAsync(new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = title });
        }

        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public async Task CreateBackupAsync_ProducesAZipContainingTheDatabase()
    {
        await SeedDatabaseAsync("Buy milk");
        var sut = CreateSut();

        var backup = await sut.CreateBackupAsync();

        Assert.True(File.Exists(backup.FilePath));
        Assert.True(backup.SizeBytes > 0);
    }

    [Fact]
    public async Task GetBackupsAsync_ReturnsCreatedBackups_MostRecentFirst()
    {
        await SeedDatabaseAsync("Task 1");
        var sut = CreateSut();
        var first = await sut.CreateBackupAsync();
        await Task.Delay(1100); // ensure a distinct filename timestamp (second-resolution)
        var second = await sut.CreateBackupAsync();

        var backups = await sut.GetBackupsAsync();

        Assert.Equal(2, backups.Count);
        Assert.Equal(second.FilePath, backups[0].FilePath);
        Assert.Equal(first.FilePath, backups[1].FilePath);
    }

    [Fact]
    public async Task DeleteBackupAsync_RemovesTheFile()
    {
        await SeedDatabaseAsync("Task 1");
        var sut = CreateSut();
        var backup = await sut.CreateBackupAsync();

        await sut.DeleteBackupAsync(backup.FilePath);

        Assert.False(File.Exists(backup.FilePath));
    }

    [Fact]
    public async Task SimulateRestoreAsync_ReportsTasksThatWouldBeAdded()
    {
        await SeedDatabaseAsync("Task 1", "Task 2");
        var sut = CreateSut();
        var backup = await sut.CreateBackupAsync();

        // Live database now has one more task than the backup.
        await SeedDatabaseAsync("Task 3 (added after backup)");

        var result = await sut.SimulateRestoreAsync(backup.FilePath);

        Assert.Equal(2, result.TotalTasksInBackup);
        Assert.Equal(1, result.TasksToRemove); // the live-only task would be gone after restoring
        Assert.Equal(0, result.TasksToAdd);
    }

    [Fact]
    public async Task RestoreAsync_ReplacesTheLiveDatabase_WithTheBackupsContent()
    {
        await SeedDatabaseAsync("Original task");
        var sut = CreateSut();
        var backup = await sut.CreateBackupAsync();

        await SeedDatabaseAsync("A second task added after the backup");
        var options = new DbContextOptionsBuilder<DeskTodoDbContext>().UseSqlite($"Data Source={DatabasePath}").Options;
        await using (var contextBeforeRestore = new DeskTodoDbContext(options))
        {
            Assert.Equal(2, await contextBeforeRestore.Tasks.CountAsync());
        }

        await sut.RestoreAsync(backup.FilePath);

        await using var contextAfterRestore = new DeskTodoDbContext(options);
        Assert.Equal(1, await contextAfterRestore.Tasks.CountAsync());
    }

    [Fact]
    public async Task RestoreAsync_TakesASafetyBackupOfTheCurrentDatabase_First()
    {
        await SeedDatabaseAsync("Task 1");
        var sut = CreateSut();
        var backupToRestore = await sut.CreateBackupAsync();

        await sut.RestoreAsync(backupToRestore.FilePath);

        var backupsAfterRestore = await sut.GetBackupsAsync();
        Assert.True(backupsAfterRestore.Count >= 2); // the original backup + the pre-restore safety backup
    }

    private sealed class SingleOptionsDbContextFactory(DbContextOptions<DeskTodoDbContext> options) : IDbContextFactory<DeskTodoDbContext>
    {
        public DeskTodoDbContext CreateDbContext() => new(options);
    }
}
