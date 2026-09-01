using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeskTodo.App.Views;

public partial class TaskVersionWindow : Window
{
    public TaskVersionWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
