using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DeskTodo.App.ViewModels;

namespace DeskTodo.App.Views;

public partial class ExportProfilesWindow : Window
{
    public ExportProfilesWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ExportProfilesViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }

    private async void OnRunProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExportProfilesViewModel viewModel || sender is not Button { Tag: ExportProfileRow row })
        {
            return;
        }

        viewModel.SelectedProfile = row;

        var fileType = new FilePickerFileType(row.Format.ToString()) { Patterns = [$"*.{viewModel.SelectedProfileExtension}"] };
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Run \"{row.Name}\"",
            SuggestedFileName = $"{row.Name}.{viewModel.SelectedProfileExtension}",
            DefaultExtension = viewModel.SelectedProfileExtension,
            FileTypeChoices = [fileType],
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await viewModel.RunSelectedProfileAsync(stream);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
