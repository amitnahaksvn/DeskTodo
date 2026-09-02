using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IActivityTimelineService"/>
public sealed class ActivityTimelineService(
    ITaskHistoryRepository taskHistoryRepository,
    IFocusSessionService focusSessionService,
    IGoalRepository goalRepository) : IActivityTimelineService
{
    public async Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var entries = new List<ActivityEntry>();

        var history = await taskHistoryRepository.GetAllAsync(cancellationToken);
        foreach (var entry in history)
        {
            var taskTitle = entry.Task?.Title ?? "(deleted task)";
            var description = entry.Action switch
            {
                TaskHistoryAction.Created => $"Created \"{taskTitle}\"",
                TaskHistoryAction.Completed => $"Completed \"{taskTitle}\"",
                TaskHistoryAction.Reopened => $"Reopened \"{taskTitle}\"",
                TaskHistoryAction.Archived => $"Archived \"{taskTitle}\"",
                TaskHistoryAction.Restored => $"Restored \"{taskTitle}\"",
                TaskHistoryAction.Deleted => $"Deleted \"{taskTitle}\"",
                TaskHistoryAction.Renamed => $"Renamed \"{entry.OldValue}\" to \"{entry.NewValue}\"",
                TaskHistoryAction.Updated => $"Updated {entry.FieldName} on \"{taskTitle}\"",
                _ => $"{entry.Action} \"{taskTitle}\"",
            };
            entries.Add(new ActivityEntry(entry.Timestamp, "Task", description));
        }

        var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);
        foreach (var session in sessions)
        {
            var subject = session.Task?.Title is { } title ? $" on \"{title}\"" : string.Empty;
            entries.Add(new ActivityEntry(session.EndedAt, "Focus", $"Focus session: {session.DurationMinutes}m{subject}"));
        }

        var goals = await goalRepository.GetAllAsync(cancellationToken);
        foreach (var goal in goals)
        {
            foreach (var completion in goal.Completions)
            {
                var timestamp = completion.CompletedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                entries.Add(new ActivityEntry(timestamp, "Goal", $"Completed goal \"{goal.Name}\" for {completion.CompletedDate:MMM d}"));
            }
        }

        return entries.OrderByDescending(e => e.Timestamp).Take(limit).ToList();
    }
}
