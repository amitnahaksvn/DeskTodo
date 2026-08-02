using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DeskTodo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.Views;

public partial class WidgetWindow : Window
{
    // Tracks the row being dragged for reordering. A private field (rather than routing
    // the task's Guid through Avalonia's IDataTransfer/DataFormat payload machinery) is
    // enough because this drag never leaves the window it started in — DoDragDropAsync is
    // still used for the actual gesture (visual feedback, DragOver/Drop routing), just not
    // for carrying the payload.
    private Guid? _draggedTaskId;

    public WidgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is WidgetViewModel viewModel)
        {
            viewModel.TaskEditRequested += OnTaskEditRequested;
            viewModel.SettingsRequested += OnSettingsRequested;
            viewModel.GridViewRequested += OnGridViewRequested;
            _ = viewModel.LoadTasksAsync();
        }
    }

    // Bounds are captured here (before the window actually closes) rather than in
    // OnClosed, since Position/Width/Height are meaningless to read once the window has
    // torn down. Saved with GetAwaiter().GetResult() — blocking briefly on a local JSON
    // write during shutdown, the same pattern Program.cs uses for the database migration —
    // rather than fire-and-forget, since fire-and-forget here could easily lose the write
    // to process exit. Wrapped in Task.Run for the same reason as App.axaml.cs's
    // LoadSettingsAsync call — blocking the UI thread on an un-decoupled async chain risks
    // the same deadlock class confirmed live at startup.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is WidgetViewModel viewModel)
        {
            var left = Position.X;
            var top = Position.Y;
            var width = Width;
            var height = Height;
            Task.Run(() => viewModel.SaveWindowBoundsAsync(left, top, width, height)).GetAwaiter().GetResult();
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is WidgetViewModel viewModel)
        {
            viewModel.TaskEditRequested -= OnTaskEditRequested;
            viewModel.SettingsRequested -= OnSettingsRequested;
            viewModel.GridViewRequested -= OnGridViewRequested;
        }

        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }

    private async void OnSettingsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new SettingsWindow { DataContext = settingsViewModel };
        settingsViewModel.Saved += (_, _) => settingsWindow.Close();
        settingsViewModel.CancelRequested += (_, _) => settingsWindow.Close();

        await settingsViewModel.LoadAsync();
        await settingsWindow.ShowDialog(this);

        // Re-applies live even on Cancel — cheap, and correct either way: Cancel didn't
        // persist anything, so this just reloads the same settings that were already active.
        await viewModel.LoadSettingsAsync();
        App.ApplyAccentColor(viewModel.AccentColorHex);

        // Also reloads tasks: an Import/Export round trip through Settings (see
        // SettingsWindow's "Import / Export tasks…" button) may have added tasks for the
        // day currently being viewed.
        await viewModel.LoadTasksAsync();
    }

    private async void OnGridViewRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var gridViewModel = App.Services.GetRequiredService<GridViewModel>();
        var gridWindow = new GridWindow { DataContext = gridViewModel };
        gridViewModel.CloseRequested += (_, _) => gridWindow.Close();

        await gridViewModel.LoadAsync();
        await gridWindow.ShowDialog(this);

        // Grid edits (title/date/priority/category/due/completed/notes, or deletes) may
        // have changed what belongs on the day currently being viewed.
        await viewModel.LoadTasksAsync();
    }

    private async void OnTaskEditRequested(object? sender, Guid taskId)
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

        if (DataContext is WidgetViewModel viewModel)
        {
            await viewModel.LoadTasksAsync();
        }
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
        if (sender is not Control { DataContext: TaskItemViewModel taskItem } titleBlock)
        {
            return;
        }

        taskItem.BeginEditCommand.Execute(null);

        // The edit TextBox becomes visible as a side effect of the command above, but
        // toggling IsVisible doesn't detach/reattach it from the visual tree, so there's
        // no "just appeared" lifecycle event to hook — focusing has to be deferred past
        // this layout pass instead of attempted immediately.
        if (titleBlock.GetVisualParent() is Control row)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var editBox = row.GetVisualChildren().OfType<TextBox>().FirstOrDefault();
                editBox?.Focus();
                editBox?.SelectAll();
            }, DispatcherPriority.Loaded);
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

    private async void OnDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem } handle)
        {
            return;
        }

        if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _draggedTaskId = taskItem.Id;
        await DragDrop.DoDragDropAsync(e, new DataTransfer(), DragDropEffects.Move);
        _draggedTaskId = null;
    }

    private void OnRowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _draggedTaskId.HasValue ? DragDropEffects.Move : DragDropEffects.None;
    }

    private async void OnRowDrop(object? sender, DragEventArgs e)
    {
        if (_draggedTaskId is not { } draggedId ||
            sender is not Control { DataContext: TaskItemViewModel targetItem } ||
            DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        await viewModel.ReorderAsync(draggedId, targetItem.Id);
    }

    // Delete is gated behind ConfirmDialogWindow here in code-behind rather than
    // TaskItemViewModel/WidgetViewModel showing the dialog themselves — no ViewModel in
    // this app owns a Window reference (see TaskEditRequested/SettingsRequested/
    // GridViewRequested, all handled the same way), so the confirm step lives on the same
    // side as every other dialog hand-off. DeleteCommand/BulkDeleteCommand themselves are
    // unchanged; this only gates *invoking* them.
    private async void OnDeleteTaskClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem })
        {
            return;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete task?",
            $"\"{taskItem.Title}\" will be deleted. This can't be undone from here.");
        if (confirmed)
        {
            await taskItem.DeleteCommand.ExecuteAsync(null);
        }
    }

    private async void OnBulkDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var count = viewModel.SelectedCount;
        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete selected tasks?",
            $"{count} selected {(count == 1 ? "task" : "tasks")} will be deleted. This can't be undone from here.");
        if (confirmed)
        {
            await viewModel.BulkDeleteCommand.ExecuteAsync(null);
        }
    }
}
