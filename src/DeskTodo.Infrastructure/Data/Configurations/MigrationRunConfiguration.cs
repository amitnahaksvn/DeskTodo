using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class MigrationRunConfiguration : IEntityTypeConfiguration<MigrationRun>
{
    public void Configure(EntityTypeBuilder<MigrationRun> builder)
    {
        builder.ToTable("MigrationRuns");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SourceDescription).IsRequired().HasMaxLength(500);

        var logComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        builder.Property(r => r.LogEntries)
            .HasConversion(
                entries => JsonSerializer.Serialize(entries, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(logComparer);
    }
}
