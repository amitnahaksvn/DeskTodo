using Avalonia.Controls;
using Avalonia.Interactivity;
using DeskTodo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.OpenUrlRequested += OnOpenUrlRequested;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.OpenUrlRequested -= OnOpenUrlRequested;
        }

        base.OnClosed(e);
    }

    /// <summary>Phase 30's "View Release" button — opens the GitHub release page in the OS default browser via Avalonia's cross-platform launcher, the same mechanism <c>TaskEditWindow.OnAttachmentOpenRequested</c> already uses for files.</summary>
    private async void OnOpenUrlRequested(object? sender, string url)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            await topLevel.Launcher.LaunchUriAsync(new Uri(url));
        }
    }

    private async void OnImportExportClick(object? sender, RoutedEventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var importExportViewModel = App.Services.GetRequiredService<ImportExportViewModel>();
        var importExportWindow = new ImportExportWindow { DataContext = importExportViewModel };
        await importExportWindow.ShowDialog(this);
    }
}
