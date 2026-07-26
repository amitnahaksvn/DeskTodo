using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Data;

public sealed class DeskTodoDbContext(DbContextOptions<DeskTodoDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeskTodoDbContext).Assembly);
    }
}
