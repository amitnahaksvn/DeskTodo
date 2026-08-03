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
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<PlannerViewModel>();
        services.AddTransient<WeekViewModel>();
        services.AddTransient<YearViewModel>();
        services.AddTransient<AgendaViewModel>();
        services.AddTransient<TimelineViewModel>();
        services.AddTransient<KanbanViewModel>();
        services.AddTransient<MatrixViewModel>();
        services.AddTransient<GoalsViewModel>();
        services.AddTransient<MilestonesViewModel>();

        return services;
    }
}
