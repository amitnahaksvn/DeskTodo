using System.Collections.ObjectModel;
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

    /// <summary>See <see cref="AppSettings.PomodoroWorkMinutes"/>/<see cref="AppSettings.PomodoroBreakMinutes"/>. decimal?, not int, to bind directly to NumericUpDown.Value — same reasoning as <see cref="TaskEditViewModel.EstimatedMinutes"/>.</summary>
    [ObservableProperty]
    public partial decimal? PomodoroWorkMinutes { get; set; } = 25m;

    [ObservableProperty]
    public partial decimal? PomodoroBreakMinutes { get; set; } = 5m;

    /// <summary>See <see cref="AppSettings.BreakReminderEnabled"/>/<see cref="AppSettings.BreakReminderIntervalMinutes"/>.</summary>
    [ObservableProperty]
    public partial bool BreakReminderEnabled { get; set; }

    [ObservableProperty]
    public partial decimal? BreakReminderIntervalMinutes { get; set; } = 60m;

    [ObservableProperty]
    public partial bool WaterReminderEnabled { get; set; }

    [ObservableProperty]
    public partial decimal? WaterReminderIntervalMinutes { get; set; } = 45m;

    [ObservableProperty]
    public partial bool StretchReminderEnabled { get; set; }

    [ObservableProperty]
    public partial decimal? StretchReminderIntervalMinutes { get; set; } = 90m;

    /// <summary>
    /// Populated by <c>WidgetWindow</c> (via <see cref="SetAvailableMonitors"/>) before
    /// <see cref="LoadAsync"/> runs — this ViewModel has no Avalonia dependency of its own,
    /// so it can't enumerate <c>Screens</c> itself; the View hands it over, same
    /// "give the item what it needs directly" pattern used elsewhere in this codebase.
    /// </summary>
    public ObservableCollection<MonitorOption> Monitors { get; } = [MonitorOption.Unspecified];

    /// <summary>See <see cref="AppSettings.PreferredMonitorId"/>.</summary>
    [ObservableProperty]
    public partial MonitorOption SelectedMonitor { get; set; } = MonitorOption.Unspecified;

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
        SelectedMonitor = Monitors.FirstOrDefault(m => m.Id == _loaded.PreferredMonitorId) ?? MonitorOption.Unspecified;
        PomodoroWorkMinutes = _loaded.PomodoroWorkMinutes;
        PomodoroBreakMinutes = _loaded.PomodoroBreakMinutes;
        BreakReminderEnabled = _loaded.BreakReminderEnabled;
        BreakReminderIntervalMinutes = _loaded.BreakReminderIntervalMinutes;
        WaterReminderEnabled = _loaded.WaterReminderEnabled;
        WaterReminderIntervalMinutes = _loaded.WaterReminderIntervalMinutes;
        StretchReminderEnabled = _loaded.StretchReminderEnabled;
        StretchReminderIntervalMinutes = _loaded.StretchReminderIntervalMinutes;
        IsLoaded = true;
    }

    /// <summary>Called by <c>WidgetWindow</c> before <see cref="LoadAsync"/>, once per Settings-window opening (the set of connected monitors can change between openings).</summary>
    public void SetAvailableMonitors(IReadOnlyList<MonitorOption> monitors)
    {
        Monitors.Clear();
        Monitors.Add(MonitorOption.Unspecified);
        foreach (var monitor in monitors)
        {
            Monitors.Add(monitor);
        }
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
            _loaded.PreferredMonitorId = SelectedMonitor.Id.Length == 0 ? null : SelectedMonitor.Id;
            _loaded.PomodoroWorkMinutes = (int)(PomodoroWorkMinutes ?? 25m);
            _loaded.PomodoroBreakMinutes = (int)(PomodoroBreakMinutes ?? 5m);
            _loaded.BreakReminderEnabled = BreakReminderEnabled;
            _loaded.BreakReminderIntervalMinutes = (int)(BreakReminderIntervalMinutes ?? 60m);
            _loaded.WaterReminderEnabled = WaterReminderEnabled;
            _loaded.WaterReminderIntervalMinutes = (int)(WaterReminderIntervalMinutes ?? 45m);
            _loaded.StretchReminderEnabled = StretchReminderEnabled;
            _loaded.StretchReminderIntervalMinutes = (int)(StretchReminderIntervalMinutes ?? 90m);

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
