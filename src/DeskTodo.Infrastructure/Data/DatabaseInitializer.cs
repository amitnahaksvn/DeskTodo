using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Infrastructure.Data;

/// <summary>
/// Applies pending EF Core migrations on startup (this is what "support
/// automatic migrations" means in practice: no manual `dotnet ef database
/// update` step for end users — the app creates/upgrades its own SQLite
/// file on first run and on every version upgrade).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task MigrateDeskTodoDatabaseAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeskTodoDbContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeskTodoDbContext>>();

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending database migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        }

        await context.Database.MigrateAsync(cancellationToken);
    }
}
