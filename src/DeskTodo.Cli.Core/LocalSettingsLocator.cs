using System.Text.Json;

namespace DeskTodo.Cli;

/// <summary>
/// Auto-discovers the running desktop app's Local REST API port/token by reading its
/// <c>settings.json</c> directly — "This ensures the CLI behaves exactly like the desktop
/// application" (this feature's own spec) means no separate token-copying step for the common
/// case of the CLI and the desktop app running as the same user on the same machine.
/// Deliberately duplicates <c>DeskTodo.Infrastructure.Storage.AppStoragePaths</c>'s tiny path
/// resolution rather than referencing that project — pulling in EF Core/SQLite/ClosedXML/Serilog
/// transitively just for one static method would be a poor trade for a ~15-line CLI tool. Reads
/// only the two fields it needs via <see cref="JsonDocument"/> rather than the full
/// <c>AppSettings</c> type, for the same reason.
/// </summary>
public static class LocalSettingsLocator
{
    private const string AppFolderName = "DeskTodo";
    private const string SettingsFileName = "settings.json";

    public static (int? Port, string? Token) TryReadApiSettings() =>
        TryReadApiSettingsFrom(Path.Combine(ResolveDefaultRootDirectory(), SettingsFileName));

    /// <summary>Exposed separately from <see cref="TryReadApiSettings"/> so tests can point it at a real, throwaway settings file instead of the actual per-user app-data path.</summary>
    public static (int? Port, string? Token) TryReadApiSettingsFrom(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
        {
            return (null, null);
        }

        try
        {
            using var stream = File.OpenRead(settingsFilePath);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            int? port = root.TryGetProperty("LocalApiPort", out var portElement) && portElement.TryGetInt32(out var parsedPort) ? parsedPort : null;
            string? token = root.TryGetProperty("LocalApiToken", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String ? tokenElement.GetString() : null;
            return (port, token);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string ResolveDefaultRootDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName);
    }
}
