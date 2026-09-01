using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeskTodo.App.Views;

public partial class IntegrityCheckWindow : Window
{
    public IntegrityCheckWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
