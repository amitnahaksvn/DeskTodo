using Avalonia.Controls;
using Avalonia.Input;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class QuickAddWindow : Window
{
    public QuickAddWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is QuickAddViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    /// <summary>Focuses the title box every time the window is (re)activated — Quick Add is meant to be typed into immediately, and it can be summoned again from the tray while already open.</summary>
    private void OnActivated(object? sender, EventArgs e) => TitleTextBox.Focus();

    private void OnTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not QuickAddViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.AddCommand.Execute(null);
                break;
            case Key.Escape:
                viewModel.CancelCommand.Execute(null);
                break;
        }
    }
}
