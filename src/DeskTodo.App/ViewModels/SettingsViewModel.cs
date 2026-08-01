using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Settings window: accent color, background opacity, remembered
/// window position/size (Phase 12), notifications (Phase 13), and
/// auto-start (Phase 15). Theme, backups, shortcuts, language and
/// date/time format still need systems that don't exist yet (a
/// themed-resource pass, import/export, a shortcut system, i18n), so
/// they're still not here — see docs/ARCHITECTURE.md's "Phase 12" section.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAutoStartService _autoStartService;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _loaded = new();

    public SettingsViewModel(ISettingsService settingsService, IAutoStartService autoStartService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _logger = logger;
    }

    public IReadOnlyList<string> AccentColorPresets { get; } =
        ["#3B82F6", "#8B5CF6", "#10B981", "#EC4899", "#F97316", "#14B8A6"];

    [ObservableProperty]
    public partial string AccentColorHex { get; set; } = "#3B82F6";

    /// <summary>0–100 for the slider's display; converted to/from <see cref="AppSettings.WidgetOpacity"/>'s 0.4–1.0 range on load/save.</summary>
    [ObservableProperty]
    public partial double OpacityPercent { get; set; } = 95;

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Seeded from <see cref="IAutoStartService.IsEnabled"/> (the real OS registration
    /// state — a LaunchAgent plist's presence on macOS, a registry value's presence on
    /// Windows) rather than a persisted flag in <see cref="AppSettings"/>, so this can never
    /// drift out of sync with reality (e.g. if the user deleted the LaunchAgent by hand).
    /// </summary>
    [ObservableProperty]
    public partial bool AutoStartEnabled { get; set; }

    /// <summary>See <see cref="AppSettings.ShowInTaskbar"/>.</summary>
    [ObservableProperty]
    public partial bool ShowInTaskbar { get; set; } = true;

    /// <summary>See <see cref="AppSettings.AutoRescheduleOverdueTasks"/>.</summary>
    [ObservableProperty]
    public partial bool AutoRescheduleOverdueTasks { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    /// <summary>Raised after a successful save; the view closes itself and the widget re-applies settings in response.</summary>
    public event EventHandler? Saved;

    public event EventHandler? CancelRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _loaded = await _settingsService.LoadAsync(cancellationToken);
        AccentColorHex = _loaded.AccentColorHex;
        OpacityPercent = Math.Round(_loaded.WidgetOpacity * 100);
        NotificationsEnabled = _loaded.NotificationsEnabled;
        AutoStartEnabled = _autoStartService.IsEnabled;
        ShowInTaskbar = _loaded.ShowInTaskbar;
        AutoRescheduleOverdueTasks = _loaded.AutoRescheduleOverdueTasks;
        IsLoaded = true;
    }

    [RelayCommand]
    private void SelectAccentColor(string hex) => AccentColorHex = hex;

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            _loaded.AccentColorHex = AccentColorHex;
            _loaded.WidgetOpacity = Math.Clamp(OpacityPercent / 100.0, 0.4, 1.0);
            _loaded.NotificationsEnabled = NotificationsEnabled;
            _loaded.ShowInTaskbar = ShowInTaskbar;
            _loaded.AutoRescheduleOverdueTasks = AutoRescheduleOverdueTasks;

            await _settingsService.SaveAsync(_loaded);

            // Not persisted alongside the rest of AppSettings — see AutoStartEnabled's doc
            // comment. Idempotent either way (re-enabling just rewrites the same
            // plist/registry value), so no need to compare against the previous state first.
            if (AutoStartEnabled)
            {
                _autoStartService.Enable();
            }
            else
            {
                _autoStartService.Disable();
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    /// <summary>Clears the remembered window position/size — takes effect the next time DeskTodo opens, not on the window that's currently open.</summary>
    [RelayCommand]
    private async Task ResetWindowPositionAsync()
    {
        try
        {
            _loaded.WindowLeft = null;
            _loaded.WindowTop = null;
            _loaded.WindowWidth = null;
            _loaded.WindowHeight = null;

            await _settingsService.SaveAsync(_loaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset window position");
        }
    }

    [RelayCommand]
    private void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);
}
