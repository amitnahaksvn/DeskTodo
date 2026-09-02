using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("Decisions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).IsRequired().HasMaxLength(300);
        builder.Property(d => d.DecisionText).IsRequired().HasMaxLength(4000);
        builder.Property(d => d.Context).HasMaxLength(4000);
        builder.Property(d => d.Alternatives).HasMaxLength(4000);
        builder.Property(d => d.Reason).HasMaxLength(4000);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.CreatedAt);
    }
}
