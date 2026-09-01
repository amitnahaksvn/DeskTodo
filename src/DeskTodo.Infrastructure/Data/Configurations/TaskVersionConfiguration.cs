using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskVersionConfiguration : IEntityTypeConfiguration<TaskVersion>
{
    public void Configure(EntityTypeBuilder<TaskVersion> builder)
    {
        builder.ToTable("TaskVersions");
        builder.HasKey(v => v.Id);

        // SetNull, not Cascade — same reasoning as TaskHistoryConfiguration: a task's version
        // history is still meaningful evidence after the task is permanently removed.
        builder.HasOne(v => v.Task)
            .WithMany()
            .HasForeignKey(v => v.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.TaskId);
        builder.HasIndex(v => new { v.TaskId, v.VersionNumber });
    }
}
