namespace DeskTodo.App.ViewModels;

/// <summary>A Focus Context choice for the task editor's dropdown (Feature 63, Roadmap-39-100.md), including the "no context" option.</summary>
public sealed record ContextOption(Guid? Id, string Name)
{
    public static readonly ContextOption None = new(null, "No context");
}
