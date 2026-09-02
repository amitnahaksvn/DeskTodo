namespace DeskTodo.Application.Settings;

/// <summary>
/// User-editable appearance/behavior settings, persisted as JSON under
/// <see cref="Options.AppStorageOptions.SettingsFileName"/>. Distinct from
/// <see cref="Options.AppStorageOptions"/> (host-configured, bound once at
/// startup from `IConfiguration`) — this is loaded/saved at runtime as the
/// user changes it in the Settings window.
/// </summary>
public sealed class AppSettings
{
    /// <summary>"#RRGGBB". Overrides Avalonia's <c>SystemAccentColor</c> resource, which the Fluent theme uses for default control accenting (e.g. the progress bar fill).</summary>
    public string AccentColorHex { get; set; } = "#3B82F6";

    /// <summary>0.4–1.0. Blended into the widget's background alpha channel — the widget stays fully opaque text on a more/less see-through card.</summary>
    public double WidgetOpacity { get; set; } = 0.95;

    /// <summary>Last-known window bounds, restored on next launch. Null (all four, always together) means "use the built-in default size, centered."</summary>
    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    /// <summary>
    /// Overdue-task alerts and the once-daily "N tasks today" summary. Auto-start has no
    /// equivalent flag here — <c>IAutoStartService.IsEnabled</c> (the OS registration itself:
    /// a LaunchAgent plist on macOS, a registry value on Windows) is the single source of
    /// truth for that, rather than a second flag that could drift out of sync with it.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the widget appears in the OS taskbar/Dock and app-switcher (Alt+Tab /
    /// Cmd+Tab). Defaults to <c>true</c> (today's behavior — DeskTodo shows up as a normal
    /// app) so introducing this setting doesn't silently change anything for existing users;
    /// flipping it off is opt-in. This only ever hides the taskbar/Dock *entry* — it doesn't
    /// place the window behind desktop icons or otherwise change how it's drawn; that's a
    /// separate, deliberately out-of-scope feature (see docs/ARCHITECTURE.md's "Phase 15").
    /// </summary>
    public bool ShowInTaskbar { get; set; } = true;

    /// <summary>
    /// When true, every incomplete task left behind on a past day is bumped to today the
    /// next time the widget loads today's list (see <c>ITaskService.RescheduleOverdueTasksAsync</c>).
    /// Defaults to <c>false</c> — moving a task's <see cref="Domain.Entities.TaskItem.PlanDate"/>
    /// is a real data change (not just a display preference), so it stays opt-in rather than
    /// silently altering existing users' task history the first time this ships.
    /// </summary>
    public bool AutoRescheduleOverdueTasks { get; set; }

    /// <summary>
    /// Names of grid-view columns (Category/Due/Notes — the ones the user can hide) currently
    /// hidden, restored the next time the grid opens. This is the grid's "current" layout —
    /// distinct from <see cref="GridSavedViews"/>, which are named snapshots of this same
    /// shape that a user can save and re-apply.
    /// </summary>
    public List<string> HiddenGridColumns { get; set; } = [];

    /// <summary>
    /// Whether the grid's checkbox + Title columns stay pinned while scrolling horizontally.
    /// Defaults to <c>true</c> — today's fixed behavior — so introducing this setting doesn't
    /// silently change anything for existing users.
    /// </summary>
    public bool GridColumnsFrozen { get; set; } = true;

    /// <summary>
    /// Named, user-saved grid column layouts (see <see cref="Settings.GridSavedView"/>) — a
    /// user can save the grid's current hidden-column set under a name and re-apply it later,
    /// distinct from the single "current" layout in <see cref="HiddenGridColumns"/>.
    /// </summary>
    public List<GridSavedView> GridSavedViews { get; set; } = [];

    /// <summary>
    /// Phase 22's Mini Widget — when true, the widget collapses to just its header and
    /// today's progress summary (no day-nav, search, add-task row, or task list), for users
    /// who want a smaller desktop footprint. Defaults to <c>false</c> (today's full layout).
    /// </summary>
    public bool IsMiniWidgetMode { get; set; }

    /// <summary>
    /// Phase 22's Multi Monitor Support — an opaque identifier (built by the App layer from
    /// the platform's screen enumeration; this project doesn't know its shape) for the
    /// monitor the widget should open on. Null means "use whatever
    /// <see cref="WindowLeft"/>/<see cref="WindowTop"/> already resolve to" (today's
    /// behavior) rather than actively re-placing the window — this is only consulted when
    /// the user explicitly picks a monitor in Settings.
    /// </summary>
    public string? PreferredMonitorId { get; set; }

    /// <summary>
    /// Phase 23's Pomodoro preset lengths — the one piece of session-timer configuration
    /// worth exposing (everything else about a session, like which task it's linked to, is
    /// chosen per-session in the Focus Timer window itself, not a standing setting). Defaults
    /// match the classic 25-minute-work/5-minute-break technique.
    /// </summary>
    public int PomodoroWorkMinutes { get; set; } = 25;

    public int PomodoroBreakMinutes { get; set; } = 5;

    /// <summary>
    /// Phase 23's Break/Water/Stretch Reminders — periodic wellness nudges delivered via the
    /// existing <see cref="Abstractions.INotificationService"/> (Phase 13), on the widget's
    /// existing 30-second poll (see <c>WidgetViewModel</c>'s doc comment on why this reuses
    /// that timer rather than adding a new one). All three default to disabled — an unasked-for
    /// recurring notification is exactly the kind of thing that should be opt-in, not a
    /// surprise the first time this ships.
    /// </summary>
    public bool BreakReminderEnabled { get; set; }

    public int BreakReminderIntervalMinutes { get; set; } = 60;

    public bool WaterReminderEnabled { get; set; }

    public int WaterReminderIntervalMinutes { get; set; } = 45;

    public bool StretchReminderEnabled { get; set; }

    public int StretchReminderIntervalMinutes { get; set; } = 90;

    /// <summary>
    /// Phase 26's Sound Notification — whether overdue/daily-summary/wellness notifications
    /// play a sound (see <see cref="Abstractions.INotificationService.NotifyAsync"/>'s
    /// <c>playSound</c> parameter). Defaults to <c>true</c> — today's existing behavior —
    /// so introducing this setting doesn't silently go quiet for existing users.
    /// </summary>
    public bool NotificationSoundEnabled { get; set; } = true;

    /// <summary>
    /// Phase 29's PIN Lock — when true (and <see cref="PinHash"/> is set), a lock screen is
    /// shown at app startup instead of the widget, requiring the correct PIN before it
    /// appears. Defaults to <c>false</c> so introducing this setting doesn't lock anyone out
    /// the first time it ships.
    /// </summary>
    public bool PinLockEnabled { get; set; }

    /// <summary>PBKDF2 hash of the PIN, base64-encoded — see <see cref="Security.PinHasher"/>. Never the plaintext PIN itself.</summary>
    public string? PinHash { get; set; }

    /// <summary>Random per-installation salt for <see cref="PinHash"/>, base64-encoded.</summary>
    public string? PinSalt { get; set; }

    /// <summary>
    /// Phase 32's User Profile — the one piece of "Team collaboration &amp; sharing" that
    /// ships independently of Phase 31's (deferred) sync/backend infrastructure: purely
    /// local personalization, not an account. Null/empty means "no name set," which the UI
    /// falls back to an unnamed placeholder for rather than treating as an error.
    /// </summary>
    public string? UserDisplayName { get; set; }

    /// <summary>The avatar initial's background color — "#RRGGBB", same format as <see cref="AccentColorHex"/>. Defaults to the app's own default accent color so a fresh install's avatar isn't an arbitrary, unrelated color.</summary>
    public string UserAvatarColorHex { get; set; } = "#3B82F6";

    /// <summary>
    /// Phase 27's Light/Dark/System theme — one of "System", "Light", "Dark". Defaults to
    /// "System" (follow the OS), matching the app's pre-Phase-27 behavior: <c>App.axaml</c>
    /// already had <c>RequestedThemeVariant="Default"</c> from the start, it just had no
    /// themed resources for that to actually affect until this phase.
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Feature 54's Capacity Planning — how many hours per day the user actually has available
    /// for planned work. Deliberately a single number, not the spec's fuller
    /// <c>CapacityProfile</c> (working days/holidays/breaks/timezone) — one global daily figure
    /// is enough to compute Feature 53's workload heatmap and Feature 51's health-score
    /// "capacity" factor without needing a calendar-of-exceptions UI. Defaults to 8 (a standard
    /// workday).
    /// </summary>
    public double WorkingHoursPerDay { get; set; } = 8.0;

    /// <summary>
    /// Feature 56's Task Cost Tracking — an hourly rate applied to estimated/actual minutes to
    /// compute a cost. Null (the default) means cost tracking is off entirely, per the spec's
    /// own "keep this optional, many users won't need monetary tracking" note — the Analytics
    /// window's cost section only renders once this is set.
    /// </summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>Feature 83's Saved Views, generalized from the grid (<see cref="GridSavedViews"/>) to the widget's own day-list search/filter/sort bar — see <see cref="Settings.WidgetSavedView"/>.</summary>
    public List<WidgetSavedView> WidgetSavedViews { get; set; } = [];

    /// <summary>
    /// Feature 77's Keyboard Shortcut Manager — user overrides of the app's default shortcuts,
    /// keyed by a stable command id (e.g. "CommandPalette") to an OS-neutral combo string (e.g.
    /// "Mod+K", where "Mod" resolves to Cmd on macOS / Ctrl elsewhere, matching
    /// <c>WidgetWindow.RegisterKeyboardShortcuts</c>'s existing per-OS modifier handling).
    /// Empty means "every shortcut is at its built-in default" — no entry needed for that case.
    /// </summary>
    public Dictionary<string, string> KeyboardShortcutOverrides { get; set; } = [];
}
