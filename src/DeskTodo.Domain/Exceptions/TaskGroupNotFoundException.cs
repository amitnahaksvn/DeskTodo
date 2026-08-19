namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a task group ID that doesn't exist.</summary>
public sealed class TaskGroupNotFoundException(Guid groupId)
    : Exception($"No task group was found with id '{groupId}'.")
{
    public Guid GroupId { get; } = groupId;
}
