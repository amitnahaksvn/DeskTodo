using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class MassImportWindow : Window
{
    private static readonly FilePickerFileType CsvFileType = new("CSV file") { Patterns = ["*.csv"] };

    private IStorageFile? _selectedFile;

    public MassImportWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MassImportViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private async void OnChooseFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MassImportViewModel viewModel)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a CSV file to import",
            AllowMultiple = false,
            FileTypeFilter = [CsvFileType],
        });

        _selectedFile = files.Count > 0 ? files[0] : null;
        if (_selectedFile is null)
        {
            return;
        }

        await using var stream = await _selectedFile.OpenReadAsync();
        await viewModel.LoadHeadersAsync(stream, _selectedFile.Name);
    }

    private async void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MassImportViewModel viewModel || _selectedFile is null)
        {
            return;
        }

        await using var stream = await _selectedFile.OpenReadAsync();
        await viewModel.PreviewAsync(stream);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MassImportViewModel viewModel || _selectedFile is null)
        {
            return;
        }

        await using var stream = await _selectedFile.OpenReadAsync();
        await viewModel.ImportAsync(stream);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
