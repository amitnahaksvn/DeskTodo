using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Platform.Mac;
using DeskTodo.Platform.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.DependencyInjection;

/// <summary>
/// Picks the right platform implementation of each Platform-layer abstraction at startup.
/// This is the one place allowed to know about both <c>DeskTodo.Platform.Windows</c> and
/// <c>DeskTodo.Platform.Mac</c> — everything else (ViewModels, the abstractions themselves)
/// only ever sees the Application-layer interface.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddDeskTodoPlatform(this IServiceCollection services)
    {
        if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<INotificationService, MacNotificationService>();
            services.AddSingleton<IAutoStartService, MacAutoStartService>();
            services.AddSingleton<IGlobalHotkeyService, MacGlobalHotkeyService>();
        }
        else if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<INotificationService, WindowsNotificationService>();
            services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
            services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        }
        else
        {
            services.AddSingleton<INotificationService, NullNotificationService>();
            services.AddSingleton<IAutoStartService, NullAutoStartService>();
            services.AddSingleton<IGlobalHotkeyService, NullGlobalHotkeyService>();
        }

        return services;
    }
}
