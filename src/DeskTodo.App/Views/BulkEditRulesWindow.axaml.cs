using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class BulkEditRulesWindow : Window
{
    public BulkEditRulesWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is BulkEditRulesViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
