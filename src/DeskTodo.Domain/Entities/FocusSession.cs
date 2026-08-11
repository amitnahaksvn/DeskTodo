using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// A completed (or manually stopped) timer session — Phase 23's Time Tracking log. Written
/// once, at the moment a session ends, not ticked/updated live; <see cref="DurationMinutes"/>
/// is the source of truth for how much focus time to credit (it's the actual accumulated
/// running time, which can be less than <see cref="EndedAt"/> minus <see cref="StartedAt"/>
/// if the session was paused), while <see cref="StartedAt"/>/<see cref="EndedAt"/> are the
/// wall-clock bounds for a history view. Optionally linked to a <see cref="TaskItem"/> — a
/// session doesn't have to be "about" a task (a generic Pomodoro/Stopwatch run is valid on
/// its own) — and when it is, completing it adds <see cref="DurationMinutes"/> onto that
/// task's <see cref="TaskItem.ActualMinutes"/> (see <c>IFocusSessionService.CompleteSessionAsync</c>).
/// </summary>
public sealed class FocusSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required FocusSessionType Type { get; set; }

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public required DateTime StartedAt { get; set; }

    public required DateTime EndedAt { get; set; }

    public required int DurationMinutes { get; set; }
}
