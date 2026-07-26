using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DeskTodo.App.DesignTime;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
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
                ?? new WidgetViewModel(new DesignTimeTaskService(), NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

            desktop.MainWindow = new WidgetWindow
            {
                DataContext = widgetViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}