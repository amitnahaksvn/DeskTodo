namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a task ID that doesn't exist.</summary>
public sealed class TaskNotFoundException(Guid taskId)
    : Exception($"No task was found with id '{taskId}'.")
{
    public Guid TaskId { get; } = taskId;
}
