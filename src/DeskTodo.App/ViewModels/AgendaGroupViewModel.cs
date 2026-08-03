namespace DeskTodo.App.ViewModels;

/// <summary>One date's worth of rows in <see cref="AgendaViewModel"/>'s scrollable list.</summary>
public sealed class AgendaGroupViewModel(DateOnly date, string dateLabel, IReadOnlyList<PlannerTaskRowViewModel> tasks)
{
    public DateOnly Date { get; } = date;

    public string DateLabel { get; } = dateLabel;

    public IReadOnlyList<PlannerTaskRowViewModel> Tasks { get; } = tasks;
}
