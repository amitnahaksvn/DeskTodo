using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class ProjectTemplateConfiguration : IEntityTypeConfiguration<ProjectTemplate>
{
    public void Configure(EntityTypeBuilder<ProjectTemplate> builder)
    {
        builder.ToTable("ProjectTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(4000);

        // Same manual JSON-string conversion as TaskTemplateConfiguration.ChecklistItems,
        // extended to a list of small value objects rather than plain strings — these items
        // never exist outside their owning template, so a JSON column keeps them exactly as
        // "part of the template's shape" rather than needing their own table + foreign key.
        var taskItemsComparer = new ValueComparer<List<ProjectTemplateTaskItem>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, i) => HashCode.Combine(hash, i.Title, i.Priority, i.DayOffsetStart, i.DurationDays)),
            v => v.ToList());

        builder.Property(t => t.TaskItems)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<ProjectTemplateTaskItem>() : JsonSerializer.Deserialize<List<ProjectTemplateTaskItem>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(taskItemsComparer);

        var milestoneItemsComparer = new ValueComparer<List<ProjectTemplateMilestoneItem>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, i) => HashCode.Combine(hash, i.Title, i.DayOffset)),
            v => v.ToList());

        builder.Property(t => t.MilestoneItems)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<ProjectTemplateMilestoneItem>() : JsonSerializer.Deserialize<List<ProjectTemplateMilestoneItem>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(milestoneItemsComparer);
    }
}
