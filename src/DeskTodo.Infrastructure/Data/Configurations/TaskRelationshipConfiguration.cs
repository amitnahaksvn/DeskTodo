using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskRelationshipConfiguration : IEntityTypeConfiguration<TaskRelationship>
{
    public void Configure(EntityTypeBuilder<TaskRelationship> builder)
    {
        builder.ToTable("TaskRelationships");
        builder.HasKey(r => r.Id);

        // Same reasoning as TaskDependencyConfiguration: Restrict on both sides avoids the
        // multiple-cascade-paths problem two FKs into the same table would otherwise create,
        // and a hard delete cleans these rows up explicitly (see TaskRepository.RemoveAsync)
        // rather than relying on a cascade.
        builder.HasOne(r => r.SourceTask)
            .WithMany()
            .HasForeignKey(r => r.SourceTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TargetTask)
            .WithMany()
            .HasForeignKey(r => r.TargetTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.SourceTaskId);
        builder.HasIndex(r => r.TargetTaskId);
        builder.HasIndex(r => new { r.SourceTaskId, r.TargetTaskId, r.RelationshipType }).IsUnique();
    }
}
