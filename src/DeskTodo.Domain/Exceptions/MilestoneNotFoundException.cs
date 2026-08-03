namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a milestone ID that doesn't exist.</summary>
public sealed class MilestoneNotFoundException(Guid milestoneId)
    : Exception($"No milestone was found with id '{milestoneId}'.")
{
    public Guid MilestoneId { get; } = milestoneId;
}
