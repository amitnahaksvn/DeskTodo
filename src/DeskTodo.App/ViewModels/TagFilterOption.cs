namespace DeskTodo.App.ViewModels;

/// <summary>A tag choice for the search bar's filter dropdown — mirrors <see cref="CategoryFilterOption"/>'s shape and reasoning.</summary>
public sealed record TagFilterOption(Guid? Id, string Name)
{
    public static readonly TagFilterOption All = new(null, "All Tags");

    /// <summary>See <see cref="CategoryFilterOption.ToString"/> for why this exists (an Avalonia ComboBox closed-box rendering quirk).</summary>
    public override string ToString() => Name;
}
