namespace DeskTodo.Application.Settings;

/// <summary>
/// A named, user-saved grid preset (see <see cref="AppSettings.GridSavedViews"/>) — both
/// which columns are hidden (matching the scope of the single auto-persisted layout,
/// <see cref="AppSettings.HiddenGridColumns"/>, that the column half of this generalizes)
/// and, as of Phase 25, the grid's filter bar state (search text, status/category/project/
/// smart-list selections). Column width/order/sort still aren't captured, for the same
/// reason the single-layout version doesn't capture them. Phase 25's "Saved Searches" is
/// deliberately not a second, parallel concept — a saved filter combination and a saved
/// column layout are both just "what the grid currently looks like," so one named preset
/// covers both rather than asking a user to juggle two similar-but-different lists.
/// </summary>
public sealed class GridSavedView
{
    public required string Name { get; set; }

    public List<string> HiddenColumns { get; set; } = [];

    public string? SearchText { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? ProjectId { get; set; }

    /// <summary>The App-layer <c>TaskStatusFilter</c> enum's name, stored as a plain string since this Settings class lives in Application and can't reference an App-layer type — parsed back with <c>Enum.TryParse</c> by the caller.</summary>
    public string StatusFilter { get; set; } = "All";

    /// <summary>The App-layer <c>GridSmartFilter</c> enum's name — same string-not-enum reasoning as <see cref="StatusFilter"/>.</summary>
    public string SmartFilter { get; set; } = "None";
}
