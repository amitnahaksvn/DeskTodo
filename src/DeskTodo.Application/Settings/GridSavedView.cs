namespace DeskTodo.Application.Settings;

/// <summary>
/// A named, user-saved grid column layout (see <see cref="AppSettings.GridSavedViews"/>) —
/// just which columns are hidden, matching the scope of the single auto-persisted layout
/// (<see cref="AppSettings.HiddenGridColumns"/>) this generalizes. Column width/order/sort
/// aren't captured, for the same reason the single-layout version doesn't capture them.
/// </summary>
public sealed class GridSavedView
{
    public required string Name { get; set; }

    public List<string> HiddenColumns { get; set; } = [];
}
