using Avalonia.Controls;
using Avalonia.Input;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class LockScreenWindow : Window
{
    private bool _unlocked;

    public LockScreenWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is LockScreenViewModel viewModel)
        {
            viewModel.Unlocked += OnUnlocked;
        }

        PinBox.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LockScreenViewModel viewModel)
        {
            viewModel.Unlocked -= OnUnlocked;
        }

        base.OnClosed(e);
    }

    private void OnUnlocked(object? sender, EventArgs e)
    {
        _unlocked = true;
        Close();
    }

    /// <summary>Refuses to close via the OS close button (or any other non-programmatic close) unless the PIN was actually entered correctly, or the app is genuinely quitting via the tray's "Quit" item — see <see cref="App.IsQuitting"/>. Otherwise the close button would just be a bypass for the whole feature.</summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_unlocked && !App.IsQuitting)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    private void OnPinKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LockScreenViewModel viewModel)
        {
            viewModel.UnlockCommand.Execute(null);
        }
    }
}
