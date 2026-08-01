using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies");
        builder.HasKey(d => d.Id);

        // Restrict on both sides: hard-deleting a Tasks row never happens (see
        // TaskItemConfiguration's ParentTask remark), so this is a no-surprises default
        // rather than a load-bearing choice. Two FKs into the same table both pointing at
        // it would give SQLite/EF Core multiple cascade paths under Cascade, which Restrict
        // sidesteps outright.
        builder.HasOne(d => d.BlockingTask)
            .WithMany(t => t.BlockingDependencies)
            .HasForeignKey(d => d.BlockingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.BlockedTask)
            .WithMany(t => t.BlockedByDependencies)
            .HasForeignKey(d => d.BlockedTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.BlockedTaskId);
        builder.HasIndex(d => new { d.BlockingTaskId, d.BlockedTaskId }).IsUnique();
    }
}
