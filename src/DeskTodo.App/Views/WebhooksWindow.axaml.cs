using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class WebhooksWindow : Window
{
    public WebhooksWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is WebhooksViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnWebhookRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: WebhookRow row } && DataContext is WebhooksViewModel viewModel)
        {
            viewModel.SelectedWebhook = row;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
