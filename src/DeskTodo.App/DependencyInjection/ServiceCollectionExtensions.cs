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
        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<QuickAddViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<TaskGroupViewModel>();
        services.AddTransient<TrashViewModel>();
        services.AddTransient<TaskHistoryViewModel>();
        services.AddTransient<TaskVersionViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<IntegrityCheckViewModel>();
        services.AddTransient<InboxViewModel>();
        services.AddTransient<ArchiveViewModel>();
        services.AddTransient<ActivityTimelineViewModel>();
        services.AddTransient<DatabaseMaintenanceViewModel>();
        services.AddTransient<WorkSessionHistoryViewModel>();
        services.AddTransient<PlanningInsightsViewModel>();
        services.AddTransient<DecisionLogViewModel>();
        services.AddTransient<JournalViewModel>();
        services.AddTransient<AchievementsViewModel>();
        services.AddTransient<DistractionLogViewModel>();
        services.AddTransient<ContextsViewModel>();
        services.AddTransient<KeyboardShortcutsViewModel>();
        services.AddTransient<MeetingSessionViewModel>();
        services.AddTransient<TaskGraphViewModel>();
        services.AddTransient<WebhooksViewModel>();
        services.AddTransient<ApiExplorerViewModel>();

        // Singleton, not transient: a running timer is app-wide state that must keep
        // ticking (and stay reflected in the widget header's indicator) whether or not
        // FocusTimerWindow is currently open — see FocusTimerViewModel's own doc comment.
        services.AddSingleton<FocusTimerViewModel>();

        // Singleton, same reasoning as FocusTimerViewModel above: the clipboard poll keeps
        // accumulating history in the background whether or not ClipboardHistoryWindow is
        // open — see ClipboardHistoryViewModel's own doc comment.
        services.AddSingleton<ClipboardHistoryViewModel>();

        // Singleton — Feature 40's "recent commands" list (Roadmap-39-100.md) needs to survive
        // across separate Cmd/Ctrl+K summons within the same session; a fresh instance per
        // summon would reset it every time. See CommandPaletteViewModel's own doc comment.
        services.AddSingleton<CommandPaletteViewModel>();

        return services;
    }
}
