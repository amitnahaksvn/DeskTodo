using Avalonia.Controls;
using Avalonia.Input.Platform;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class ClipboardHistoryWindow : Window
{
    // Tracks the one open instance so every entry point (the tray menu, the Command
    // Palette) shares it rather than each opening its own window over the same
    // DI-singleton ClipboardHistoryViewModel — see ShowOrActivate, the same pattern
    // FocusTimerWindow uses.
    private static ClipboardHistoryWindow? _current;

    public ClipboardHistoryWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ClipboardHistoryViewModel viewModel)
        {
            viewModel.CopyBackRequested += OnCopyBackRequested;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ClipboardHistoryViewModel viewModel)
        {
            viewModel.CopyBackRequested -= OnCopyBackRequested;
        }

        if (_current == this)
        {
            _current = null;
        }

        base.OnClosed(e);
    }

    private async void OnCopyBackRequested(object? sender, string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public static void ShowOrActivate(ClipboardHistoryViewModel viewModel)
    {
        if (_current is not null)
        {
            _current.Activate();
            return;
        }

        _current = new ClipboardHistoryWindow { DataContext = viewModel };
        _current.Show();
    }
}
