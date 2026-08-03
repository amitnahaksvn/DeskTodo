namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a goal ID that doesn't exist.</summary>
public sealed class GoalNotFoundException(Guid goalId)
    : Exception($"No goal was found with id '{goalId}'.")
{
    public Guid GoalId { get; } = goalId;
}
