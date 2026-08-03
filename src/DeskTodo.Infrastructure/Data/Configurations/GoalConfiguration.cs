using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Description).HasMaxLength(2000);

        // GetCurrentStreak needs no Ignore() — it's a parameterized method, not a property,
        // so EF Core's convention-based model discovery never considers mapping it (unlike
        // TaskItem.IsBlocked, a parameterless computed *property*, which does need one).
    }
}
