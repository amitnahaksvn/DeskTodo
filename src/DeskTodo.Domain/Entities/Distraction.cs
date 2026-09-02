using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>Feature 64 (Roadmap-39-100.md) — a logged interruption, optionally scoped to a running <see cref="FocusSession"/>.</summary>
public sealed class Distraction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    /// <summary>Minutes, set once <see cref="EndedAt"/> is known — null while the distraction is still ongoing.</summary>
    public int? DurationMinutes { get; set; }

    public required DistractionCategory Category { get; set; }

    public string? Notes { get; set; }

    /// <summary>The focus session this interruption happened during, if any — a distraction can also be logged with no active session.</summary>
    public Guid? FocusSessionId { get; set; }

    public FocusSession? FocusSession { get; set; }

    public void End(DateTime endedAt)
    {
        EndedAt = endedAt;
        DurationMinutes = Math.Max(1, (int)Math.Round((endedAt - StartedAt).TotalMinutes));
    }
}
