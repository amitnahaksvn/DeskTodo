using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeskTodo.Infrastructure.Data;

/// <inheritdoc cref="IDatabaseMaintenanceService"/>
public sealed class DatabaseMaintenanceService(
    IDbContextFactory<DeskTodoDbContext> contextFactory,
    IOptions<AppStorageOptions> storageOptions,
    ILogger<DatabaseMaintenanceService> logger) : IDatabaseMaintenanceService
{
    public async Task<DatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var databasePath = Path.Combine(storageOptions.Value.RootDirectory, storageOptions.Value.DatabaseFileName);
        var sizeBytes = File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0;

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        var latestMigration = appliedMigrations.OrderBy(m => m, StringComparer.Ordinal).LastOrDefault() ?? "(none)";

        return new DatabaseStats(
            DatabaseSizeBytes: sizeBytes,
            TaskCount: await context.Tasks.CountAsync(cancellationToken),
            ProjectCount: await context.Projects.CountAsync(cancellationToken),
            TagCount: await context.Tags.CountAsync(cancellationToken),
            HistoryRecordCount: await context.TaskHistories.CountAsync(cancellationToken),
            VersionCount: await context.TaskVersions.CountAsync(cancellationToken),
            AttachmentCount: await context.Attachments.CountAsync(cancellationToken),
            MigrationVersion: latestMigration);
    }

    public async Task VacuumAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken);
        logger.LogInformation("Database VACUUM completed");
    }

    public async Task RebuildIndexesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync("REINDEX", cancellationToken);
        logger.LogInformation("Database REINDEX completed");
    }
}
