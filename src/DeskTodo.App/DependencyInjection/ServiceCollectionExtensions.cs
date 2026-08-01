using DeskTodo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.DependencyInjection;

/// <summary>
/// Registers Avalonia-facing services: ViewModels and, as pages are added
/// in later phases, their supporting navigation/dialog services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeskTodoApp(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<WidgetViewModel>();
        services.AddTransient<TaskEditViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ImportExportViewModel>();
        services.AddTransient<GridViewModel>();

        return services;
    }
}
