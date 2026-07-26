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

        // Drives "today's list" / calendar-day lookups — the most frequent query pattern.
        builder.HasIndex(t => new { t.PlanDate, t.DayOrder });
        builder.HasIndex(t => t.IsDeleted);
        builder.HasIndex(t => t.IsArchived);

        // Computed at read time from DueDate + IsCompleted; not persisted.
        builder.Ignore(t => t.IsOverdue);
    }
}
