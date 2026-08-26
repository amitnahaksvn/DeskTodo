using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskHistoryConfiguration : IEntityTypeConfiguration<TaskHistory>
{
    public void Configure(EntityTypeBuilder<TaskHistory> builder)
    {
        builder.ToTable("TaskHistories");
        builder.HasKey(h => h.Id);

        // SetNull, not Cascade: a task's audit trail is still meaningful evidence after the
        // task itself is permanently removed (Feature 46's "Delete Forever"/"Empty Trash") —
        // same reasoning as FocusSession.TaskId (Phase 23).
        builder.HasOne(h => h.Task)
            .WithMany()
            .HasForeignKey(h => h.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => h.TaskId);
        builder.HasIndex(h => h.Timestamp);
    }
}
