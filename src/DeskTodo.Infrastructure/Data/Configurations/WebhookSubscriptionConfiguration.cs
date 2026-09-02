using System.Text.Json;
using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(2000);

        // Same manual JSON-string conversion as TaskGroupConfiguration.TemplateIds — see that
        // class's comment for why this is preferred over EF Core's newer built-in
        // primitive-collection mapping on this project's pinned SQLite provider version.
        var eventTypesComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            v => v.ToList());

        builder.Property(w => w.EventTypes)
            .HasConversion(
                types => JsonSerializer.Serialize(types, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(eventTypesComparer);

        var headersComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => new Dictionary<string, string>(v));

        builder.Property(w => w.Headers)
            .HasConversion(
                headers => JsonSerializer.Serialize(headers, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(headersComparer);
    }
}
