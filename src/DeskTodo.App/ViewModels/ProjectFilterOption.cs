namespace DeskTodo.App.ViewModels;

/// <summary>A project choice for the search bar's filter dropdown — same "distinct sentinel meaning" reasoning as <see cref="CategoryFilterOption"/> vs. <see cref="ProjectOption"/>.</summary>
public sealed record ProjectFilterOption(Guid? Id, string Name)
{
    public static readonly ProjectFilterOption All = new(null, "All Projects");

    /// <summary>See <see cref="CategoryFilterOption.ToString"/> for why this overrides ToString rather than relying on an ItemTemplate.</summary>
    public override string ToString() => Name;
}
