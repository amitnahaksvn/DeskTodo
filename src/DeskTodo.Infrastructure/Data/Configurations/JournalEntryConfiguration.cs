using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title).IsRequired().HasMaxLength(300);
        builder.Property(j => j.Content).IsRequired().HasMaxLength(20000);
        builder.Property(j => j.Mood).HasMaxLength(50);

        builder.HasIndex(j => j.Date);
    }
}
