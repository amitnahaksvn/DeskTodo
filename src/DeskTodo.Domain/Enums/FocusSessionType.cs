namespace DeskTodo.Domain.Enums;

/// <summary>
/// Which timer mechanism produced a <see cref="Entities.FocusSession"/>. "Focus Timer,"
/// "Focus Mode," and "Deep Work Session" from the original wishlist are all the same
/// underlying mechanism (a countdown to zero) at different default durations — a UI-level
/// preset choice, not a reason for three more enum members — so they all map to
/// <see cref="CountdownTimer"/>. See docs/ARCHITECTURE.md's "Phase 23" section.
/// </summary>
public enum FocusSessionType
{
    /// <summary>Alternating work/break countdown cycles.</summary>
    Pomodoro = 0,

    /// <summary>Open-ended count-up with no target duration.</summary>
    Stopwatch = 1,

    /// <summary>A single countdown to zero — "Focus Timer"/"Focus Mode"/"Deep Work Session" are this at different preset lengths.</summary>
    CountdownTimer = 2,
}
