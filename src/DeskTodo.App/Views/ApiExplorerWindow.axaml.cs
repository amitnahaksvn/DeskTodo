using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class ApiExplorerWindow : Window
{
    public ApiExplorerWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ApiExplorerViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private async void OnCopyResponseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ApiExplorerViewModel viewModel)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(viewModel.ResponseText);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
