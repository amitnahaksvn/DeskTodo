namespace DeskTodo.Application.Settings;

/// <summary>
/// Feature 83 (Roadmap-39-100.md), generalizing <see cref="GridSavedView"/>'s same "what does
/// this view currently look like" shape to the widget's own day-list search/filter/sort bar,
/// per this feature's own note to reuse that shape rather than inventing a parallel one.
/// </summary>
public sealed class WidgetSavedView
{
    public required string Name { get; set; }

    public string? SearchText { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? TagId { get; set; }

    public Guid? ProjectId { get; set; }

    /// <summary>The App-layer <c>TaskStatusFilter</c> enum's name, stored as a plain string — same reasoning as <see cref="GridSavedView.StatusFilter"/>.</summary>
    public string StatusFilter { get; set; } = "All";

    /// <summary>The App-layer <c>TaskSortOption</c> enum's name.</summary>
    public string SortOption { get; set; } = "Manual";
}
