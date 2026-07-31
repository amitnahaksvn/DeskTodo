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
