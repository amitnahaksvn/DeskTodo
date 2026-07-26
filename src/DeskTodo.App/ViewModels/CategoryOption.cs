namespace DeskTodo.App.ViewModels;

/// <summary>A category choice for the task editor's dropdown, including the "no category" option.</summary>
public sealed record CategoryOption(Guid? Id, string Name, string? ColorHex)
{
    public static readonly CategoryOption None = new(null, "No category", null);
}
