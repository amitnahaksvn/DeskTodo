namespace DeskTodo.App.ViewModels;

/// <summary>A project choice for the full-field editor's "Project" picker — same shape as <see cref="MilestoneOption"/>, kept as its own type for the same "reads oddly typed as something else" reason.</summary>
public sealed record ProjectOption(Guid? Id, string Name)
{
    public static readonly ProjectOption None = new(null, "None");

    public override string ToString() => Name;
}
