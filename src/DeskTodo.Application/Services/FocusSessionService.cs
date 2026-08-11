using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IFocusSessionService"/>
public sealed class FocusSessionService(IFocusSessionRepository focusSessionRepository, ITaskService taskService) : IFocusSessionService
{
    public async Task<FocusSession> CompleteSessionAsync(
        FocusSessionType type,
        DateTime startedAt,
        DateTime endedAt,
        int durationMinutes,
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var session = new FocusSession
        {
            Type = type,
            TaskId = taskId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMinutes = durationMinutes,
        };
        await focusSessionRepository.AddAsync(session, cancellationToken);

        if (taskId is { } id)
        {
            await taskService.AddActualMinutesAsync(id, durationMinutes, cancellationToken);
        }

        return session;
    }

    public Task<IReadOnlyList<FocusSession>> GetSessionsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        focusSessionRepository.GetByTaskIdAsync(taskId, cancellationToken);

    public Task<IReadOnlyList<FocusSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default) =>
        focusSessionRepository.GetAllAsync(cancellationToken);
}
