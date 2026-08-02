using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeskTodo.App.Views;

public partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow()
    {
        InitializeComponent();
    }

    /// <summary>Shows the dialog centered over <paramref name="owner"/> and returns true only if the user clicked the confirm button — the OS close button and Cancel both resolve to false, so callers never need to special-case "closed without answering."</summary>
    public static Task<bool> ShowAsync(Window owner, string title, string message, string confirmText = "Delete")
    {
        var dialog = new ConfirmDialogWindow();
        dialog.TitleTextBlock.Text = title;
        dialog.MessageTextBlock.Text = message;
        dialog.ConfirmButton.Content = confirmText;
        return dialog.ShowDialog<bool>(owner);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);
}
