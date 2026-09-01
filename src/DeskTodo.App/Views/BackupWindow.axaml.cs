using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class BackupWindow : Window
{
    public BackupWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is BackupViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    /// <summary>
    /// Gated behind ConfirmDialogWindow — same "no ViewModel owns a Window" split every other
    /// destructive action in this app already uses (see TrashWindow.OnDeleteForeverClick).
    /// Restoring overwrites the live database, so this is the most consequential confirmation
    /// in the app.
    /// </summary>
    private async void OnConfirmRestoreClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BackupViewModel viewModel)
        {
            return;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Restore this backup?",
            "This replaces the current database with the selected backup. A safety backup of the current data is taken first.");
        if (confirmed)
        {
            await viewModel.ConfirmRestoreCommand.ExecuteAsync(null);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
