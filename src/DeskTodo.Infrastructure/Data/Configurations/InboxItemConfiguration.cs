using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class InboxItemConfiguration : IEntityTypeConfiguration<InboxItem>
{
    public void Configure(EntityTypeBuilder<InboxItem> builder)
    {
        builder.ToTable("InboxItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Content).IsRequired().HasMaxLength(4000);

        builder.HasOne(i => i.ConvertedTask)
            .WithMany()
            .HasForeignKey(i => i.ConvertedTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.Status);
    }
}
