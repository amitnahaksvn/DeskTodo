namespace DeskTodo.Domain.Enums;

/// <summary>
/// What kind of change a <see cref="Entities.TaskHistory"/> row records. Deliberately a small,
/// fixed set — see <see cref="Entities.TaskHistory"/>'s own doc comment for which task actions
/// are (and, just as deliberately, are not) recorded.
/// </summary>
public enum TaskHistoryAction
{
    Created,
    Renamed,
    Updated,
    Completed,
    Reopened,
    Archived,
    Restored,
    Deleted,
}
