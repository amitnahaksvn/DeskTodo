using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class CommandPaletteWindow : Window
{
    public CommandPaletteWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is CommandPaletteViewModel viewModel)
        {
            viewModel.CloseRequested += OnCloseRequested;
        }

        SearchBox.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is CommandPaletteViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        base.OnClosed(e);
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.ExecuteSelectedCommand.Execute(null);
                break;
            case Key.Escape:
                viewModel.CancelCommand.Execute(null);
                break;
            case Key.Down:
                // Lets arrow-down move focus into the list from the search box without the
                // TextBox itself eating the keystroke first — ListBox handles subsequent
                // Up/Down navigation on its own once focused.
                EntriesListBox.Focus();
                break;
        }
    }

    private void OnEntryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel viewModel)
        {
            viewModel.ExecuteSelectedCommand.Execute(null);
        }
    }
}
