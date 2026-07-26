using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class WidgetWindow : Window
{
    public WidgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is WidgetViewModel viewModel)
        {
            _ = viewModel.LoadTasksAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }

    // The window has no title bar (SystemDecorations="None"), so the header
    // area itself drives moving it — the standard Avalonia pattern for
    // borderless/chromeless windows.
    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e) => Close();

    private void OnAddTaskKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is WidgetViewModel viewModel)
        {
            viewModel.AddTaskCommand.Execute(null);
        }
    }

    private void OnTitleDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: TaskItemViewModel taskItem })
        {
            taskItem.BeginEditCommand.Execute(null);
        }
    }

    private void OnEditTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem })
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                taskItem.CommitEditCommand.Execute(null);
                break;
            case Key.Escape:
                taskItem.CancelEditCommand.Execute(null);
                break;
        }
    }
}
