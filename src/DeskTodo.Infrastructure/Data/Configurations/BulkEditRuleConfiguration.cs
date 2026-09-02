using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class BulkEditRuleConfiguration : IEntityTypeConfiguration<BulkEditRule>
{
    public void Configure(EntityTypeBuilder<BulkEditRule> builder)
    {
        builder.ToTable("BulkEditRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);

        var conditionsComparer = new ValueComparer<List<BulkEditCondition>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, c) => HashCode.Combine(hash, c.Field, c.Operator, c.Value)),
            v => v.ToList());

        builder.Property(r => r.Conditions)
            .HasConversion(
                conditions => JsonSerializer.Serialize(conditions, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<BulkEditCondition>() : JsonSerializer.Deserialize<List<BulkEditCondition>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(conditionsComparer);

        var actionsComparer = new ValueComparer<List<BulkEditAction>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, a) => HashCode.Combine(hash, a.Type, a.Value)),
            v => v.ToList());

        builder.Property(r => r.Actions)
            .HasConversion(
                actions => JsonSerializer.Serialize(actions, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<BulkEditAction>() : JsonSerializer.Deserialize<List<BulkEditAction>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(actionsComparer);
    }
}
