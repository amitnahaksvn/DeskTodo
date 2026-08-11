namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a project ID that doesn't exist.</summary>
public sealed class ProjectNotFoundException(Guid projectId)
    : Exception($"No project was found with id '{projectId}'.")
{
    public Guid ProjectId { get; } = projectId;
}
