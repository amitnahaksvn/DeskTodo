namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown by <c>TaskService.CompleteTaskAsync</c> when the task has an incomplete blocking dependency — see <see cref="Entities.TaskItem.IsBlocked"/>.</summary>
public sealed class TaskBlockedException(Guid taskId)
    : Exception($"Task '{taskId}' can't be completed — it's still blocked by an incomplete task.")
{
    public Guid TaskId { get; } = taskId;
}
