using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>Time-tracking use cases: logging a completed timer session and reading a task's session history.</summary>
public interface IFocusSessionService
{
    /// <summary>
    /// Logs a completed (or manually stopped) session and, if <paramref name="taskId"/> is
    /// given, adds <paramref name="durationMinutes"/> onto that task's
    /// <see cref="TaskItem.ActualMinutes"/> (see <see cref="ITaskService.AddActualMinutesAsync"/>).
    /// Not called for sessions abandoned before completing at least one minute — see the
    /// caller's own guard (<c>FocusTimerViewModel</c>).
    /// </summary>
    Task<FocusSession> CompleteSessionAsync(
        FocusSessionType type,
        DateTime startedAt,
        DateTime endedAt,
        int durationMinutes,
        Guid? taskId = null,
        CancellationToken cancellationToken = default);

    /// <summary>A task's own logged sessions, most recent first — for the full-field editor's time-tracking section.</summary>
    Task<IReadOnlyList<FocusSession>> GetSessionsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Every session ever logged, most recent first — raw material for Phase 24's analytics aggregation.</summary>
    Task<IReadOnlyList<FocusSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default);
}
