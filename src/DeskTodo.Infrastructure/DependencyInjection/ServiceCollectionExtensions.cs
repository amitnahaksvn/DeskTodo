using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using DeskTodo.Application.Services;
using DeskTodo.Application.Updates;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.ImportExport;
using DeskTodo.Infrastructure.Repositories;
using DeskTodo.Infrastructure.Storage;
using DeskTodo.Infrastructure.Updates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeskTodo.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Infrastructure-layer services (options binding, EF Core/SQLite
/// persistence, settings persistence, file watching) into the composition
/// root's container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppStorageOptions>()
            .Bind(configuration.GetSection(AppStorageOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.RootDirectory))
                {
                    options.RootDirectory = AppStoragePaths.ResolveDefaultRootDirectory();
                }

                Directory.CreateDirectory(options.RootDirectory);
            });

        // A factory (rather than a single shared DbContext) is used because this is
        // a long-running desktop process, not a per-request web app: EF Core's own
        // guidance for WPF/Avalonia-style apps is to create a short-lived context per
        // unit of work via IDbContextFactory, since one context shared for the app's
        // whole lifetime is neither thread-safe nor bounded in change-tracker growth.
        services.AddDbContextFactory<DeskTodoDbContext>((serviceProvider, optionsBuilder) =>
        {
            var storage = serviceProvider.GetRequiredService<IOptions<AppStorageOptions>>().Value;
            var databasePath = Path.Combine(storage.RootDirectory, storage.DatabaseFileName);
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        });

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskHistoryRepository, TaskHistoryRepository>();
        services.AddScoped<ITaskVersionRepository, TaskVersionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<ITaskTemplateRepository, TaskTemplateRepository>();
        services.AddScoped<ITaskGroupRepository, TaskGroupRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ITaskDependencyRepository, TaskDependencyRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<IFocusSessionRepository, FocusSessionRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IChecklistService, ChecklistService>();
        services.AddScoped<ITaskTemplateService, TaskTemplateService>();
        services.AddScoped<ITaskGroupService, TaskGroupService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ITaskDependencyService, TaskDependencyService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IMilestoneService, MilestoneService>();
        services.AddScoped<IFocusSessionService, FocusSessionService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ITaskExportService, TaskExportService>();
        services.AddSingleton<ITaskImportService, TaskImportService>();
        services.AddScoped<IBackupService, LocalBackup.BackupService>();
        services.AddScoped<IDataIntegrityService, DataIntegrityService>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IInboxService, InboxService>();
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IActivityTimelineService, ActivityTimelineService>();
        services.AddScoped<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddSingleton<IQuickAddParser, RuleBasedQuickAddParser>();
        services.AddSingleton<IMeetingActionExtractor, RuleBasedMeetingActionExtractor>();
        services.AddScoped<IPlanningAnalyticsService, PlanningAnalyticsService>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<IDecisionService, DecisionService>();
        services.AddScoped<IJournalRepository, JournalRepository>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IFocusContextRepository, FocusContextRepository>();
        services.AddScoped<IDistractionRepository, DistractionRepository>();
        services.AddScoped<IAchievementService, AchievementService>();

        // Singleton — the undo/redo stack is app-wide state that must survive across
        // WidgetViewModel's own DI lifetime the same way FocusTimerViewModel's timer does.
        services.AddSingleton<IUndoRedoService, UndoRedoService>();

        // A single shared instance rather than the full IHttpClientFactory machinery
        // (which would need a new Microsoft.Extensions.Http package reference) — this app
        // makes exactly one kind of outbound call, on-demand from a Settings button, not a
        // high-frequency hot path the factory's connection-pooling/DNS-refresh behavior is
        // meant to protect. Still a singleton, not one-per-check, to avoid socket exhaustion
        // from repeated ad-hoc HttpClient construction.
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IUpdateCheckService, GitHubUpdateCheckService>();

        return services;
    }
}
