using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using DeskTodo.Application.Services;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.ImportExport;
using DeskTodo.Infrastructure.Repositories;
using DeskTodo.Infrastructure.Storage;
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
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<ITaskTemplateRepository, TaskTemplateRepository>();
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

        return services;
    }
}
