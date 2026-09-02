namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a project template ID that doesn't exist.</summary>
public sealed class ProjectTemplateNotFoundException(Guid templateId)
    : Exception($"No project template was found with id '{templateId}'.")
{
    public Guid TemplateId { get; } = templateId;
}
