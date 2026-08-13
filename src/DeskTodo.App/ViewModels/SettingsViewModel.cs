using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Security;
using DeskTodo.Application.Settings;
using DeskTodo.Application.Updates;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Settings window: accent color, background opacity, remembered
/// window position/size (Phase 12), notifications (Phase 13), auto-start
/// (Phase 15), and Light/Dark/System theme (Phase 27). Backups, shortcuts,
/// language and date/time format still need systems that don't exist yet
/// (import/export, a shortcut system, i18n), so they're still not here —
/// see docs/ARCHITECTURE.md's "Phase 12" section.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAutoStartService _autoStartService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _loaded = new();

    public SettingsViewModel(ISettingsService settingsService, IAutoStartService autoStartService, IUpdateCheckService updateCheckService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _updateCheckService = updateCheckService;
        _logger = logger;
    }

    public IReadOnlyList<string> AccentColorPresets { get; } =
        ["#3B82F6", "#8B5CF6", "#10B981", "#EC4899", "#F97316", "#14B8A6"];

    /// <summary>See <see cref="AppSettings.Theme"/>. "System" first, matching the app's own pre-Phase-27 default.</summary>
    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    [ObservableProperty]
    public partial string Theme { get; set; } = "System";

    [ObservableProperty]
    public partial string AccentColorHex { get; set; } = "#3B82F6";

    /// <summary>See <see cref="AppSettings.UserDisplayName"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvatarInitial))]
    public partial string UserDisplayName { get; set; } = string.Empty;

    /// <summary>See <see cref="AppSettings.UserAvatarColorHex"/>. Reuses <see cref="AccentColorPresets"/> for the picker — one set of preset colors for the whole app, not two independent palettes to keep in sync.</summary>
    [ObservableProperty]
    public partial string UserAvatarColorHex { get; set; } = "#3B82F6";

    /// <summary>The single letter shown in the avatar circle — the trimmed name's first character, uppercased, or "?" for an unnamed profile. Computed, not persisted.</summary>
    public string AvatarInitial => string.IsNullOrWhiteSpace(UserDisplayName) ? "?" : UserDisplayName.Trim()[..1].ToUpperInvariant();

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

    /// <summary>See <see cref="AppSettings.PinLockEnabled"/>.</summary>
    [ObservableProperty]
    public partial bool PinLockEnabled { get; set; }

    /// <summary>True once a PIN has actually been set — distinct from <see cref="PinLockEnabled"/>, which can be freshly toggled on with no PIN chosen yet. Drives whether the UI shows "Change PIN" or prompts to set one.</summary>
    [ObservableProperty]
    public partial bool HasPinSet { get; set; }

    /// <summary>Staged, never round-tripped from <see cref="AppSettings.PinHash"/> — the actual PIN is never recoverable once hashed, and shouldn't be pre-filled even if it were.</summary>
    [ObservableProperty]
    public partial string NewPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PinErrorMessage { get; set; } = string.Empty;

    /// <summary>Phase 30's Auto-update system, scoped to an on-demand check — see <see cref="IUpdateCheckService"/>'s doc comment. The running assembly's version, read once at load, not a network call.</summary>
    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCheckingForUpdate { get; set; }

    [ObservableProperty]
    public partial string UpdateStatusMessage { get; set; } = string.Empty;

    /// <summary>Set only when <see cref="UpdateCheckResult.IsUpdateAvailable"/> was true — drives whether the "View Release" button shows at all.</summary>
    [ObservableProperty]
    public partial string? AvailableUpdateUrl { get; set; }

    /// <summary>Raised by <see cref="OpenReleasePageCommand"/> — a ViewModel shouldn't launch a browser itself, same "ViewModel shouldn't construct Views" reasoning as every other window hand-off in this app; <c>SettingsWindow</c> handles it via <c>TopLevel.Launcher</c>.</summary>
    public event EventHandler<string>? OpenUrlRequested;

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

    /// <summary>See <see cref="AppSettings.NotificationSoundEnabled"/>.</summary>
    [ObservableProperty]
    public partial bool NotificationSoundEnabled { get; set; } = true;

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
        Theme = _loaded.Theme;
        UserDisplayName = _loaded.UserDisplayName ?? string.Empty;
        UserAvatarColorHex = _loaded.UserAvatarColorHex;
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
        NotificationSoundEnabled = _loaded.NotificationSoundEnabled;
        PinLockEnabled = _loaded.PinLockEnabled;
        HasPinSet = !string.IsNullOrEmpty(_loaded.PinHash);
        NewPin = string.Empty;
        ConfirmPin = string.Empty;
        PinErrorMessage = string.Empty;
        AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";
        UpdateStatusMessage = string.Empty;
        AvailableUpdateUrl = null;
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
    private void SelectAvatarColor(string hex) => UserAvatarColorHex = hex;

    /// <summary>
    /// Validates and stages the PIN Lock fields into <see cref="_loaded"/> — returns false
    /// (with <see cref="PinErrorMessage"/> set) to abort <see cref="SaveAsync"/> without
    /// persisting or closing the window, the one validation-can-fail path this form has.
    /// </summary>
    private bool TryApplyPinSettings()
    {
        PinErrorMessage = string.Empty;

        if (!PinLockEnabled)
        {
            _loaded.PinLockEnabled = false;
            _loaded.PinHash = null;
            _loaded.PinSalt = null;
            return true;
        }

        if (string.IsNullOrEmpty(NewPin) && string.IsNullOrEmpty(ConfirmPin))
        {
            if (string.IsNullOrEmpty(_loaded.PinHash))
            {
                PinErrorMessage = "Enter a PIN to turn on App Lock.";
                return false;
            }

            // Toggled on with an existing PIN and no new one entered — keep it unchanged.
            _loaded.PinLockEnabled = true;
            return true;
        }

        if (NewPin.Length < 4)
        {
            PinErrorMessage = "PIN must be at least 4 digits.";
            return false;
        }

        if (NewPin != ConfirmPin)
        {
            PinErrorMessage = "PINs don't match.";
            return false;
        }

        var (salt, hash) = PinHasher.Hash(NewPin);
        _loaded.PinLockEnabled = true;
        _loaded.PinSalt = salt;
        _loaded.PinHash = hash;
        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!TryApplyPinSettings())
            {
                return;
            }

            _loaded.AccentColorHex = AccentColorHex;
            _loaded.Theme = Theme;
            _loaded.UserDisplayName = string.IsNullOrWhiteSpace(UserDisplayName) ? null : UserDisplayName.Trim();
            _loaded.UserAvatarColorHex = UserAvatarColorHex;
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
            _loaded.NotificationSoundEnabled = NotificationSoundEnabled;

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

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdate = true;
        UpdateStatusMessage = string.Empty;
        AvailableUpdateUrl = null;

        try
        {
            var result = await _updateCheckService.CheckForUpdateAsync();
            if (result.ErrorMessage is { } error)
            {
                UpdateStatusMessage = error;
            }
            else if (result.IsUpdateAvailable)
            {
                UpdateStatusMessage = $"Version {result.LatestVersion} is available.";
                AvailableUpdateUrl = result.ReleaseUrl;
            }
            else
            {
                UpdateStatusMessage = "You're on the latest version.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            UpdateStatusMessage = "Couldn't check for updates.";
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        if (AvailableUpdateUrl is { } url)
        {
            OpenUrlRequested?.Invoke(this, url);
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
