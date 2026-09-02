using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class KeyboardShortcutsWindow : Window
{
    public KeyboardShortcutsWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is KeyboardShortcutsViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    /// <summary>
    /// While <see cref="KeyboardShortcutsViewModel.CapturingCommandId"/> is set, the next key
    /// press (that includes the platform's primary modifier — see
    /// <see cref="KeyboardShortcutDefinition.TryFormatCombo"/>) becomes that shortcut's new
    /// combo. A bare Escape cancels capture without changing anything.
    /// </summary>
    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not KeyboardShortcutsViewModel viewModel || viewModel.CapturingCommandId is not { } commandId)
        {
            return;
        }

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            viewModel.CancelCaptureCommand.Execute(null);
            return;
        }

        var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        var combo = KeyboardShortcutDefinition.TryFormatCombo(e.Key, e.KeyModifiers, modifier);
        if (combo is not null)
        {
            await viewModel.ApplyCapturedComboAsync(commandId, combo);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
