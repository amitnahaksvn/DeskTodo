using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class PlanningInsightsWindow : Window
{
    public PlanningInsightsWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is PlanningInsightsViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
