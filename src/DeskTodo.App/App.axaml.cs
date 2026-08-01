using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DeskTodo.App.DesignTime;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.App;

// The base type is fully qualified (rather than "using Avalonia;" + bare
// "Application") because this project's own namespace, DeskTodo.App, is
// a sibling of the DeskTodo.Application project's namespace under the
// shared "DeskTodo" root. C# resolves that sibling namespace member
// before an unqualified "Application" would ever reach Avalonia's type,
// producing CS0118. Fully qualifying avoids the ambiguity everywhere it
// would otherwise recur (e.g. future Avalonia.Application.Current calls).
public partial class App : global::Avalonia.Application
{
    /// <summary>
    /// The application's root DI container. Assigned by <see cref="Program"/>
    /// once the generic host has started, before Avalonia's desktop lifetime
    /// begins. Left null at XAML-designer time, where <see cref="OnFrameworkInitializationCompleted"/>
    /// never runs against a real container.
    /// </summary>
    public static IServiceProvider? Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var widgetViewModel = Services?.GetRequiredService<WidgetViewModel>()
                ?? new WidgetViewModel(
                    new DesignTimeTaskService(),
                    new DesignTimeCategoryRepository(),
                    new DesignTimeTagService(),
                    new DesignTimeTaskTemplateService(),
                    new DesignTimeSettingsService(),
                    new NullNotificationService(),
                    TimeProvider.System,
                    NullLogger<WidgetViewModel>.Instance,
                    NullLogger<TaskItemViewModel>.Instance);

            // Loaded synchronously (blocking on a local JSON file read, same pattern as
            // Program.cs's database migration) so the window's first frame already has the
            // right accent color and position — doing this later in OnOpened would show the
            // window at its default bounds/color first, then visibly jump/flash.
            widgetViewModel.LoadSettingsAsync().GetAwaiter().GetResult();

            var widgetWindow = new WidgetWindow { DataContext = widgetViewModel };

            if (widgetViewModel.WindowWidth is { } width && widgetViewModel.WindowHeight is { } height)
            {
                widgetWindow.Width = width;
                widgetWindow.Height = height;
            }

            if (widgetViewModel.WindowLeft is { } left && widgetViewModel.WindowTop is { } top)
            {
                widgetWindow.Position = new PixelPoint((int)left, (int)top);
            }

            ApplyAccentColor(widgetViewModel.AccentColorHex);

            desktop.MainWindow = widgetWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Overrides Avalonia's <c>SystemAccentColor</c> resource, which the Fluent theme
    /// threads through default control accenting (e.g. the progress bar fill) — called
    /// at startup here and again from <c>WidgetWindow</c> after the Settings window saves
    /// a new color, so the running widget re-colors without needing a restart.
    /// </summary>
    public static void ApplyAccentColor(string hex)
    {
        if (Color.TryParse(hex, out var color))
        {
            Current!.Resources["SystemAccentColor"] = color;
        }
    }
}