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
    /// hidden, restored the next time the grid opens. A single persisted layout rather than
    /// multiple named "saved views" — column width/order aren't included, only visibility,
    /// to keep this to the one thing worth remembering across sessions without needing a
    /// dedicated saved-views concept.
    /// </summary>
    public List<string> HiddenGridColumns { get; set; } = [];
}
