using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeskTodo.App.Views;

public partial class MeetingModeWindow : Window
{
    public MeetingModeWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
