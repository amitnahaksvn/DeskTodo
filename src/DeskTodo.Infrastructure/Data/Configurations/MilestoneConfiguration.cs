using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("Milestones");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);

        // SetNull, not Cascade — deleting a project shouldn't silently delete milestones that
        // may still be referenced from elsewhere; it just orphans them back to "standalone",
        // the same way TaskItem.CategoryId/ProjectId already behave on their parent's deletion.
        builder.HasOne(m => m.Project)
            .WithMany()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.ProjectId, m.Order });
    }
}
