using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.Views;

public partial class TaskGraphWindow : Window
{
    public TaskGraphWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TaskGraphViewModel viewModel)
        {
            viewModel.OpenTaskRequested += OnOpenTaskRequested;
        }
    }

    private async void OnOpenTaskRequested(object? sender, Guid taskId)
    {
        if (App.Services is null)
        {
            return;
        }

        var editViewModel = App.Services.GetRequiredService<TaskEditViewModel>();
        var editWindow = new TaskEditWindow { DataContext = editViewModel };
        editViewModel.Saved += (_, _) => editWindow.Close();
        editViewModel.CancelRequested += (_, _) => editWindow.Close();

        await editViewModel.LoadAsync(taskId);
        await editWindow.ShowDialog(this);

        if (DataContext is TaskGraphViewModel viewModel)
        {
            await viewModel.LoadAsync(taskId);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
