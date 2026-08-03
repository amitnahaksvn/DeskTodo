using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class GoalCompletionConfiguration : IEntityTypeConfiguration<GoalCompletion>
{
    public void Configure(EntityTypeBuilder<GoalCompletion> builder)
    {
        builder.ToTable("GoalCompletions");
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Goal)
            .WithMany(g => g.Completions)
            .HasForeignKey(c => c.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // A goal can only be marked done once per day — the same "prevent a duplicate
        // assignment" reasoning as TaskDependency's unique composite index.
        builder.HasIndex(c => new { c.GoalId, c.CompletedDate }).IsUnique();
    }
}
