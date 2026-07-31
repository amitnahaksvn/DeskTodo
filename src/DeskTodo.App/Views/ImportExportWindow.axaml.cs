using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;

namespace DeskTodo.App.Views;

public partial class ImportExportWindow : Window
{
    private static readonly FilePickerFileType CsvFileType = new("CSV file") { Patterns = ["*.csv"] };
    private static readonly FilePickerFileType JsonFileType = new("JSON file") { Patterns = ["*.json"] };
    private static readonly FilePickerFileType MarkdownFileType = new("Markdown file") { Patterns = ["*.md"] };
    private static readonly FilePickerFileType ExcelFileType = new("Excel workbook") { Patterns = ["*.xlsx"] };

    public ImportExportWindow()
    {
        InitializeComponent();
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ImportExportViewModel viewModel)
        {
            return;
        }

        var fileType = viewModel.SelectedExportFormat switch
        {
            TaskExportFormat.Csv => CsvFileType,
            TaskExportFormat.Json => JsonFileType,
            TaskExportFormat.Markdown => MarkdownFileType,
            TaskExportFormat.Excel => ExcelFileType,
            _ => CsvFileType,
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export tasks",
            SuggestedFileName = $"desktodo-export.{viewModel.SelectedExportExtension}",
            DefaultExtension = viewModel.SelectedExportExtension,
            FileTypeChoices = [fileType],
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await viewModel.ExportToAsync(stream);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ImportExportViewModel viewModel)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import tasks",
            AllowMultiple = false,
            FileTypeFilter = [CsvFileType, JsonFileType],
        });

        var file = files.Count > 0 ? files[0] : null;
        if (file is null)
        {
            return;
        }

        var format = Path.GetExtension(file.Name).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? TaskImportFormat.Json
            : TaskImportFormat.Csv;

        await using var stream = await file.OpenReadAsync();
        await viewModel.ImportFromAsync(stream, format);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
