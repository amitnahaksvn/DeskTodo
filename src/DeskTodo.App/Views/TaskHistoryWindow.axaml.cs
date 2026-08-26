using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeskTodo.App.Views;

public partial class TaskHistoryWindow : Window
{
    public TaskHistoryWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
