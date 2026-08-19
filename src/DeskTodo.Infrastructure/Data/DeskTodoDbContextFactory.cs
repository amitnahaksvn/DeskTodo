using DeskTodo.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeskTodo.Infrastructure.Data;

/// <summary>
/// Design-time-only factory the EF Core CLI (<c>dotnet ef migrations add</c>) uses to build a
/// <see cref="DeskTodoDbContext"/> without constructing the whole app's DI container via
/// <c>DeskTodo.App.Program</c>. Building the full host at design time hits an unrelated,
/// pre-existing lifetime-validation failure (a singleton ViewModel consuming a scoped
/// service) that never surfaces during normal app startup — normal startup doesn't run with
/// <c>ServiceProviderOptions.ValidateScopes</c> on, but the EF CLI's own host bootstrapping
/// does. Rather than change an unrelated singleton's lifetime as a side effect of adding a
/// migration, this factory sidesteps the whole app host, matching Microsoft's documented
/// "no parameterless constructor" design-time pattern.
/// </summary>
public sealed class DeskTodoDbContextFactory : IDesignTimeDbContextFactory<DeskTodoDbContext>
{
    public DeskTodoDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(AppStoragePaths.ResolveDefaultRootDirectory(), "desktodo.db");
        var optionsBuilder = new DbContextOptionsBuilder<DeskTodoDbContext>();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        return new DeskTodoDbContext(optionsBuilder.Options);
    }
}
