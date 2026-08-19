using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskGroupConfiguration : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.ToTable("TaskGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);

        // Same manual JSON-string conversion as TaskTemplateConfiguration.ChecklistItems —
        // see that class's comment for why this is preferred over EF Core's newer built-in
        // primitive-collection mapping on this project's pinned SQLite provider version.
        var templateIdsComparer = new ValueComparer<List<Guid>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            v => v.ToList());

        builder.Property(g => g.TemplateIds)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(templateIdsComparer);
    }
}
