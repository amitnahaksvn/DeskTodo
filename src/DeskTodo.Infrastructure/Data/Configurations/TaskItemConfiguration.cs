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

        // Self-referencing one-level parent/child (Subtasks). Restrict rather than Cascade:
        // no code path ever hard-deletes a Tasks row (deletion is always the IsDeleted soft
        // flag), so the delete behavior is moot in practice, but Restrict is the safer
        // no-surprises default for a self-referencing FK on the rare provider where it isn't.
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

        // Computed at read time; not persisted.
        builder.Ignore(t => t.IsOverdue);
        builder.Ignore(t => t.IsBlocked);
    }
}
