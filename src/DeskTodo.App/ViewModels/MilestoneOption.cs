namespace DeskTodo.App.ViewModels;

/// <summary>A milestone choice for the full-field editor's "Milestone" picker — the same shape as <see cref="TaskOption"/>, kept as its own type since a "which milestone" choice reads oddly typed as a <c>TaskOption</c>.</summary>
public sealed record MilestoneOption(Guid? Id, string Title)
{
    public static readonly MilestoneOption None = new(null, "None");

    public override string ToString() => Title;
}
