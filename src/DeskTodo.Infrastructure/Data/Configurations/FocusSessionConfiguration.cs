using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("FocusSessions");
        builder.HasKey(s => s.Id);

        // SetNull, not Cascade: a session's own history (what kind, how long, when) is
        // still meaningful after its linked task is deleted — the same reasoning as
        // Milestone's SetNull FK on TaskItem (Phase 21).
        builder.HasOne(s => s.Task)
            .WithMany()
            .HasForeignKey(s => s.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.TaskId);
        builder.HasIndex(s => s.StartedAt);
    }
}
