namespace DeskTodo.App.ViewModels;

/// <summary>A same-day task choice — used by the full-field editor's "Parent task" and "Blocked by" pickers.</summary>
public sealed record TaskOption(Guid? Id, string Title)
{
    public static readonly TaskOption None = new(null, "None");

    public override string ToString() => Title;
}
