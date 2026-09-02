using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class ProjectTemplatesWindow : Window
{
    public ProjectTemplatesWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ProjectTemplatesViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
