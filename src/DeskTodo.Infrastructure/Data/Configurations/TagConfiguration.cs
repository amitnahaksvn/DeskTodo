using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();

        // Implicit many-to-many (EF Core's "skip navigation" support) — no explicit TaskTag
        // class needed; EF Core generates the join table itself.
        builder.HasMany(t => t.Tasks)
            .WithMany(t => t.Tags)
            .UsingEntity(j => j.ToTable("TaskTags"));
    }
}
