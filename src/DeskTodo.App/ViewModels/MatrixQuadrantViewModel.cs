using System.Collections.ObjectModel;

namespace DeskTodo.App.ViewModels;

/// <summary>One of <see cref="MatrixViewModel"/>'s four Eisenhower quadrants.</summary>
public sealed class MatrixQuadrantViewModel(string title, string subtitle)
{
    public string Title { get; } = title;

    public string Subtitle { get; } = subtitle;

    public ObservableCollection<PlannerTaskRowViewModel> Tasks { get; } = [];
}
