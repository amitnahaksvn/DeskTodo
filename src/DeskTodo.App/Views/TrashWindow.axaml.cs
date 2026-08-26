using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class TrashWindow : Window
{
    public TrashWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is TrashViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    // Delete Forever / Empty Trash are gated behind ConfirmDialogWindow here in code-behind
    // rather than TrashViewModel showing the dialog itself — same "no ViewModel in this app
    // owns a Window reference" split every other destructive action already uses (see
    // WidgetWindow.OnDeleteTaskClick).
    private async void OnDeleteForeverClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: TrashedTaskOption task } || DataContext is not TrashViewModel viewModel)
        {
            return;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete forever?",
            $"\"{task.Title}\" will be permanently deleted. This can't be undone.");
        if (confirmed)
        {
            await viewModel.DeleteForeverCommand.ExecuteAsync(task.Id);
        }
    }

    private async void OnEmptyTrashClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TrashViewModel viewModel)
        {
            return;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Empty Trash?",
            $"All {viewModel.DeletedTasks.Count} task(s) in Trash will be permanently deleted. This can't be undone.");
        if (confirmed)
        {
            await viewModel.EmptyTrashCommand.ExecuteAsync(null);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
