using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Feature 39's Task Inbox / Capture Queue use cases.</summary>
public interface IInboxService
{
    Task<InboxItem> CaptureAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>The working queue — unprocessed items, oldest first.</summary>
    Task<IReadOnlyList<InboxItem>> GetUnprocessedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an inbox item into a real task on <paramref name="planDate"/> and marks the
    /// item Converted. The new task starts plain (no priority/category/due date/tags) — those
    /// are set afterward through the normal full-field editor, not duplicated here.
    /// </summary>
    Task<TaskItem> ConvertToTaskAsync(Guid inboxItemId, DateOnly planDate, CancellationToken cancellationToken = default);

    Task ArchiveAsync(Guid inboxItemId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid inboxItemId, CancellationToken cancellationToken = default);
}
