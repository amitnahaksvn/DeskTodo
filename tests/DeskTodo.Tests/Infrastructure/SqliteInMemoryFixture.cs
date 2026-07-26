using DeskTodo.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// A real (not mocked) SQLite database for repository tests, kept alive for
/// the fixture's lifetime via one open ":memory:" connection shared by every
/// <see cref="DeskTodoDbContext"/> instance created through <see cref="ContextFactory"/>
/// — SQLite's in-memory database only exists for as long as at least one
/// connection to it stays open. Schema (including seeded categories) is
/// created directly from the current model via <c>EnsureCreated</c> rather
/// than by applying migrations, which is the standard, faster approach for
/// tests that don't need to verify the migrations themselves.
/// </summary>
public sealed class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeskTodoDbContext>()
            .UseSqlite(_connection)
            .Options;

        ContextFactory = new DelegatingDbContextFactory(options);

        using var context = ContextFactory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    public IDbContextFactory<DeskTodoDbContext> ContextFactory { get; }

    public void Dispose() => _connection.Dispose();

    private sealed class DelegatingDbContextFactory(DbContextOptions<DeskTodoDbContext> options) : IDbContextFactory<DeskTodoDbContext>
    {
        public DeskTodoDbContext CreateDbContext() => new(options);
    }
}
