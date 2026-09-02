using DeskTodo.Application.Options;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>Feature 69 (Roadmap-39-100.md) — real SQLite file on disk, same bar as BackupServiceTests.</summary>
public sealed class DatabaseMaintenanceServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<DeskTodoDbContext> _options;

    public DatabaseMaintenanceServiceTests()
    {
        Directory.CreateDirectory(_rootDirectory);
        _options = new DbContextOptionsBuilder<DeskTodoDbContext>().UseSqlite($"Data Source={Path.Combine(_rootDirectory, "desktodo.db")}").Options;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_rootDirectory, recursive: true);
    }

    private DatabaseMaintenanceService CreateSut() =>
        new(
            new SingleOptionsDbContextFactory(_options),
            Options.Create(new AppStorageOptions { RootDirectory = _rootDirectory, DatabaseFileName = "desktodo.db" }),
            NullLogger<DatabaseMaintenanceService>.Instance);

    [Fact]
    public async Task GetStatsAsync_ReportsRealCountsAndFileSize()
    {
        // Real migrations, not EnsureCreated — GetStatsAsync reports the latest applied
        // migration from __EFMigrationsHistory, which only MigrateAsync populates.
        await using (var context = new DeskTodoDbContext(_options))
        {
            await context.Database.MigrateAsync();
        }

        var taskRepository = new TaskRepository(new SingleOptionsDbContextFactory(_options));
        await taskRepository.AddAsync(new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task 1" });
        await taskRepository.AddAsync(new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task 2" });
        SqliteConnection.ClearAllPools();

        var sut = CreateSut();
        var stats = await sut.GetStatsAsync();

        Assert.Equal(2, stats.TaskCount);
        Assert.True(stats.DatabaseSizeBytes > 0);
        Assert.NotEqual("(none)", stats.MigrationVersion);
    }

    [Fact]
    public async Task VacuumAsync_RunsWithoutError_AndDatabaseStaysReadable()
    {
        await using (var context = new DeskTodoDbContext(_options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var sut = CreateSut();
        await sut.VacuumAsync();

        await using var contextAfter = new DeskTodoDbContext(_options);
        Assert.Equal(0, await contextAfter.Tasks.CountAsync());
    }

    [Fact]
    public async Task RebuildIndexesAsync_RunsWithoutError_AndDatabaseStaysReadable()
    {
        await using (var context = new DeskTodoDbContext(_options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var sut = CreateSut();
        await sut.RebuildIndexesAsync();

        await using var contextAfter = new DeskTodoDbContext(_options);
        Assert.Equal(0, await contextAfter.Tasks.CountAsync());
    }

    private sealed class SingleOptionsDbContextFactory(DbContextOptions<DeskTodoDbContext> options) : IDbContextFactory<DeskTodoDbContext>
    {
        public DeskTodoDbContext CreateDbContext() => new(options);
    }
}
