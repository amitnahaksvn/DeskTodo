using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.EventType).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(l => l.WebhookId);

        // Cascade (unlike TaskHistory's SetNull): a delivery log has no value once its own
        // webhook subscription is deleted — there's no "audit trail that should survive its
        // parent" reason to keep it, unlike TaskHistory surviving a deleted task.
        builder.HasOne<WebhookSubscription>()
            .WithMany()
            .HasForeignKey(l => l.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
