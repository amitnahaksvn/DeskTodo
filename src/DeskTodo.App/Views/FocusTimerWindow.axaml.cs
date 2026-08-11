using Avalonia.Controls;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class FocusTimerWindow : Window
{
    // Tracks the one open instance so every entry point (the widget header's icon, the
    // full-field editor's "Start Timer" button) shares it rather than each opening its own
    // window over the same DI-singleton FocusTimerViewModel — see ShowOrActivate.
    private static FocusTimerWindow? _current;

    public FocusTimerWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is FocusTimerViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_current == this)
        {
            _current = null;
        }

        base.OnClosed(e);
    }

    /// <summary>Opens the Focus Timer window, or brings the already-open one forward — never two at once, since both would just be two views over the same running (or not-yet-started) session.</summary>
    public static void ShowOrActivate(FocusTimerViewModel viewModel)
    {
        if (_current is not null)
        {
            _current.Activate();
            return;
        }

        _current = new FocusTimerWindow { DataContext = viewModel };
        _current.Show();
    }
}
