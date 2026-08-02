using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;
using DeskTodo.Application.Settings;

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

        await ApplyHiddenColumnsToGridAsync(viewModel);
        TasksGrid.FrozenColumnCount = await viewModel.GetColumnsFrozenAsync() ? 2 : 0;
    }

    /// <summary>Applies the persisted hidden-column set to the live <c>DataGrid</c> — shared by <see cref="OnOpened"/> and <see cref="OnApplyViewClick"/>, since applying a saved view changes the same underlying setting <see cref="OnOpened"/> reads at startup.</summary>
    private async Task ApplyHiddenColumnsToGridAsync(GridViewModel viewModel)
    {
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

        if (this.FindControl<CheckBox>("FreezeColumnsCheckBox") is { } freezeCheckBox)
        {
            freezeCheckBox.IsChecked = await viewModel.GetColumnsFrozenAsync();
        }
    }

    private async void OnFreezeColumnsChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var isFrozen = checkBox.IsChecked == true;
        TasksGrid.FrozenColumnCount = isFrozen ? 2 : 0;
        await viewModel.SetColumnsFrozenAsync(isFrozen);
    }

    /// <summary>Syncs the "Views" flyout's list to the persisted saved views — same realized-on-open timing reasoning as <see cref="OnColumnsFlyoutOpened"/>.</summary>
    private async void OnViewsFlyoutOpened(object? sender, EventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var views = await viewModel.GetSavedViewsAsync();
        SavedViewsListBox.ItemsSource = views.Select(v => v.Name).ToList();
    }

    private async void OnApplyViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel || SavedViewsListBox.SelectedItem is not string name)
        {
            return;
        }

        await viewModel.ApplyViewAsync(name);
        await ApplyHiddenColumnsToGridAsync(viewModel);
    }

    private async void OnDeleteViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel || SavedViewsListBox.SelectedItem is not string name)
        {
            return;
        }

        await viewModel.DeleteSavedViewAsync(name);
        var views = await viewModel.GetSavedViewsAsync();
        SavedViewsListBox.ItemsSource = views.Select(v => v.Name).ToList();
    }

    private async void OnSaveViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var name = NewViewNameTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await viewModel.SaveCurrentViewAsync(name);
        NewViewNameTextBox.Text = string.Empty;
        var views = await viewModel.GetSavedViewsAsync();
        SavedViewsListBox.ItemsSource = views.Select(v => v.Name).ToList();
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

    /// <summary>Gated behind ConfirmDialogWindow the same way as the widget's per-row/bulk delete — see WidgetWindow.axaml.cs's OnDeleteTaskClick/OnBulkDeleteClick doc comment.</summary>
    private async void OnDeleteSelectedClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GridViewModel viewModel)
        {
            return;
        }

        var count = viewModel.SelectedCount;
        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete selected tasks?",
            $"{count} selected {(count == 1 ? "task" : "tasks")} will be deleted. This can't be undone from here.");
        if (confirmed)
        {
            await viewModel.DeleteSelectedCommand.ExecuteAsync(null);
        }
    }
}
