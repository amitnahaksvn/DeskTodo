namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references an export profile ID that doesn't exist.</summary>
public sealed class ExportProfileNotFoundException(Guid profileId)
    : Exception($"No export profile was found with id '{profileId}'.")
{
    public Guid ProfileId { get; } = profileId;
}
