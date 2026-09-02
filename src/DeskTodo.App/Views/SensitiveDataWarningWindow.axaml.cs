using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class SensitiveDataWarningWindow : Window
{
    public SensitiveDataWarningWindow()
    {
        InitializeComponent();
    }

    /// <summary>Shows the dialog centered over <paramref name="owner"/>. Closing via the OS close button (or any path other than the three buttons below) resolves to <see cref="SensitiveDataPromptResult.Cancelled"/> — the save stays aborted, same "never silently proceed" convention as <see cref="ConfirmDialogWindow"/>.</summary>
    public static async Task<SensitiveDataPromptResult> ShowAsync(Window owner, IReadOnlyList<TaskFieldSensitiveMatch> matches)
    {
        var dialog = new SensitiveDataWarningWindow();
        dialog.MatchesItemsControl.ItemsSource = matches.Select(m => m.DisplayText).ToList();
        // ShowDialog<T> returns default(T) — null for this reference type — if closed via the OS
        // close button rather than one of the three buttons below, so that's coalesced to the
        // same Cancelled result OnCancelClick already produces.
        return await dialog.ShowDialog<SensitiveDataPromptResult?>(owner) ?? SensitiveDataPromptResult.Cancelled;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(SensitiveDataPromptResult.Cancelled);

    private void OnKeepAnywayClick(object? sender, RoutedEventArgs e) =>
        Close(new SensitiveDataPromptResult(ShouldSave: true, RemoveFlagged: false, DontWarnAgainCheckBox.IsChecked == true));

    private void OnRemoveClick(object? sender, RoutedEventArgs e) =>
        Close(new SensitiveDataPromptResult(ShouldSave: true, RemoveFlagged: true, DontWarnAgainCheckBox.IsChecked == true));
}
