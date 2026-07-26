namespace DeskTodo.Application.Options;

/// <summary>
/// Root filesystem locations DeskTodo uses for logs, settings and
/// crash-recovery data. Bound from the "AppStorage" configuration section.
/// </summary>
public sealed class AppStorageOptions
{
    /// <summary>
    /// The configuration section name this type binds to.
    /// </summary>
    public const string SectionName = "AppStorage";

    /// <summary>
    /// Root directory for all DeskTodo application data. When left blank,
    /// the host resolves this to the OS-appropriate per-user application
    /// data folder (see the Infrastructure layer's storage path resolver).
    /// </summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Name of the subdirectory under <see cref="RootDirectory"/> where log files are written.
    /// </summary>
    public string LogsDirectoryName { get; set; } = "logs";

    /// <summary>
    /// Name of the JSON file under <see cref="RootDirectory"/> that stores user settings.
    /// </summary>
    public string SettingsFileName { get; set; } = "settings.json";

    /// <summary>
    /// Name of the SQLite database file under <see cref="RootDirectory"/> that stores tasks and daily plans.
    /// </summary>
    public string DatabaseFileName { get; set; } = "desktodo.db";
}
