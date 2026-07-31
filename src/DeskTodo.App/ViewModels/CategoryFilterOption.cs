namespace DeskTodo.App.ViewModels;

/// <summary>
/// A category choice for the search bar's filter dropdown. Distinct from
/// <see cref="CategoryOption"/> (the task editor's "assign this category" choice) because
/// the default/sentinel means something different here — "don't filter" rather than
/// "no category assigned."
/// </summary>
public sealed record CategoryFilterOption(Guid? Id, string Name)
{
    public static readonly CategoryFilterOption All = new(null, "All Categories");

    /// <summary>
    /// The search bar's category <c>ComboBox</c> uses no <c>ItemTemplate</c>/<c>DisplayMemberBinding</c>
    /// (see WidgetWindow.axaml) — its closed-state selection box only ever rendered blank
    /// with either of those set, even though the open dropdown list rendered correctly with
    /// them (an Avalonia ComboBox quirk). Falling back to the default ToString()-based
    /// template — the same mechanism the Status/Sort enum ComboBoxes rely on, and the only
    /// one confirmed to render the closed box — sidesteps it.
    /// </summary>
    public override string ToString() => Name;
}
