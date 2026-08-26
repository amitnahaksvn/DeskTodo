using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// One <see cref="TaskHistory"/> row as shown in <see cref="TaskHistoryViewModel"/>'s timeline —
/// <see cref="Description"/> and <see cref="TimestampDisplay"/> are already-formatted display
/// strings (same "no converter needed" approach as <see cref="TrashedTaskOption"/>).
/// </summary>
public sealed record TaskHistoryEntryOption(string Description, string TimestampDisplay)
{
    public static TaskHistoryEntryOption FromEntity(TaskHistory entry)
    {
        var description = entry.Action switch
        {
            TaskHistoryAction.Created => "Created",
            TaskHistoryAction.Completed => "Completed",
            TaskHistoryAction.Reopened => "Reopened",
            TaskHistoryAction.Archived => "Archived",
            TaskHistoryAction.Restored => "Restored",
            TaskHistoryAction.Deleted => "Deleted",
            TaskHistoryAction.Renamed => $"Renamed from \"{entry.OldValue}\" to \"{entry.NewValue}\"",
            TaskHistoryAction.Updated => $"{entry.FieldName} changed from \"{Blank(entry.OldValue)}\" to \"{Blank(entry.NewValue)}\"",
            _ => entry.Action.ToString(),
        };

        var timestampDisplay = entry.Timestamp.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt");
        return new TaskHistoryEntryOption(description, timestampDisplay);
    }

    private static string Blank(string? value) => string.IsNullOrEmpty(value) ? "(none)" : value;
}
