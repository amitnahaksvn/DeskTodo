using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class RecurringProjectScheduleConfiguration : IEntityTypeConfiguration<RecurringProjectSchedule>
{
    public void Configure(EntityTypeBuilder<RecurringProjectSchedule> builder)
    {
        builder.ToTable("RecurringProjectSchedules");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.GeneratedProjectNamePattern).HasMaxLength(400);
        builder.Property(s => s.ColorHex).IsRequired().HasMaxLength(7);

        builder.HasOne(s => s.ProjectTemplate)
            .WithMany()
            .HasForeignKey(s => s.ProjectTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        var idsComparer = new ValueComparer<List<Guid>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
            v => v.ToList());

        builder.Property(s => s.GeneratedProjectIds)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(idsComparer);
    }
}
