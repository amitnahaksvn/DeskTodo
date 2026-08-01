using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class GridWindow : Window
{
    /// <summary>Maps each "Columns" flyout checkbox to the persisted setting name (see <see cref="GridViewModel.HideableColumnNames"/>) and its <see cref="DataGrid.Columns"/> index. Index-based rather than name-based lookup into <c>Columns</c>, since that collection stays in definition order regardless of the user drag-reordering columns (<c>DisplayIndex</c> tracks visual position separately).</summary>
    private static readonly (string CheckBoxName, string ColumnName, int ColumnIndex)[] ColumnVisibilityMap =
    [
        ("CategoryColumnCheckBox", "Category", 4),
        ("DueColumnCheckBox", "Due", 5),
        ("NotesColumnCheckBox", "Notes", 7),
        ("StatusColumnCheckBox", "Status", 8),
        ("ProgressColumnCheckBox", "Progress", 9),
    ];

    public GridWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var hidden = await viewModel.GetHiddenColumnsAsync();
        foreach (var (_, columnName, columnIndex) in ColumnVisibilityMap)
        {
            TasksGrid.Columns[columnIndex].IsVisible = !hidden.Contains(columnName);
        }
    }

    /// <summary>Syncs the flyout's checkboxes to the actual column visibility — deferred to here (rather than <see cref="OnOpened"/>) since a <c>Flyout</c>'s content isn't realized until it's actually shown, so <c>FindControl</c> would find nothing before then.</summary>
    private async void OnColumnsFlyoutOpened(object? sender, EventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var hidden = await viewModel.GetHiddenColumnsAsync();
        foreach (var (checkBoxName, columnName, _) in ColumnVisibilityMap)
        {
            if (this.FindControl<CheckBox>(checkBoxName) is { } checkBox)
            {
                checkBox.IsChecked = !hidden.Contains(columnName);
            }
        }
    }

    private async void OnColumnVisibilityChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Name: { } name } checkBox || DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var mapping = Array.Find(ColumnVisibilityMap, m => m.CheckBoxName == name);
        if (mapping.ColumnName is null)
        {
            return;
        }

        var isVisible = checkBox.IsChecked == true;
        TasksGrid.Columns[mapping.ColumnIndex].IsVisible = isVisible;
        await viewModel.SetColumnHiddenAsync(mapping.ColumnName, isHidden: !isVisible);
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(viewModel.BuildClipboardText());
    }

    private async void OnPasteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var text = await clipboard.TryGetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            await viewModel.PasteFromClipboardAsync(text);
        }
    }
}
