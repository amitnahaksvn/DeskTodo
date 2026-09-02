using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class InboxWindow : Window
{
    public InboxWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is InboxViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnCaptureKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is InboxViewModel viewModel)
        {
            viewModel.CaptureCommand.Execute(null);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
