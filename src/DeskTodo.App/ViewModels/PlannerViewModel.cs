using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Hosts Phase 21's remaining planner tabs — Week, Year, Agenda, Timeline, Kanban, Matrix,
/// Goals, Milestones — behind one window with a <c>TabControl</c> (see
/// <c>PlannerWindow.axaml</c>), rather than eight separate windows/header icons. This
/// composes eight small, independent sub-ViewModels (each already usable on its own)
/// instead of being one large ViewModel with everything inlined — every tab's data loads
/// together in <see cref="LoadAsync"/>, and picking a date in *any* date-bearing tab
/// bubbles up through this class's own <see cref="DateSelected"/> so <c>WidgetWindow</c>
/// only needs to handle one event. Goals and Milestones don't raise it — a goal has no
/// single day, and clicking a milestone row triggers its own Toggle/Delete buttons rather
/// than navigating the widget anywhere.
/// </summary>
public sealed partial class PlannerViewModel : ViewModelBase
{
    public PlannerViewModel(WeekViewModel week, YearViewModel year, AgendaViewModel agenda, TimelineViewModel timeline, KanbanViewModel kanban, MatrixViewModel matrix, GoalsViewModel goals, MilestonesViewModel milestones)
    {
        Week = week;
        Year = year;
        Agenda = agenda;
        Timeline = timeline;
        Kanban = kanban;
        Matrix = matrix;
        Goals = goals;
        Milestones = milestones;

        Week.DateSelected += (_, date) => DateSelected?.Invoke(this, date);
        Year.DateSelected += (_, date) => DateSelected?.Invoke(this, date);
        Agenda.DateSelected += (_, date) => DateSelected?.Invoke(this, date);
        Timeline.DateSelected += (_, date) => DateSelected?.Invoke(this, date);
        Matrix.DateSelected += (_, date) => DateSelected?.Invoke(this, date);
    }

    public WeekViewModel Week { get; }

    public YearViewModel Year { get; }

    public AgendaViewModel Agenda { get; }

    public TimelineViewModel Timeline { get; }

    public KanbanViewModel Kanban { get; }

    public MatrixViewModel Matrix { get; }

    public GoalsViewModel Goals { get; }

    public MilestonesViewModel Milestones { get; }

    public event EventHandler<DateOnly>? DateSelected;

    public event EventHandler? CloseRequested;

    public async Task LoadAsync(DateOnly? initialDate = null, CancellationToken cancellationToken = default)
    {
        await Week.LoadAsync(initialDate, cancellationToken);
        await Year.LoadAsync(initialDate, cancellationToken);
        await Agenda.LoadAsync(cancellationToken);
        await Timeline.LoadAsync(cancellationToken);
        await Kanban.LoadAsync(cancellationToken);
        await Matrix.LoadAsync(cancellationToken);
        await Goals.LoadAsync(cancellationToken);
        await Milestones.LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
