using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class DistractionConfiguration : IEntityTypeConfiguration<Distraction>
{
    public void Configure(EntityTypeBuilder<Distraction> builder)
    {
        builder.ToTable("Distractions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Notes).HasMaxLength(2000);

        // SetNull, not Cascade — same "the log outlives the session it happened during"
        // reasoning as FocusSession.TaskId (Phase 23).
        builder.HasOne(d => d.FocusSession)
            .WithMany()
            .HasForeignKey(d => d.FocusSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.StartedAt);
    }
}
