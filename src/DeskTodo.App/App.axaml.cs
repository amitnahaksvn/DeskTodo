using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DeskTodo.App.DesignTime;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Set just before the tray icon's "Quit" item calls <c>TryShutdown</c> — the one signal
    /// <see cref="Views.WidgetWindow.OnClosing"/> needs to tell "the user wants to genuinely
    /// exit" apart from "the user clicked the widget's own close button," which now hides it
    /// to the tray instead (see the "Minimize to Tray" deliverable, Phase 22).
    /// </summary>
    public static bool IsQuitting { get; set; }

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
                    new DesignTimeProjectService(),
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
            //
            // Wrapped in Task.Run rather than awaited-then-blocked directly: at this point
            // in startup (inside OnFrameworkInitializationCompleted, called synchronously
            // from AppBuilder.SetupWithLifetime, *before* the classic desktop lifetime's own
            // dispatcher loop has started pumping), the Avalonia UI thread's dispatcher isn't
            // processing posted work yet. A plain .GetAwaiter().GetResult() on the ambient
            // thread deadlocks forever the moment any awaited step inside LoadSettingsAsync
            // actually yields (a genuine, reproducible hang confirmed live via a managed dump
            // — the main thread parked at this exact line, every thread-pool worker idle).
            // Task.Run hands the whole async chain to a thread-pool thread with no captured
            // dispatcher, so blocking on *that* task here is safe.
            Task.Run(() => widgetViewModel.LoadSettingsAsync()).GetAwaiter().GetResult();

            var widgetWindow = new WidgetWindow { DataContext = widgetViewModel };

            if (widgetViewModel.WindowWidth is { } width && widgetViewModel.WindowHeight is { } height)
            {
                widgetWindow.Width = width;
                widgetWindow.Height = height;
            }

            // A chosen monitor (Phase 22's "Multi Monitor Support") wins over the raw saved
            // WindowLeft/Top — those coordinates go stale the moment the monitor arrangement
            // changes, whereas re-resolving the monitor by identity and re-centering on it
            // stays correct across reboots/reconnects as long as that same monitor is present.
            var preferredScreen = MonitorIdentity.Resolve(widgetWindow.Screens, widgetViewModel.PreferredMonitorId);
            if (preferredScreen is not null)
            {
                var area = preferredScreen.WorkingArea;
                widgetWindow.Position = new PixelPoint(
                    area.X + (area.Width - (int)widgetWindow.Width) / 2,
                    area.Y + (area.Height - (int)widgetWindow.Height) / 2);
            }
            else if (widgetViewModel.WindowLeft is { } left && widgetViewModel.WindowTop is { } top)
            {
                widgetWindow.Position = new PixelPoint((int)left, (int)top);
            }

            ApplyAccentColor(widgetViewModel.AccentColorHex);
            ApplyTheme(widgetViewModel.Theme);
            widgetViewModel.IsDarkTheme = widgetWindow.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

            // Windows never auto-exits the app just because a window closed — the tray
            // icon's "Quit" item (SetupTrayIcon below) is the only path to a real shutdown,
            // matching "Minimize to Tray" (Phase 22): closing the widget hides it instead.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Tray/hotkey are wired unconditionally, against the widgetWindow instance,
            // *before* deciding whether to show it or a lock screen first — a locked session
            // still needs a working "Quit" path (see LockScreenWindow.OnClosing), and the
            // widget itself is fully built either way, just not yet visible.
            SetupTrayIcon(desktop, widgetWindow, widgetViewModel);
            SetupGlobalHotkey(desktop, widgetWindow, widgetViewModel);

            desktop.MainWindow = (Window?)TrySetupLockScreen(widgetWindow) ?? widgetWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Phase 29's PIN Lock — returns a <see cref="LockScreenWindow"/> to show first (instead
    /// of <paramref name="widgetWindow"/>) when <see cref="DeskTodo.Application.Settings.AppSettings.PinLockEnabled"/>
    /// is on and a PIN is actually set, or null to proceed straight to the widget (either PIN
    /// Lock is off, or this is the design-time/no-DI path). <paramref name="widgetWindow"/> is
    /// already fully constructed either way — unlocking just calls <c>Show()</c>/<c>Activate()</c>
    /// on it, no further setup needed.
    /// </summary>
    private static LockScreenWindow? TrySetupLockScreen(WidgetWindow widgetWindow)
    {
        if (Services is null)
        {
            return null;
        }

        var settingsService = Services.GetRequiredService<ISettingsService>();
        var settings = Task.Run(() => settingsService.LoadAsync()).GetAwaiter().GetResult();

        if (!settings.PinLockEnabled || string.IsNullOrEmpty(settings.PinHash))
        {
            return null;
        }

        var lockScreenViewModel = new LockScreenViewModel(settingsService, Services.GetRequiredService<ILogger<LockScreenViewModel>>());
        var lockScreenWindow = new LockScreenWindow { DataContext = lockScreenViewModel };
        lockScreenViewModel.Unlocked += (_, _) =>
        {
            widgetWindow.Show();
            widgetWindow.Activate();
        };

        return lockScreenWindow;
    }

    /// <summary>
    /// Phase 22's tray icon (Windows) / menu bar item (macOS) — Avalonia's <see cref="TrayIcon"/>
    /// is the same cross-platform API for both, confirmed via reflection against the pinned
    /// 12.1.0 package before use, the same discipline as every other Avalonia API this
    /// project depends on. Built directly here (not raised as a WidgetViewModel event the
    /// way Settings/Grid/Calendar/Planner are) since the tray icon isn't owned by or
    /// scoped to the widget window's own lifecycle — it exists independently, for as long
    /// as the app runs, widget hidden or not.
    /// </summary>
    private static void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, WidgetWindow widgetWindow, WidgetViewModel widgetViewModel)
    {
        var icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://DeskTodo/Assets/avalonia-logo.ico"))));

        var toggleVisibilityItem = new NativeMenuItem("Show/Hide Widget");
        toggleVisibilityItem.Click += (_, _) => ToggleWidgetVisibility(widgetWindow);

        var quickAddItem = new NativeMenuItem("Quick Add…");
        quickAddItem.Click += (_, _) => OpenQuickAdd(widgetWindow, widgetViewModel);

        var settingsItem = new NativeMenuItem("Settings…");
        settingsItem.Click += (_, _) => widgetViewModel.OpenSettingsCommand.Execute(null);

        var clipboardHistoryItem = new NativeMenuItem("Clipboard History…");
        clipboardHistoryItem.Click += (_, _) => widgetViewModel.OpenClipboardHistoryCommand.Execute(null);

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) =>
        {
            IsQuitting = true;
            desktop.TryShutdown();
        };

        var trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "DeskTodo",
            Menu = new NativeMenu
            {
                toggleVisibilityItem,
                quickAddItem,
                clipboardHistoryItem,
                settingsItem,
                new NativeMenuItemSeparator(),
                quitItem,
            },
        };
        trayIcon.Clicked += (_, _) => ToggleWidgetVisibility(widgetWindow);

        TrayIcon.SetIcons(Current!, new TrayIcons { trayIcon });
    }

    private static void ToggleWidgetVisibility(Window widgetWindow)
    {
        if (widgetWindow.IsVisible)
        {
            widgetWindow.Hide();
        }
        else
        {
            widgetWindow.Show();
            widgetWindow.Activate();
        }
    }

    /// <summary>Opens Quick Add from the tray — always re-shows the widget first if it's hidden, since Quick Add's own "created task" feedback (Phase 22) is a row appearing in the widget's list, which would be invisible otherwise.</summary>
    private static void OpenQuickAdd(WidgetWindow widgetWindow, WidgetViewModel widgetViewModel)
    {
        if (!widgetWindow.IsVisible)
        {
            widgetWindow.Show();
        }

        if (Services is null)
        {
            return;
        }

        var quickAddViewModel = Services.GetRequiredService<QuickAddViewModel>();
        var quickAddWindow = new QuickAddWindow { DataContext = quickAddViewModel };
        quickAddViewModel.Closed += (_, _) =>
        {
            quickAddWindow.Close();
            _ = widgetViewModel.LoadTasksAsync();
        };

        quickAddWindow.Show();
        quickAddWindow.Activate();
    }

    /// <summary>
    /// Wires Quick Add to Cmd/Ctrl+Shift+N, systemwide (Phase 22's "Global Shortcut"
    /// deliverable). Resolved from DI rather than constructed here so
    /// <see cref="DependencyInjection.PlatformServiceCollectionExtensions"/> stays the single place that knows
    /// which platform implementation applies. A failed <c>Register()</c> (already claimed by
    /// another app, or an unverified Windows P/Invoke path failing) is logged by the service
    /// itself and left as a silently-unavailable feature rather than surfaced to the user —
    /// the tray menu's "Quick Add…" item is still there as a fallback either way.
    /// </summary>
    private static void SetupGlobalHotkey(IClassicDesktopStyleApplicationLifetime desktop, WidgetWindow widgetWindow, WidgetViewModel widgetViewModel)
    {
        if (Services is null)
        {
            return;
        }

        var hotkeyService = Services.GetRequiredService<IGlobalHotkeyService>();
        hotkeyService.Pressed += (_, _) => Dispatcher.UIThread.Post(() => OpenQuickAdd(widgetWindow, widgetViewModel));
        hotkeyService.Register();

        desktop.ShutdownRequested += (_, _) => hotkeyService.Unregister();
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

    /// <summary>
    /// Phase 27's Light/Dark/System theme — sets <c>RequestedThemeVariant</c> on the
    /// <c>Application</c> itself (not per-window), which every open window's
    /// <c>DynamicResource</c>-bound colors re-resolve against automatically; no per-window
    /// wiring needed. Called at startup here and again from <c>WidgetWindow</c> after the
    /// Settings window saves a theme choice, mirroring <see cref="ApplyAccentColor"/>'s
    /// "apply once at launch, re-apply live after Settings closes" pattern. Unrecognized or
    /// "System" values fall through to <c>ThemeVariant.Default</c> (follow the OS) —
    /// the same fallback this app already had before Phase 27, when
    /// <c>RequestedThemeVariant="Default"</c> in <c>App.axaml</c> had no themed resources to
    /// actually affect yet.
    /// </summary>
    public static void ApplyTheme(string theme)
    {
        Current!.RequestedThemeVariant = theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default,
        };
    }
}