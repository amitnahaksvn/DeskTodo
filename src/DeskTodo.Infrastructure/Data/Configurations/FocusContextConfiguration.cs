using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class FocusContextConfiguration : IEntityTypeConfiguration<FocusContext>
{
    public void Configure(EntityTypeBuilder<FocusContext> builder)
    {
        builder.ToTable("FocusContexts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ColorHex).IsRequired().HasMaxLength(9);
    }
}
