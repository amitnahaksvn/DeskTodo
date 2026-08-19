using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class TaskGroupWindow : Window
{
    public TaskGroupWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is TaskGroupViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
