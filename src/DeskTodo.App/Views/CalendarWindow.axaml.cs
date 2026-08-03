using Avalonia.Controls;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class CalendarWindow : Window
{
    public CalendarWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is CalendarViewModel viewModel)
        {
            viewModel.DateSelected += OnDateSelected;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is CalendarViewModel viewModel)
        {
            viewModel.DateSelected -= OnDateSelected;
        }

        base.OnClosed(e);
    }

    /// <summary>Set as a field here (not raised straight through to WidgetWindow) so WidgetWindow only needs to know "a date was picked," not reach into this window's own event wiring — see OnCalendarViewRequested.</summary>
    public DateOnly? SelectedDate { get; private set; }

    private void OnDateSelected(object? sender, DateOnly date)
    {
        SelectedDate = date;
        Close();
    }
}
