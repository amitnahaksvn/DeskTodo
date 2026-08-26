using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.Notes).HasMaxLength(4000);
        builder.Property(t => t.ColorHex).HasMaxLength(9);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Milestone)
            .WithMany(m => m.Tasks)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing one-level parent/child (Subtasks). Restrict rather than Cascade:
        // deletion is normally the IsDeleted soft flag, which never touches this FK at all.
        // Feature 46's Trash ("Delete Permanently"/"Empty Trash") is the one genuine hard-delete
        // path in this app — TaskRepository.RemoveAsync orphans any subtasks (clears
        // ParentTaskId) itself before removing the row, rather than relying on Cascade here,
        // since a subtask of a since-soft-deleted parent may still be an entirely live task
        // the user hasn't asked to delete.
        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // Drives "today's list" / calendar-day lookups — the most frequent query pattern.
        builder.HasIndex(t => new { t.PlanDate, t.DayOrder });
        builder.HasIndex(t => t.IsDeleted);
        builder.HasIndex(t => t.IsArchived);
        builder.HasIndex(t => t.IsFavorite);
        builder.HasIndex(t => t.ParentTaskId);
        builder.HasIndex(t => t.MilestoneId);
        builder.HasIndex(t => t.ProjectId);

        // Computed at read time; not persisted.
        builder.Ignore(t => t.IsOverdue);
        builder.Ignore(t => t.IsBlocked);
    }
}
