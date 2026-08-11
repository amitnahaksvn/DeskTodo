using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class AnalyticsWindow : Window
{
    private static readonly FilePickerFileType MarkdownFileType = new("Markdown file") { Patterns = ["*.md"] };

    public AnalyticsWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is AnalyticsViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private async void OnCopyReportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AnalyticsViewModel viewModel)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(viewModel.ReportText);
        }
    }

    private async void OnSaveReportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AnalyticsViewModel viewModel)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save report",
            SuggestedFileName = "desktodo-report.md",
            DefaultExtension = "md",
            FileTypeChoices = [MarkdownFileType],
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(viewModel.ReportText);
    }
}
